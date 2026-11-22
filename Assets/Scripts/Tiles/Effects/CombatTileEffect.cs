using System;

namespace ggo2026
{
    /// <summary>
    /// Base for effects that act on the fight rather than the board: each queues one <see cref="CombatAction"/>.
    /// Does nothing when the board is not in a fight. The board resolves the action right after this fires and
    /// attaches what happened to the returned event's <see cref="TileEffectActivatedEvent.CombatEvents"/>.
    /// </summary>
    [Serializable]
    public abstract class CombatTileEffect : TileEffect
    {
        public sealed override TileEffectActivatedEvent Activate(TileEffectContext context)
        {
            if (context.Combat == null) return null;

            CombatAction action = CreateAction(context);
            if (action == null) return null;

            context.Combat.Enqueue(action);
            return new TileEffectActivatedEvent(this, context.Tile, context.Cell);
        }

        /// <summary>The action to queue, or null for none. <see cref="TileEffectContext.Combat"/> is never null here.</summary>
        protected abstract CombatAction CreateAction(TileEffectContext context);
    }
}
