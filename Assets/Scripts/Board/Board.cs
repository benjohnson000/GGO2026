using System;
using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Rules of the match-3 grid, with no presentation. Every operation resolves instantly and returns the
    /// <see cref="BoardEvent"/>s it produced, in order, for the view to play back.
    /// Cells are [x, y] with (0, 0) at the bottom left; gravity pulls toward y = 0.
    /// </summary>
    public class Board
    {
        // Safety limits so an unlucky deck can never hang the game.
        private const int MaxFillAttempts = 20;
        private const int MaxShuffleAttempts = 100;
        private const int MaxShuffleRounds = 5;
        private const int MaxCascades = 100;

        /// <summary>A destroyed tile whose effects still have to fire.</summary>
        private struct PendingEffect
        {
            public Tile Tile;
            public Vector2Int Cell;

            /// <summary>The effect that destroyed it, or null if it was matched or fired by a swap.</summary>
            public TileEffectActivatedEvent Cause;

            /// <summary>The other swapped tile, when the swap itself fired this one.</summary>
            public Tile SwappedWith;
        }

        private readonly Tile[,] cells;
        private readonly TilePool pool;
        private readonly System.Random rng;
        private readonly ComboTable combos;
        private readonly MatchFinder matchFinder = new MatchFinder();

        private readonly List<MatchGroup> matches = new List<MatchGroup>();
        private readonly List<ComboCreatedEvent> comboSpawns = new List<ComboCreatedEvent>();
        private readonly List<PendingEffect> pendingEffects = new List<PendingEffect>();
        private readonly List<Tile> rejected = new List<Tile>();
        private readonly List<Tile> shuffleTiles = new List<Tile>();
        private readonly List<Vector2Int> shuffleCells = new List<Vector2Int>();
        private readonly Dictionary<Tile, Vector2Int> shuffleOrigins = new Dictionary<Tile, Vector2Int>();

        public int Width { get; }
        public int Height { get; }

        /// <summary>Accepted swaps so far. Turns are measured in swaps, so cascades and rejected swaps don't count.</summary>
        public int SwapCount { get; private set; }

        /// <summary>
        /// The fight tile effects act on, or null for none (effects that only touch combat then do nothing).
        /// Actions an effect queues are resolved right after it fires and attached to its
        /// <see cref="TileEffectActivatedEvent.CombatEvents"/>, so they stay in order with the board events.
        /// </summary>
        public Combat Combat { get; set; }

        /// <param name="combos">Which combo tiles special matches make. Null means matches never make combos.</param>
        public Board(int width, int height, TilePool pool, System.Random rng, ComboTable combos = null)
        {
            if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width), $"Board size must be at least 1x1, got {width}x{height}.");
            this.pool = pool ?? throw new ArgumentNullException(nameof(pool));
            this.rng = rng ?? new System.Random();
            this.combos = combos;

            Width = width;
            Height = height;
            cells = new Tile[width, height];
        }

        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }

        /// <summary>Tile at the cell, or null if the cell is empty or out of bounds.</summary>
        public Tile GetTile(Vector2Int cell)
        {
            return IsInBounds(cell) ? cells[cell.x, cell.y] : null;
        }

        /// <summary>
        /// Fills every empty cell from the pool without creating matches where the pool allows it,
        /// then reshuffles if there is no valid move. Meant to be called once, on an empty board.
        /// </summary>
        public List<BoardEvent> Fill()
        {
            List<BoardEvent> events = new List<BoardEvent>();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (cells[x, y] != null) continue;

                    Tile tile = DrawWithoutMatch(x, y);
                    cells[x, y] = tile;
                    // Every tile starts the same distance above its cell, so the whole board drops in together.
                    events.Add(new TileSpawnedEvent(tile, new Vector2Int(x, y), new Vector2Int(x, Height + y)));
                }
            }

            EnsureValidMove(events);
            return events;
        }

        /// <summary>
        /// Swaps two adjacent tiles. If the swap makes no match (and neither tile fires on swap, like a colour bomb)
        /// it is undone and does not count as a turn.
        /// Otherwise the board resolves every match and cascade, then reshuffles if no valid move is left.
        /// Returns no events for an illegal request (not adjacent, out of bounds or empty).
        /// </summary>
        public List<BoardEvent> TrySwap(Vector2Int a, Vector2Int b)
        {
            List<BoardEvent> events = new List<BoardEvent>();

            Tile tileA = GetTile(a);
            Tile tileB = GetTile(b);
            if (tileA == null || tileB == null) return events;
            if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1) return events;

            SwapCells(a, b);
            bool accepted = ActivatesOnSwap(tileA) || ActivatesOnSwap(tileB)
                || MatchFinder.HasMatchThrough(cells, a) || MatchFinder.HasMatchThrough(cells, b);
            if (!accepted) SwapCells(a, b);

            events.Add(new SwapEvent(tileA, tileB, a, b, accepted));
            if (!accepted) return events;

            SwapCount++;
            Resolve(events, a, b);
            EnsureValidMove(events);
            return events;
        }

        /// <summary>True if some swap of two adjacent tiles would make a match or fire a swap-activated tile.</summary>
        public bool HasValidMove()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (x + 1 < Width && SwapMakesMatch(cell, new Vector2Int(x + 1, y))) return true;
                    if (y + 1 < Height && SwapMakesMatch(cell, new Vector2Int(x, y + 1))) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Clears matches, drops tiles and refills until the board is stable.
        /// <paramref name="swapA"/> and <paramref name="swapB"/> are the swapped cells (dragged tile first), where
        /// combos from the swap's own matches are placed; null when resolving without a swap.
        /// </summary>
        private void Resolve(List<BoardEvent> events, Vector2Int? swapA = null, Vector2Int? swapB = null)
        {
            for (int cascade = 0; cascade < MaxCascades; cascade++)
            {
                comboSpawns.Clear();
                pendingEffects.Clear();

                // A swap that fires a tile (colour bomb) resolves only that this step; matches wait for the next cascade.
                bool firedBySwap = cascade == 0 && FireSwapActivations(events, swapA, swapB);
                if (!firedBySwap)
                {
                    matchFinder.FindMatches(cells, matches);
                    if (matches.Count == 0) return;

                    foreach (MatchGroup group in matches) events.Add(new MatchEvent(group, cascade));

                    // Combos are chosen before anything is destroyed, but placed after, into cells the match emptied.
                    if (combos != null)
                    {
                        foreach (MatchGroup group in matches)
                        {
                            if (!combos.TryCreate(group.Shape, group.Colour, out Tile combo)) continue;
                            Vector2Int cell = cascade == 0 ? PickComboCell(group, swapA, swapB) : group.ComboCell;
                            comboSpawns.Add(new ComboCreatedEvent(combo, cell, group));
                        }
                    }

                    foreach (MatchGroup group in matches)
                    {
                        foreach (Vector2Int cell in group.Cells) DestroyAt(cell, events, null);
                    }
                }

                // Only the swap's own matches know the swap direction; cascades and chained effects do not.
                Vector2Int? swapDirection = cascade == 0 && swapA.HasValue && swapB.HasValue ? swapB.Value - swapA.Value : (Vector2Int?)null;

                // Effects can destroy more tiles with effects, which queue their own, so this list may grow while looping.
                for (int i = 0; i < pendingEffects.Count; i++)
                {
                    PendingEffect pending = pendingEffects[i];
                    TileEffectContext context = new TileEffectContext(this, pending.Tile, pending.Cell,
                        pending.Cause == null ? swapDirection : null, pending.Cause, pending.SwappedWith, Combat, rng);

                    // A tile's effects fire in list order, each seeing the board as the previous one left it.
                    IReadOnlyList<TileEffect> effects = pending.Tile.Effects;
                    for (int e = 0; e < effects.Count; e++)
                    {
                        if (effects[e] != null) FireEffect(effects[e], context, events);
                    }
                }

                foreach (ComboCreatedEvent spawn in comboSpawns)
                {
                    cells[spawn.Cell.x, spawn.Cell.y] = spawn.Tile;
                    events.Add(spawn);
                }

                ApplyGravity(events);
                Refill(events);
            }

            Debug.LogWarning($"Board: stopped resolving after {MaxCascades} cascades.");
        }

        /// <summary>
        /// Runs one effect, resolves the combat actions it queued, logs its event and destroys its targets
        /// (whose own effects queue with this event as the cause).
        /// </summary>
        private void FireEffect(TileEffect effect, TileEffectContext context, List<BoardEvent> events)
        {
            TileEffectActivatedEvent activated = effect.Activate(context);

            List<CombatEvent> combatEvents = Combat != null ? Combat.Resolve() : null;
            bool hasCombatEvents = combatEvents != null && combatEvents.Count > 0;
            if (activated == null)
            {
                // An effect that queued combat actions but returned no event still gets one, so its results are shown.
                if (!hasCombatEvents) return;
                activated = new TileEffectActivatedEvent(effect, context.Tile, context.Cell);
            }
            if (hasCombatEvents) activated.CombatEvents = combatEvents;

            events.Add(activated);
            foreach (Vector2Int target in activated.Targets)
            {
                if (IsInBounds(target)) DestroyAt(target, events, activated);
            }
        }

        /// <summary>The dragged tile's cell if it is in the group, else the other swapped cell, else the group's default.</summary>
        private static Vector2Int PickComboCell(MatchGroup group, Vector2Int? swapA, Vector2Int? swapB)
        {
            if (swapA.HasValue && group.Contains(swapA.Value)) return swapA.Value;
            if (swapB.HasValue && group.Contains(swapB.Value)) return swapB.Value;
            return group.ComboCell;
        }

        /// <summary>
        /// Destroys swapped tiles that fire on swap and queues their effects with the other tile as the partner.
        /// Both tiles are read first, so two swapped colour bombs each see the other. Returns true if any fired.
        /// </summary>
        private bool FireSwapActivations(List<BoardEvent> events, Vector2Int? swapA, Vector2Int? swapB)
        {
            if (!swapA.HasValue || !swapB.HasValue) return false;

            Vector2Int a = swapA.Value;
            Vector2Int b = swapB.Value;
            Tile tileA = cells[a.x, a.y];
            Tile tileB = cells[b.x, b.y];
            bool firesA = ActivatesOnSwap(tileA);
            bool firesB = ActivatesOnSwap(tileB);

            if (firesA) DestroyAt(a, events, null, tileB);
            if (firesB) DestroyAt(b, events, null, tileA);
            return firesA || firesB;
        }

        private static bool ActivatesOnSwap(Tile tile)
        {
            return tile != null && tile.ActivatesOnSwap;
        }

        /// <summary>
        /// Removes the tile at a cell, if any, returns it to the pool and queues its effect.
        /// <paramref name="cause"/> is the effect that destroyed it, or null for a match or swap;
        /// <paramref name="swappedWith"/> is the partner tile when the swap itself fired it.
        /// </summary>
        private void DestroyAt(Vector2Int cell, List<BoardEvent> events, TileEffectActivatedEvent cause, Tile swappedWith = null)
        {
            Tile tile = cells[cell.x, cell.y];
            if (tile == null) return;

            cells[cell.x, cell.y] = null;
            events.Add(new TileDestroyedEvent(tile, cell));
            // Returned before refilling, so a destroyed tile can be redrawn straight away. Combos are discarded.
            pool.Return(tile);

            if (tile.HasEffects) pendingEffects.Add(new PendingEffect { Tile = tile, Cell = cell, Cause = cause, SwappedWith = swappedWith });
        }

        private void ApplyGravity(List<BoardEvent> events)
        {
            for (int x = 0; x < Width; x++)
            {
                int write = 0;
                for (int y = 0; y < Height; y++)
                {
                    Tile tile = cells[x, y];
                    if (tile == null) continue;

                    if (y != write)
                    {
                        cells[x, write] = tile;
                        cells[x, y] = null;
                        events.Add(new TileMovedEvent(tile, new Vector2Int(x, y), new Vector2Int(x, write)));
                    }
                    write++;
                }
            }
        }

        /// <summary>Fills empty cells from the pool. After gravity, empty cells are always at the top of a column.</summary>
        private void Refill(List<BoardEvent> events)
        {
            for (int x = 0; x < Width; x++)
            {
                int spawned = 0;
                for (int y = 0; y < Height; y++)
                {
                    if (cells[x, y] != null) continue;

                    Tile tile = pool.Draw();
                    cells[x, y] = tile;
                    events.Add(new TileSpawnedEvent(tile, new Vector2Int(x, y), new Vector2Int(x, Height + spawned)));
                    spawned++;
                }
            }
        }

        /// <summary>
        /// Draws a tile that won't complete a run or 2x2 square with the tiles already placed to its left and below.
        /// Rejected draws go back to the pool. If nothing fits, the last draw is used anyway.
        /// </summary>
        private Tile DrawWithoutMatch(int x, int y)
        {
            Tile chosen = null;
            for (int attempt = 0; attempt < MaxFillAttempts; attempt++)
            {
                Tile tile = pool.Draw();
                if (!CompletesRunOnFill(x, y, tile.Colour))
                {
                    chosen = tile;
                    break;
                }
                rejected.Add(tile);
            }

            if (chosen == null)
            {
                int last = rejected.Count - 1;
                chosen = rejected[last];
                rejected.RemoveAt(last);
            }

            foreach (Tile tile in rejected) pool.Return(tile);
            rejected.Clear();
            return chosen;
        }

        /// <summary>Fill goes bottom to top, left to right, so only the cells to the left and below are filled.</summary>
        private bool CompletesRunOnFill(int x, int y, TileColour colour)
        {
            if (colour == TileColour.None) return false;
            if (x >= 2 && IsColour(x - 1, y, colour) && IsColour(x - 2, y, colour)) return true;
            if (y >= 2 && IsColour(x, y - 1, colour) && IsColour(x, y - 2, colour)) return true;
            // 2x2 square with this cell as its top-right corner.
            if (x >= 1 && y >= 1 && IsColour(x - 1, y, colour) && IsColour(x, y - 1, colour) && IsColour(x - 1, y - 1, colour)) return true;
            return false;
        }

        private bool IsColour(int x, int y, TileColour colour)
        {
            return cells[x, y] != null && cells[x, y].Colour == colour;
        }

        private void EnsureValidMove(List<BoardEvent> events)
        {
            for (int round = 0; round < MaxShuffleRounds; round++)
            {
                if (HasValidMove()) return;

                Shuffle(events);
                // A fallback shuffle may contain matches; clear them like any other cascade.
                Resolve(events);
            }

            if (!HasValidMove()) Debug.LogWarning("Board: no valid move after reshuffling. The deck may not allow one.");
        }

        /// <summary>
        /// Rearranges the tiles already on the board, looking for a layout with no matches and at least one valid move.
        /// If none is found within the attempt limit, the last layout is kept.
        /// </summary>
        private void Shuffle(List<BoardEvent> events)
        {
            shuffleTiles.Clear();
            shuffleCells.Clear();
            shuffleOrigins.Clear();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Tile tile = cells[x, y];
                    if (tile == null) continue;

                    Vector2Int cell = new Vector2Int(x, y);
                    shuffleTiles.Add(tile);
                    shuffleCells.Add(cell);
                    shuffleOrigins[tile] = cell;
                }
            }

            for (int attempt = 0; attempt < MaxShuffleAttempts; attempt++)
            {
                for (int i = shuffleTiles.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (shuffleTiles[i], shuffleTiles[j]) = (shuffleTiles[j], shuffleTiles[i]);
                }

                for (int i = 0; i < shuffleTiles.Count; i++) cells[shuffleCells[i].x, shuffleCells[i].y] = shuffleTiles[i];

                if (!MatchFinder.HasAnyMatch(cells) && HasValidMove()) break;
            }

            List<TileMove> moves = new List<TileMove>();
            for (int i = 0; i < shuffleTiles.Count; i++)
            {
                Vector2Int from = shuffleOrigins[shuffleTiles[i]];
                if (from != shuffleCells[i]) moves.Add(new TileMove(shuffleTiles[i], from, shuffleCells[i]));
            }
            events.Add(new BoardShuffledEvent(moves));
        }

        private bool SwapMakesMatch(Vector2Int a, Vector2Int b)
        {
            if (cells[a.x, a.y] == null || cells[b.x, b.y] == null) return false;
            if (ActivatesOnSwap(cells[a.x, a.y]) || ActivatesOnSwap(cells[b.x, b.y])) return true;

            SwapCells(a, b);
            bool result = MatchFinder.HasMatchThrough(cells, a) || MatchFinder.HasMatchThrough(cells, b);
            SwapCells(a, b);
            return result;
        }

        private void SwapCells(Vector2Int a, Vector2Int b)
        {
            (cells[a.x, a.y], cells[b.x, b.y]) = (cells[b.x, b.y], cells[a.x, a.y]);
        }
    }
}
