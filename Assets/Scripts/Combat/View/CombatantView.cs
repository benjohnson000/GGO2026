using benjohnson;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ggo2026
{
    /// <summary>
    /// Placeholder UI for one <see cref="Combatant"/>: name, health bar, and a row of block and status icons.
    /// It shows the values from the events it is given, not the live combatant, so it stays in step with playback.
    /// Every reference is optional.
    /// </summary>
    public class CombatantView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;

        [Tooltip("Scaled on X by health / max health (0 = no health, 1 = full). Set its pivot to the end it should shrink toward, e.g. X = 0 to shrink toward the left.")]
        [SerializeField] private Image healthFill;

        [SerializeField] private TMP_Text healthText;

        [Tooltip("Optional separate block display, shown only while block is above 0. With neither this nor Block Text set, block is the first icon in the status row.")]
        [SerializeField] private GameObject blockRoot;

        [SerializeField] private TMP_Text blockText;

        [Tooltip("Optional. Where status icons go. If empty, a row is created just below this view.")]
        [SerializeField] private StatusRowView statusRow;

        [Tooltip("Enemy only: the telegraphed intent and its countdown, e.g. \"Attack 6 in 3\". Empty while there is no intent.")]
        [SerializeField] private TMP_Text intentText;

        [Tooltip("Optional. Where projectiles from tiles land, e.g. the portrait. Defaults to this object's pivot.")]
        [SerializeField] private Transform hitPoint;

        [Tooltip("Optional. Played when this combatant loses health, as the hit is shown. Not played for hits fully absorbed by block.")]
        [SerializeField] private SoundProfileSO damageSound;

        private int health;
        private int maxHealth;
        private int block;
        private bool dead;
        private string intent;
        private int countdown;

        public Combatant Combatant { get; private set; }

        /// <summary>Where projectiles from tiles aimed at this combatant land.</summary>
        public Transform HitPoint => hitPoint != null ? hitPoint : transform;

        private bool ShowsBlockInRow => blockRoot == null && blockText == null;

        /// <summary>Shows <paramref name="combatant"/> as it is right now.</summary>
        public void Bind(Combatant combatant)
        {
            Combatant = combatant;
            health = combatant.Health;
            maxHealth = combatant.MaxHealth;
            block = combatant.Block;
            dead = combatant.IsDead;
            intent = null;
            countdown = combatant.IntentCountdown;

            if (statusRow == null) statusRow = StatusRowView.Create(transform);
            TMP_Text fontSource = healthText != null ? healthText : nameText;
            statusRow.ResetRow(fontSource != null ? fontSource.font : null);

            // What is already active appears without animating.
            if (ShowsBlockInRow) statusRow.SetBlock(block, false);
            foreach (StatusEffect status in combatant.Statuses) statusRow.SetStatus(status.Data, status.Stacks, false);

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
                    if (damage.HealthLost > 0) PlaySound(damageSound);
                    break;
                case HealedEvent healed:
                    health = healed.HealthAfter;
                    break;
                case BlockChangedEvent blockChanged:
                    block = blockChanged.NewBlock;
                    break;
                case StatusChangedEvent status:
                    statusRow.SetStatus(status.Status, status.NewStacks, true);
                    break;
                case CombatantDiedEvent _:
                    dead = true;
                    intent = null;
                    break;
                case IntentChangedEvent intentChanged:
                    intent = intentChanged.Intent?.Describe();
                    break;
                case CountdownChangedEvent countdownChanged:
                    countdown = countdownChanged.NewCountdown;
                    break;
                default:
                    return;
            }

            Refresh();
        }

        /// <summary>Plays an optional sound. Silent if it is unassigned, has no clips, or no SFXManager is loaded.</summary>
        private static void PlaySound(SoundProfileSO sound)
        {
            if (sound == null || !sound.HasClips) return;
            SFXManager sfx = SFXManager.instance;
            if (sfx != null) sfx.PlaySound(sound);
        }

        private void Refresh()
        {
            if (Combatant == null) return;

            if (nameText != null) nameText.text = dead ? $"{Combatant.DisplayName} (defeated)" : Combatant.DisplayName;
            if (healthFill != null)
            {
                Transform fill = healthFill.transform;
                Vector3 scale = fill.localScale;
                scale.x = maxHealth > 0 ? Mathf.Clamp01((float)health / maxHealth) : 0f;
                fill.localScale = scale;
            }
            if (healthText != null) healthText.text = $"{health} / {maxHealth}";
            if (blockRoot != null) blockRoot.SetActive(block > 0);
            if (blockText != null) blockText.text = block.ToString();
            if (intentText != null) intentText.text = string.IsNullOrEmpty(intent) ? string.Empty : $"{intent} in {countdown}";
            if (ShowsBlockInRow) statusRow.SetBlock(block, true);
        }
    }
}
