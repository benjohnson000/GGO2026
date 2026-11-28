using System.Collections;
using benjohnson;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ggo2026
{
    /// <summary>
    /// The coin counter in the UI scene's top bar. Owns the run's <see cref="CoinPurse"/> until run state exists, so
    /// coins carry over between fights. Like <see cref="CombatantView"/>, the number follows playback rather than the
    /// live purse: gains appear through <see cref="ShowGain(int, Vector3, Color, ProjectileStyleSO)"/>, which flies
    /// coins from where they were earned to <see cref="Target"/> and counts each one up as it lands. Spends show at once.
    /// </summary>
    public class CoinsView : Singleton<CoinsView>
    {
        private const int DebugAmount = 10;

        [Tooltip("Coins the player starts with.")]
        [Min(0)]
        [SerializeField] private int startingCoins;

        [Tooltip("Shows the number of coins.")]
        [SerializeField] private TMP_Text countText;

        [Tooltip("Number format for the count, e.g. \"000\" pads to three digits, \"0\" doesn't pad.")]
        [SerializeField] private string numberFormat = "000";

        [Tooltip("Where flying coins land, e.g. the coin icon. Defaults to this object.")]
        [SerializeField] private Transform target;

        [Header("Pulse")]
        [Tooltip("Optional. Pops as each coin lands. Defaults to the target.")]
        [SerializeField] private Transform pulseTarget;

        [Min(1f)]
        [SerializeField] private float pulseScale = 1.25f;

        [Min(0f)]
        [SerializeField] private float pulseDuration = 0.15f;

        [Header("Audio")]
        [Tooltip("Optional. Played each time coins are added to the count: as each flying coin lands, or once for a gain shown at once.")]
        [SerializeField] private SoundProfileSO collectSound;

        [Header("Flying Coins")]
        [Tooltip("Projectile flown to the counter when the source has no style of its own (e.g. a coin tile's effect with no Projectile). " +
                 "Without either, gains show at once.")]
        [SerializeField] private ProjectileStyleSO defaultProjectile;

        [Tooltip("A gain is split over up to this many projectiles, so a big gain sends a stream of coins rather than one per coin.")]
        [Min(1)]
        [SerializeField] private int maxProjectilesPerGain = 10;

        [Tooltip("Seconds between the projectiles of one gain.")]
        [Min(0f)]
        [SerializeField] private float launchInterval = 0.06f;

        private int shown;
        private Transform pulseTransform;
        private Vector3 pulseRestScale;
        private Coroutine pulseRoutine;

        /// <summary>The run's coins. Null on a duplicate that is about to destroy itself.</summary>
        public CoinPurse Purse { get; private set; }

        /// <summary>Where flying coins land.</summary>
        public Transform Target => target != null ? target : transform;

        /// <summary>The number on screen: the purse minus coins still in flight (or not launched yet).</summary>
        public int ShownAmount => shown;

        protected override void Awake()
        {
            base.Awake();
            if (instance != this) return;

            Purse = new CoinPurse(startingCoins);
            shown = Purse.Amount;
            pulseTransform = pulseTarget != null ? pulseTarget : Target;
            pulseRestScale = pulseTransform.localScale;
            Refresh();
        }

        private void OnEnable()
        {
            if (Purse != null) Purse.Changed += OnPurseChanged;
        }

        private void OnDisable()
        {
            if (Purse != null) Purse.Changed -= OnPurseChanged;
            // A pulse cut short must not leave the icon enlarged.
            if (pulseTransform != null) pulseTransform.localScale = pulseRestScale;
            pulseRoutine = null;
        }

        /// <summary>
        /// Shows coins already added to <see cref="Purse"/>, flown from world position <paramref name="origin"/> to
        /// <see cref="Target"/> in <paramref name="style"/> (or <see cref="defaultProjectile"/> when null), tinted
        /// <paramref name="colour"/> if the style says so. The count goes up as each projectile lands. With no style or
        /// prefab, the count goes up at once.
        /// </summary>
        public void ShowGain(int amount, Vector3 origin, Color colour, ProjectileStyleSO style = null)
        {
            if (amount <= 0) return;

            if (style == null) style = defaultProjectile;
            if (style == null || style.Prefab == null || !isActiveAndEnabled)
            {
                AddShown(amount);
                return;
            }
            StartCoroutine(LaunchCoins(amount, origin, colour, style));
        }

        /// <summary>Shows coins already added to <see cref="Purse"/> at once, with no projectile.</summary>
        public void ShowGain(int amount)
        {
            if (amount > 0) AddShown(amount);
        }

        private IEnumerator LaunchCoins(int amount, Vector3 origin, Color colour, ProjectileStyleSO style)
        {
            int count = Mathf.Min(amount, maxProjectilesPerGain);
            WaitForSeconds wait = launchInterval > 0f ? new WaitForSeconds(launchInterval) : null;

            for (int i = 0; i < count; i++)
            {
                // Spread the amount evenly; the first few carry the remainder.
                int share = amount / count + (i < amount % count ? 1 : 0);
                Launch(share, origin, colour, style);
                if (wait != null && i < count - 1) yield return wait;
            }
        }

        private void Launch(int share, Vector3 origin, Color colour, ProjectileStyleSO style)
        {
            DamageProjectile projectile = style.Spawn(origin);
            // Kept in the UI scene, which stays loaded, so coins from a fight that unloads still arrive.
            SceneManager.MoveGameObjectToScene(projectile.gameObject, gameObject.scene);
            projectile.Launch(style, origin, Target, colour, () =>
            {
                if (this == null) return;
                AddShown(share);
            });
        }

        private void OnPurseChanged(int oldAmount, int newAmount)
        {
            // Gains are shown by whoever caused them, when their coins land. Spends have nothing to wait for.
            if (newAmount >= oldAmount) return;

            shown = Mathf.Max(0, shown - (oldAmount - newAmount));
            Refresh();
        }

        private void AddShown(int amount)
        {
            shown += amount;
            Refresh();
            Pulse();
            PlayCollectSound();
        }

        private void PlayCollectSound()
        {
            if (collectSound == null || !collectSound.HasClips) return;

            SFXManager sfx = SFXManager.instance;
            if (sfx != null) sfx.PlaySound(collectSound);
        }

        private void Refresh()
        {
            if (countText != null) countText.text = shown.ToString(numberFormat);
        }

        private void Pulse()
        {
            if (pulseDuration <= 0f || !isActiveAndEnabled) return;

            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            float elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                // Jumps to full size, then eases back.
                float t = elapsed / pulseDuration;
                pulseTransform.localScale = pulseRestScale * Mathf.Lerp(pulseScale, 1f, t * (2f - t));
                yield return null;
                elapsed += Time.deltaTime;
            }
            pulseTransform.localScale = pulseRestScale;
            pulseRoutine = null;
        }

        [ContextMenu("Debug: Gain 10 Coins")]
        private void DebugGain()
        {
            if (Purse == null)
            {
                Debug.LogWarning($"{name}: enter Play mode before using the debug actions.", this);
                return;
            }
            Purse.Gain(DebugAmount);
            // From the middle of the screen's world space, so the flight can be checked without a board.
            ShowGain(DebugAmount, Vector3.zero, Color.white);
        }
    }
}
