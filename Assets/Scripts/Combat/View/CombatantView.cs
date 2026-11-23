using benjohnson;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace ggo2026
{
    /// <summary>
    /// Placeholder UI for one <see cref="Combatant"/>: name, health bar with block, and a row of status icons.
    /// It shows the values from the events it is given, not the live combatant, so it stays in step with playback.
    /// Every reference is optional.
    /// </summary>
    public class CombatantView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;

        [Tooltip("The bar, health number and block. Its references and settings are assigned on the HealthBarView itself.")]
        [SerializeField] private HealthBarView healthBar;

        [Tooltip("Optional. Where status icons go. Without it, statuses aren't shown.")]
        [SerializeField] private StatusRowView statusRow;

        [Tooltip("Enemy only: the telegraphed intent and its countdown, e.g. \"Attack 6 in 3\". Empty while there is no intent.")]
        [SerializeField] private TMP_Text intentText;

        [Tooltip("Optional. Where projectiles from tiles land, e.g. the portrait. Defaults to this object's pivot.")]
        [SerializeField] private Transform hitPoint;

        [Header("Hit Sounds")]
        [Tooltip("Optional. Played when a hit costs this combatant health, as the hit is shown.")]
        [FormerlySerializedAs("damageSound")]
        [SerializeField] private SoundProfileSO damageUnblockedSound;

        [Tooltip("Optional. Played when block absorbs a hit and some block is left.")]
        [SerializeField] private SoundProfileSO damageBlockedSound;

        [Tooltip("Optional. Played when a hit takes block to 0. A hit that also gets through to health plays the unblocked sound too.")]
        [SerializeField] private SoundProfileSO shieldBrokenSound;

        [Tooltip("Optional. Flashed when this combatant loses health, as the hit is shown. Defaults to a GraphicFlash on the hit point, i.e. the portrait. Its colour and duration are set on the GraphicFlash.")]
        [SerializeField] private GraphicFlash portraitFlash;

        private int health;
        private int maxHealth;
        private int block;
        private bool dead;
        private string intent;
        private int countdown;
        private DamagePreview preview;

        public Combatant Combatant { get; private set; }

        /// <summary>Where projectiles from tiles aimed at this combatant land.</summary>
        public Transform HitPoint => hitPoint != null ? hitPoint : transform;


        /// <summary>Shows <paramref name="combatant"/> as it is right now.</summary>
        /// <param name="damagePreview">Its current <see cref="Combat.PreviewDamage"/>, for the health bar.</param>
        public void Bind(Combatant combatant, DamagePreview damagePreview)
        {
            Combatant = combatant;
            preview = damagePreview;
            health = combatant.Health;
            maxHealth = combatant.MaxHealth;
            block = combatant.Block;
            dead = combatant.IsDead;
            intent = null;
            countdown = combatant.IntentCountdown;

            if (statusRow != null)
            {
                statusRow.ResetRow();
                // What is already active appears without animating.
                foreach (StatusEffect status in combatant.Statuses) statusRow.SetStatus(status.Data, status.Stacks, false);
            }

            if (healthBar != null) healthBar.ResetBar(health, maxHealth);

            Refresh();
        }

        /// <summary>Updates the display from one event. An event about the other combatant can only change this one's damage preview.</summary>
        public void Apply(CombatEvent combatEvent)
        {
            if (Combatant == null) return;

            DamagePreview newPreview = combatEvent.GetPreview(Combatant);
            if (newPreview != null) preview = newPreview;

            if (combatEvent.Target != Combatant)
            {
                RefreshHealthBar();
                return;
            }

            switch (combatEvent)
            {
                case DamageEvent damage:
                    health = damage.HealthAfter;
                    block = damage.BlockAfter;
                    PlayHitFeedback(damage);
                    break;
                case HealedEvent healed:
                    health = healed.HealthAfter;
                    break;
                case BlockChangedEvent blockChanged:
                    block = blockChanged.NewBlock;
                    break;
                case StatusChangedEvent status:
                    if (statusRow != null) statusRow.SetStatus(status.Status, status.NewStacks, true);
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
                    RefreshHealthBar();
                    return;
            }

            Refresh();
        }

        /// <summary>Effects have resolved: lets the health bar's underfill catch up.</summary>
        public void Settle()
        {
            if (healthBar != null) healthBar.Settle();
        }

        /// <summary>
        /// Sounds, the block break and the portrait flash for one hit. Each sound stands for one thing that happened, so
        /// a hit that breaks the block and gets through to health plays both of those sounds.
        /// </summary>
        private void PlayHitFeedback(DamageEvent damage)
        {
            if (damage.Blocked > 0)
            {
                if (damage.BlockAfter == 0)
                {
                    // Only a hit breaks the block. Block lost at turn start just disappears.
                    PlaySound(shieldBrokenSound);
                    if (healthBar != null) healthBar.BreakBlock();
                }
                else
                {
                    PlaySound(damageBlockedSound);
                }
            }

            if (damage.HealthLost > 0)
            {
                PlaySound(damageUnblockedSound);
                Flash();
            }
        }

        /// <summary>Flashes the portrait, if it has a <see cref="GraphicFlash"/>.</summary>
        private void Flash()
        {
            if (portraitFlash == null && hitPoint != null) portraitFlash = hitPoint.GetComponent<GraphicFlash>();
            if (portraitFlash != null) portraitFlash.Flash();
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
            RefreshHealthBar();
            if (intentText != null) intentText.text = string.IsNullOrEmpty(intent) ? string.Empty : $"{intent} in {countdown}";
        }

        private void RefreshHealthBar()
        {
            if (healthBar != null) healthBar.Show(health, block, preview);
        }
    }
}
