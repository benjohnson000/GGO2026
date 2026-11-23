using System;
using Animancer;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// One thing a tile does. A <see cref="TileDataSO"/> holds an ordered list of these with <c>[SerializeReference]</c>,
    /// so a tile can combine any number of effects (e.g. clear a line and deal damage), and each tile asset has its
    /// own copies with its own tuning values (e.g. Strike and Strike+ both use one damage effect with different amounts).
    /// <para>
    /// One instance is shared by every <see cref="Tile"/> made from that asset, so subclasses must not keep per-tile
    /// state in fields; keep only tuning values there. Subclasses need <c>[Serializable]</c> and a parameterless
    /// constructor. <see cref="IPolymorphic"/> lets the inspector pick the type of each list entry.
    /// </para>
    /// </summary>
    [Serializable]
    public abstract class TileEffect : IPolymorphic
    {
        [Tooltip("Optional. How this effect's hits fly from the tile: to the combatant it affects for combat effects, " +
                 "to each target tile for a laser or colour bomb. Empty uses the default (EncounterView for combat, Board View Settings " +
                 "for lasers; a colour bomb without one sends none).")]
        [ProjectileField]
        [SerializeField] private ProjectileStyleSO projectile;

        /// <summary>
        /// The projectile style for this effect's hits, or null for the default. Presentation only: the board and
        /// combat rules never read it.
        /// </summary>
        public ProjectileStyleSO Projectile => projectile;

        /// <summary>
        /// False for effects that never send a projectile (e.g. line clears, which draw their own blast), so the
        /// inspector hides <see cref="Projectile"/>. Override to return false when an effect neither queues combat
        /// actions nor launches projectiles itself.
        /// </summary>
        public virtual bool UsesProjectile => true;

        /// <summary>
        /// True if swapping the tile with any neighbour is a valid move that fires it, even with no match.
        /// The tile is destroyed by the swap, with <see cref="TileEffectContext.SwappedWith"/> set. A tile fires on
        /// swap if any of its effects does, and then all of its effects fire.
        /// </summary>
        public virtual bool ActivatesOnSwap => false;

        /// <summary>
        /// Called when the tile is destroyed, whether by a match, a swap or another effect. Effects must not change the
        /// board or combatants directly: board changes are described by the returned event, whose
        /// <see cref="TileEffectActivatedEvent.Targets"/> the board destroys, and combat changes are queued as
        /// <see cref="CombatAction"/>s on <see cref="TileEffectContext.Combat"/>. Return null if nothing happened.
        /// </summary>
        public abstract TileEffectActivatedEvent Activate(TileEffectContext context);
    }
}
