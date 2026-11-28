using UnityEngine;

namespace ggo2026
{
    /// <summary>Why and where a tile effect is firing. Built by the <see cref="Board"/> for each destroyed tile and shared by all of its effects.</summary>
    public readonly struct TileEffectContext
    {
        /// <summary>The board, for its size and tiles. Effects must not change it; they return targets instead.</summary>
        public Board Board { get; }

        public Tile Tile { get; }

        /// <summary>The cell the tile was in when it was destroyed.</summary>
        public Vector2Int Cell { get; }

        /// <summary>
        /// Direction of the swap (dragged tile to target) when the tile was matched by that swap's own matches;
        /// null in cascades, after reshuffles, and when the tile was destroyed by another effect.
        /// </summary>
        public Vector2Int? SwapDirection { get; }

        /// <summary>The effect that destroyed this tile, or null if it was matched or fired by a swap.</summary>
        public TileEffectActivatedEvent Cause { get; }

        /// <summary>
        /// The tile this one was swapped with, when the swap itself fired it (see <see cref="TileEffect.ActivatesOnSwap"/>);
        /// otherwise null. Read its colour and effects only: it may already be destroyed.
        /// </summary>
        public Tile SwappedWith { get; }

        /// <summary>
        /// The fight, or null when the board is not in one. Effects queue <see cref="CombatAction"/>s here; the board
        /// resolves them right after the effect returns and attaches the results to its event.
        /// </summary>
        public Combat Combat { get; }

        /// <summary>The side the tile belongs to (the player), or null outside a fight.</summary>
        public Combatant Owner => Combat?.Player;

        /// <summary>The other side (the enemy), or null outside a fight.</summary>
        public Combatant Opponent => Combat?.Enemy;

        /// <summary>
        /// The player's coins, or null when the board has none. Effects never change it directly: they return a
        /// <see cref="CoinsGainedEvent"/>, which the board applies.
        /// </summary>
        public CoinPurse Coins { get; }

        public System.Random Random { get; }

        public TileEffectContext(Board board, Tile tile, Vector2Int cell, Vector2Int? swapDirection, TileEffectActivatedEvent cause, Tile swappedWith, Combat combat, CoinPurse coins, System.Random random)
        {
            Board = board;
            Tile = tile;
            Cell = cell;
            SwapDirection = swapDirection;
            Cause = cause;
            SwappedWith = swappedWith;
            Combat = combat;
            Coins = coins;
            Random = random;
        }
    }
}
