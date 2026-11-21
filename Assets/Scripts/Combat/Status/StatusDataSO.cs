using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Data for one status. Statuses are compared by asset, so each status needs exactly one asset.
    /// A status with behaviour subclasses this and overrides <see cref="CreateEffect"/> to return its
    /// own <see cref="StatusEffect"/>, with tuning values as serialized fields (see <see cref="PoisonStatusSO"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "GGO2026/Status/Plain Status", fileName = "new Status")]
    public class StatusDataSO : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        [TextArea]
        [SerializeField] private string description;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Icon => icon;
        public string Description => description;

        /// <summary>Creates the runtime effect. <see cref="Combat"/> sets its data, owner and stacks afterwards.</summary>
        public virtual StatusEffect CreateEffect() => new StatusEffect();
    }
}
