using UnityEngine;

namespace ggo2026
{
    /// <summary>Poison: at the start of its owner's turn, deals damage equal to its stacks, then loses some stacks.</summary>
    [CreateAssetMenu(menuName = "GGO2026/Status/Poison", fileName = "Poison")]
    public class PoisonStatusSO : StatusDataSO
    {
        [Min(1)]
        [Tooltip("Stacks lost after each tick.")]
        [SerializeField] private int stacksLostPerTick = 1;

        [Tooltip("If true, poison damage skips block. Not settled in the design yet.")]
        [SerializeField] private bool ignoresBlock = true;

        public int StacksLostPerTick => stacksLostPerTick;
        public bool IgnoresBlock => ignoresBlock;

        public override StatusEffect CreateEffect() => new PoisonStatus(this);
    }
}
