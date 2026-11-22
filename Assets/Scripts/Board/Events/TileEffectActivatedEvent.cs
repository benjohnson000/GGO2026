using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// One <see cref="TileEffect"/> of a destroyed tile fired. Comes after the tile's own <see cref="TileDestroyedEvent"/>
    /// and is followed by one for each tile in <see cref="Targets"/>. A tile with several effects produces one of these
    /// per effect that did something, in list order. Effects may subclass this to add detail
    /// (see <see cref="LineClearActivatedEvent"/>).
    /// </summary>
    public class TileEffectActivatedEvent : BoardEvent
    {
        private static readonly CombatEvent[] NoCombatEvents = new CombatEvent[0];

        /// <summary>The effect that fired.</summary>
        public TileEffect Effect { get; }

        public Tile Tile { get; }

        /// <summary>The cell the tile was in when it fired.</summary>
        public Vector2Int Cell { get; }

        /// <summary>Cells the effect destroys. Empty cells and out-of-bounds cells are skipped by the board.</summary>
        public IReadOnlyList<Vector2Int> Targets { get; }

        /// <summary>What the combat actions the effect queued did, in order. Set by the board; empty if none.</summary>
        public IReadOnlyList<CombatEvent> CombatEvents { get; internal set; } = NoCombatEvents;

        /// <param name="targets">Cells to destroy, or null for none.</param>
        public TileEffectActivatedEvent(TileEffect effect, Tile tile, Vector2Int cell, IReadOnlyList<Vector2Int> targets = null)
        {
            Effect = effect;
            Tile = tile;
            Cell = cell;
            Targets = targets ?? new List<Vector2Int>();
        }
    }
}
