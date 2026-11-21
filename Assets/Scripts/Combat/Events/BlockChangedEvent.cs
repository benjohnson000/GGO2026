namespace ggo2026
{
    /// <summary>Block was gained or lost outside a hit (gaining block, turn-start loss). Block absorbed by a hit is on <see cref="DamageEvent"/>.</summary>
    public class BlockChangedEvent : CombatEvent
    {
        public int OldBlock { get; }
        public int NewBlock { get; }

        public BlockChangedEvent(Combatant target, int oldBlock, int newBlock) : base(target)
        {
            OldBlock = oldBlock;
            NewBlock = newBlock;
        }
    }
}
