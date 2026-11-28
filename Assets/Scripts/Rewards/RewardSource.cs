using System;
using Animancer;

namespace ggo2026
{
    /// <summary>
    /// Authoring data for one reward, listed on an <see cref="EnemyDataSO"/> with <c>[SerializeReference]</c> like
    /// intent actions. Instances are shared by every fight against that enemy, so keep only tuning values in fields
    /// and roll anything random in <see cref="Create"/>. Subclasses need <c>[Serializable]</c> and a parameterless
    /// constructor; they then appear in the inspector's type picker.
    /// </summary>
    [Serializable]
    public abstract class RewardSource : IPolymorphic
    {
        /// <summary>A new reward for one win, or null to give nothing (e.g. nothing to offer).</summary>
        public abstract Reward Create(System.Random rng);
    }
}
