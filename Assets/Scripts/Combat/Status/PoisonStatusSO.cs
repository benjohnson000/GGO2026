using UnityEngine;

namespace ggo2026
{
    /// <summary>Poison: each tick (its owner's turn start, or every swap) deals damage equal to its stacks, then loses some stacks.</summary>
    [CreateAssetMenu(menuName = "GGO2026/Status/Poison", fileName = "Poison")]
    public class PoisonStatusSO : StatusDataSO
    {
        [Tooltip("Per Turn: ticks at the start of its owner's turn (when the enemy intent fires). Per Swap: ticks after every accepted swap.")]
        [SerializeField] private StatusTickType tickType = StatusTickType.PerTurn;

        [Min(1)]
        [Tooltip("Stacks lost after each tick.")]
        [SerializeField] private int stacksLostPerTick = 1;

        [Tooltip("If true, poison damage skips block. Not settled in the design yet.")]
        [SerializeField] private bool ignoresBlock = true;

        public StatusTickType TickType => tickType;
        public int StacksLostPerTick => stacksLostPerTick;
        public bool IgnoresBlock => ignoresBlock;

        public override StatusEffect CreateEffect() => new PoisonStatus(this);
    }
}
