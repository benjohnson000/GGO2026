using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ggo2026
{
    /// <summary>
    /// Placeholder UI for one <see cref="Combatant"/>: name, health bar, block and a text list of statuses.
    /// It shows the values from the events it is given, not the live combatant, so it stays in step with playback.
    /// Every reference is optional.
    /// </summary>
    public class CombatantView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;

        [Tooltip("An Image with Image Type set to Filled.")]
        [SerializeField] private Image healthFill;

        [SerializeField] private TMP_Text healthText;

        [Tooltip("Shown only while block is above 0.")]
        [SerializeField] private GameObject blockRoot;

        [SerializeField] private TMP_Text blockText;

        [Tooltip("Placeholder list of statuses and their stacks, until status icons exist.")]
        [SerializeField] private TMP_Text statusText;

        private readonly List<StatusDataSO> statusOrder = new List<StatusDataSO>();
        private readonly Dictionary<StatusDataSO, int> stacks = new Dictionary<StatusDataSO, int>();
        private readonly StringBuilder builder = new StringBuilder();

        private int health;
        private int maxHealth;
        private int block;
        private bool dead;

        public Combatant Combatant { get; private set; }

        /// <summary>Shows <paramref name="combatant"/> as it is right now.</summary>
        public void Bind(Combatant combatant)
        {
            Combatant = combatant;
            health = combatant.Health;
            maxHealth = combatant.MaxHealth;
            block = combatant.Block;
            dead = combatant.IsDead;

            statusOrder.Clear();
            stacks.Clear();
            foreach (StatusEffect status in combatant.Statuses)
            {
                statusOrder.Add(status.Data);
                stacks[status.Data] = status.Stacks;
            }

            Refresh();
        }

        /// <summary>Updates the display from one event. Events about the other combatant are ignored.</summary>
        public void Apply(CombatEvent combatEvent)
        {
            if (Combatant == null || combatEvent.Target != Combatant) return;

            switch (combatEvent)
            {
                case DamageEvent damage:
                    health = damage.HealthAfter;
                    block = damage.BlockAfter;
                    break;
                case HealedEvent healed:
                    health = healed.HealthAfter;
                    break;
                case BlockChangedEvent blockChanged:
                    block = blockChanged.NewBlock;
                    break;
                case StatusChangedEvent status:
                    SetStacks(status.Status, status.NewStacks);
                    break;
                case CombatantDiedEvent _:
                    dead = true;
                    break;
                default:
                    return;
            }

            Refresh();
        }

        private void SetStacks(StatusDataSO status, int count)
        {
            if (count <= 0)
            {
                statusOrder.Remove(status);
                stacks.Remove(status);
                return;
            }

            if (!stacks.ContainsKey(status)) statusOrder.Add(status);
            stacks[status] = count;
        }

        private void Refresh()
        {
            if (Combatant == null) return;

            if (nameText != null) nameText.text = dead ? $"{Combatant.DisplayName} (defeated)" : Combatant.DisplayName;
            if (healthFill != null) healthFill.fillAmount = maxHealth > 0 ? (float)health / maxHealth : 0f;
            if (healthText != null) healthText.text = $"{health} / {maxHealth}";
            if (blockRoot != null) blockRoot.SetActive(block > 0);
            if (blockText != null) blockText.text = block.ToString();

            if (statusText != null)
            {
                builder.Clear();
                foreach (StatusDataSO status in statusOrder)
                {
                    if (builder.Length > 0) builder.Append('\n');
                    builder.Append(status.DisplayName).Append(' ').Append(stacks[status]);
                }
                statusText.text = builder.ToString();
            }
        }
    }
}
