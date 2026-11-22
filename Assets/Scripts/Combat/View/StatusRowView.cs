using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// A centred, wrapping row of <see cref="StatusIconView"/>s: block first, then statuses in the order they were
    /// applied. <see cref="CombatantView"/> creates one with the default values if none is assigned; add one to a
    /// RectTransform and assign it there to position or tune it. Sizes are in canvas units.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class StatusRowView : MonoBehaviour
    {
        // Gap below the combatant view for a row created in code.
        private const float DefaultGap = 0.25f;

        [Header("Layout")]
        [Min(0.01f)]
        [SerializeField] private float iconSize = 1.25f;

        [Min(0f)]
        [SerializeField] private float spacing = 0.25f;

        [Min(1)]
        [Tooltip("Icons per line. Further icons wrap onto lines below.")]
        [SerializeField] private int iconsPerLine = 5;

        [Header("Look")]
        [Tooltip("Optional. Defaults to the font of the combatant's health or name text.")]
        [SerializeField] private TMP_FontAsset font;

        [Tooltip("Behind the stack count, and the letter on icons without a sprite.")]
        [SerializeField] private Color badgeColour = new Color(0.141f, 0.075f, 0.102f);

        [SerializeField] private Color countColour = new Color(0.902f, 0.871f, 0.792f);

        [Tooltip("Optional. Without it, block is a square with a B.")]
        [SerializeField] private Sprite blockSprite;

        [SerializeField] private Color blockColour = new Color(0.36f, 0.63f, 0.87f);

        [Header("Animation")]
        [Min(1f)]
        [SerializeField] private float pulseScale = 1.35f;

        [Min(0f)]
        [SerializeField] private float pulseDuration = 0.25f;

        [Min(0f)]
        [SerializeField] private float removeDuration = 0.15f;

        private readonly List<StatusIconView> order = new List<StatusIconView>();
        private readonly Dictionary<StatusDataSO, StatusIconView> statusIcons = new Dictionary<StatusDataSO, StatusIconView>();
        private StatusIconView blockIcon;
        private TMP_FontAsset fallbackFont;

        public float IconSize => iconSize;
        public TMP_FontAsset Font => font != null ? font : fallbackFont;
        public Color BadgeColour => badgeColour;
        public Color CountColour => countColour;
        public float PulseScale => pulseScale;
        public float PulseDuration => pulseDuration;
        public float RemoveDuration => removeDuration;

        /// <summary>Creates a row with the default values just below <paramref name="parent"/>.</summary>
        public static StatusRowView Create(Transform parent)
        {
            GameObject go = new GameObject("Statuses", typeof(RectTransform));
            go.layer = parent.gameObject.layer;

            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -DefaultGap);

            StatusRowView row = go.AddComponent<StatusRowView>();
            rect.sizeDelta = new Vector2(row.iconsPerLine * (row.iconSize + row.spacing), row.iconSize);
            return row;
        }

        /// <summary>Empties the row. Call before showing a combatant.</summary>
        /// <param name="defaultFont">Used when the row has no font of its own.</param>
        public void ResetRow(TMP_FontAsset defaultFont)
        {
            fallbackFont = defaultFont;
            foreach (StatusIconView icon in order) Destroy(icon.gameObject);

            order.Clear();
            statusIcons.Clear();
            blockIcon = null;
        }

        /// <summary>Shows block as the first icon; 0 removes it.</summary>
        public void SetBlock(int amount, bool animate)
        {
            if (amount <= 0)
            {
                if (blockIcon == null) return;
                RemoveIcon(blockIcon);
                blockIcon = null;
                return;
            }

            if (blockIcon == null)
            {
                blockIcon = StatusIconView.Create(this, "Block", blockSprite, blockColour);
                order.Insert(0, blockIcon);
                Layout();
            }
            blockIcon.SetCount(amount, animate);
        }

        /// <summary>Shows a status's stacks; 0 removes it.</summary>
        public void SetStatus(StatusDataSO status, int stacks, bool animate)
        {
            bool shown = statusIcons.TryGetValue(status, out StatusIconView icon);

            if (stacks <= 0)
            {
                if (!shown) return;
                statusIcons.Remove(status);
                RemoveIcon(icon);
                return;
            }

            if (!shown)
            {
                icon = StatusIconView.Create(this, status.DisplayName, status.Icon, status.Colour);
                statusIcons.Add(status, icon);
                order.Add(icon);
                Layout();
            }
            icon.SetCount(stacks, animate);
        }

        private void RemoveIcon(StatusIconView icon)
        {
            order.Remove(icon);
            icon.Remove();
            Layout();
        }

        /// <summary>Centres each line, filling from the top of the row downward.</summary>
        private void Layout()
        {
            int perLine = Mathf.Max(1, iconsPerLine);
            float step = iconSize + spacing;

            for (int i = 0; i < order.Count; i++)
            {
                int line = i / perLine;
                int inLine = Mathf.Min(perLine, order.Count - line * perLine);
                float x = (i % perLine - (inLine - 1) * 0.5f) * step;
                float y = -iconSize * 0.5f - line * step;
                order[i].Rect.anchoredPosition = new Vector2(x, y);
            }
        }
    }
}
