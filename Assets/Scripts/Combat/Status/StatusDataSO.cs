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

        [Tooltip("Tints the icon (leave white for full-colour art). Without an icon, colours a placeholder square.")]
        [SerializeField] private Color colour = Color.white;

        [Tooltip("Use Bar Colour on the health bar instead of Colour, e.g. when the icon is full-colour art and Colour is white.")]
        [SerializeField] private bool overrideBarColour;

        [ColorUsage(false)]
        [SerializeField] private Color barColour = Color.white;

        [TextArea]
        [SerializeField] private string description;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Icon => icon;
        public Color Colour => colour;
        public string Description => description;

        /// <summary>This status's band on the health bar. Always opaque, because the bar's layers overlap.</summary>
        public Color BarColour
        {
            get
            {
                Color band = overrideBarColour ? barColour : colour;
                band.a = 1f;
                return band;
            }
        }

        /// <summary>Creates the runtime effect. <see cref="Combat"/> sets its data, owner and stacks afterwards.</summary>
        public virtual StatusEffect CreateEffect() => new StatusEffect();
    }
}
