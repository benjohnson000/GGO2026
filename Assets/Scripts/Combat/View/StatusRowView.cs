using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// A centred, wrapping row of <see cref="StatusIconView"/>s, in the order the statuses were applied. Block is
    /// not a status: <see cref="HealthBarView"/> shows it. Icons are copies of <see cref="iconPrefab"/>, which sets
    /// their size and look, and hang from this rect's top centre. Sizes are in canvas units.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class StatusRowView : MonoBehaviour
    {
        [Tooltip("Copied for each status. Its root's size is the space each icon takes.")]
        [SerializeField] private StatusIconView iconPrefab;

        [Header("Layout")]
        [Tooltip("Gap between icons, across and down. Negative values overlap them; later icons draw on top.")]
        [SerializeField] private float spacing = 0.25f;

        [Min(1)]
        [Tooltip("Icons per line. Further icons wrap onto lines below.")]
        [SerializeField] private int iconsPerLine = 5;

        [Header("Animation")]
        [Min(1f)]
        [SerializeField] private float pulseScale = 1.35f;

        [Min(0f)]
        [SerializeField] private float pulseDuration = 0.25f;

        [Min(0f)]
        [SerializeField] private float removeDuration = 0.15f;

        private readonly List<StatusIconView> order = new List<StatusIconView>();
        private readonly Dictionary<StatusDataSO, StatusIconView> statusIcons = new Dictionary<StatusDataSO, StatusIconView>();

        public float PulseScale => pulseScale;
        public float PulseDuration => pulseDuration;
        public float RemoveDuration => removeDuration;

        /// <summary>Empties the row. Call before showing a combatant.</summary>
        public void ResetRow()
        {
            if (iconPrefab == null) Debug.LogError($"{name}: StatusRowView needs an Icon Prefab assigned, or statuses aren't shown.", this);
            foreach (StatusIconView icon in order) Destroy(icon.gameObject);

            order.Clear();
            statusIcons.Clear();
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
                if (iconPrefab == null) return; // Reported by ResetRow.

                icon = StatusIconView.Create(iconPrefab, this, status);
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
            if (order.Count == 0) return;

            int perLine = Mathf.Max(1, iconsPerLine);
            Vector2 size = order[0].Size;
            Vector2 step = size + Vector2.one * spacing;

            for (int i = 0; i < order.Count; i++)
            {
                int line = i / perLine;
                int inLine = Mathf.Min(perLine, order.Count - line * perLine);
                float x = (i % perLine - (inLine - 1) * 0.5f) * step.x;
                float y = -size.y * 0.5f - line * step.y;
                order[i].Rect.anchoredPosition = new Vector2(x, y);
            }
        }
    }
}
