namespace ggo2026
{
    /// <summary>
    /// A change to the fight waiting in <see cref="Combat"/>'s queue. Tile effects, enemy intents and listeners
    /// create these instead of changing state directly, so every change runs through modifiers and triggers.
    /// </summary>
    public abstract class CombatAction
    {
        internal abstract void Execute(Combat combat);
    }
}
