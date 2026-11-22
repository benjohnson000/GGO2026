using System;
using Animancer;

namespace ggo2026
{
    /// <summary>
    /// One thing an enemy intent does when it fires. An <see cref="EnemyIntent"/> holds an ordered list of these
    /// with <c>[SerializeReference]</c>, like tile effects. Instances are shared by every fight against that enemy,
    /// so keep only tuning values in fields. Subclasses need <c>[Serializable]</c> and a parameterless constructor.
    /// </summary>
    [Serializable]
    public abstract class IntentAction : IPolymorphic
    {
        /// <summary>The action to queue when the intent fires, or null for none.</summary>
        public abstract CombatAction CreateAction(Combat combat, Combatant enemy);

        /// <summary>Short text for the intent display, e.g. "Attack 8".</summary>
        public abstract string Describe();
    }
}
