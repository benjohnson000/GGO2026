using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ggo2026
{
    /// <summary>
    /// A layered health bar. Put it on the bar's root, assign its references, and it works as a prefab.
    /// <para>
    /// Layers are scaled on X and stacked. Back to front: the underfill, one layer per previewed status (the first
    /// to hit at the back), then the health that will be left. Each layer only needs its end point, so any number
    /// of statuses show as bands side by side. Status layers, and the underfill if none is assigned, are copies of
    /// the health fill made at runtime.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class HealthBarView : MonoBehaviour
    {
        [Tooltip("The front fill, scaled on X. Set its pivot to the end it should shrink toward, e.g. X = 0 to shrink toward the left. Put any frame after it in the hierarchy.")]
        [SerializeField] private Image healthFill;

        [Tooltip("Optional. Your own underfill, which keeps its colour and art. Put it before the fill in the hierarchy, with the same rect and pivot. Without it, a copy of the fill in the settings' Damage Colour is used.")]
        [SerializeField] private Image underfill;

        [Tooltip("Optional. Shows \"health / max health\".")]
        [SerializeField] private TMP_Text healthText;

        [Tooltip("Optional. The block icon and number's root, shown only while block is above 0. Put it after the fill in the hierarchy.")]
        [SerializeField] private GameObject blockRoot;

        [Tooltip("Optional. Shows the amount of block.")]
        [SerializeField] private TMP_Text blockText;

        [Header("Block Animation")]
        [Tooltip("Optional. The whole block icon, under Block Root. Floats down and fades in when block is gained from 0, and is hidden while the broken halves play. The break needs this and both halves.")]
        [SerializeField] private GameObject blockIcon;

        [Tooltip("Optional. Left half of the broken block icon, under Block Root and placed over the whole icon. Hidden until a hit breaks the block, then slides left and fades.")]
        [SerializeField] private Graphic brokenBlockLeft;

        [Tooltip("Optional. Right half of the broken block icon. Slides right and fades.")]
        [SerializeField] private Graphic brokenBlockRight;

        [Tooltip("Optional. Colours and timings (Create > GGO2026 > Health Bar Settings). Without it, the defaults are used.")]
        [SerializeField] private HealthBarSettingsSO settings;

        private readonly List<Image> statusFills = new List<Image>();

        private Color healthColour;
        private HealthBarSettingsSO activeSettings;
        private HealthBarModel model;
        private int maxHealth;
        private Coroutine settleRoutine;
        private Coroutine breakRoutine;
        private bool breakInterrupted;
        private Vector2 leftRest;
        private Vector2 rightRest;
        private Coroutine appearRoutine;
        private bool appearInterrupted;
        private RectTransform blockIconRect;
        private Graphic blockIconGraphic;
        private Vector2 iconRest;
        // The block last shown; -1 after a reset, so block that is already there appears without animating.
        private int shownBlock = -1;

        private bool CanBreak => blockRoot != null && blockIcon != null && brokenBlockLeft != null && brokenBlockRight != null;


        /// <summary>Starts showing a combatant at <paramref name="health"/>, with no underfill. Call before <see cref="Show"/>.</summary>
        public void ResetBar(int health, int maxHealth)
        {
            this.maxHealth = maxHealth;

            if (healthFill == null)
            {
                Debug.LogError($"{name}: HealthBarView needs a Health Fill assigned.", this);
                return;
            }

            // Not in Awake, which hasn't run yet if the bar starts inactive.
            if (model == null) Build();
            StopSettling();
            StopBreak();
            StopAppear();
            shownBlock = -1;
            model.Reset(health);
        }

        /// <summary>
        /// A hit has just taken the block to 0: the icon splits into its halves, which slide apart and fade, and the
        /// block is then hidden. Call before <see cref="Show"/> with the new block. Does nothing if no block was shown,
        /// or without the icon and both halves assigned.
        /// </summary>
        public void BreakBlock()
        {
            if (model == null || !CanBreak || !blockRoot.activeSelf || !isActiveAndEnabled) return;
            if (activeSettings.BlockBreakDuration <= 0f) return;

            StopAppear();
            StopBreak();
            breakRoutine = StartCoroutine(AnimateBreak());
        }

        /// <summary>Shows new values straight away. The underfill stays until <see cref="Settle"/>.</summary>
        /// <param name="preview">Null shows no status bands.</param>
        public void Show(int health, int block, DamagePreview preview)
        {
            if (healthText != null) healthText.text = $"{health} / {maxHealth}";
            // New block cancels a break; so does one cut short by disabling the bar. A break in progress keeps the root.
            if (block > 0 || breakRoutine == null) StopBreak();
            // Losing block, or being disabled, ends an appear. More block while it plays lets it finish.
            if (block == 0 || appearInterrupted) StopAppear();
            if (blockRoot != null) blockRoot.SetActive(block > 0 || breakRoutine != null);
            if (blockText != null) blockText.text = block.ToString();
            if (shownBlock == 0 && block > 0) StartAppear();
            shownBlock = block;
            if (model == null) return;

            model.SetHealth(health);
            if (!model.IsSettling) StopSettling();

            int end = health;
            int count = preview != null ? preview.Hits.Count : 0;
            for (int i = 0; i < count; i++)
            {
                if (i == statusFills.Count) statusFills.Add(CreateLayer($"Status Fill {i}"));

                PreviewedDamage hit = preview.Hits[i];
                statusFills[i].color = hit.Status.BarColour;
                SetEnd(statusFills[i], end);
                end -= hit.HealthLost;
            }
            for (int i = count; i < statusFills.Count; i++) SetEnd(statusFills[i], 0f);

            healthFill.color = block > 0 ? activeSettings.BlockedHealthColour : healthColour;
            SetEnd(healthFill, end);
            DrawUnderfill();
        }

        /// <summary>Effects have resolved, so the underfill can catch up.</summary>
        public void Settle()
        {
            if (model == null) return;

            model.Settle();
            StartSettling();
        }

        private void OnEnable()
        {
            // Disabling stops coroutines, so pick up any drain it interrupted.
            if (model != null) StartSettling();
        }

        private void OnDisable()
        {
            settleRoutine = null;

            // Children can't be activated or deactivated while this is being disabled, so the next Show tidies up.
            if (breakRoutine != null) breakInterrupted = true;
            breakRoutine = null;
            if (appearRoutine != null) appearInterrupted = true;
            appearRoutine = null;
        }

        private void OnDestroy()
        {
            // Only the defaults created in Build, never the assigned asset.
            if (activeSettings != null && activeSettings != settings) Destroy(activeSettings);
        }

        private void Build()
        {
            healthColour = healthFill.color;
            activeSettings = settings != null ? settings : ScriptableObject.CreateInstance<HealthBarSettingsSO>();
            model = new HealthBarModel(activeSettings.HoldDuration, activeSettings.DrainDuration);

            if (CanBreak)
            {
                leftRest = brokenBlockLeft.rectTransform.anchoredPosition;
                rightRest = brokenBlockRight.rectTransform.anchoredPosition;
                brokenBlockLeft.gameObject.SetActive(false);
                brokenBlockRight.gameObject.SetActive(false);
            }

            if (blockIcon != null)
            {
                blockIconRect = blockIcon.transform as RectTransform;
                blockIconGraphic = blockIcon.GetComponent<Graphic>();
                if (blockIconRect != null) iconRest = blockIconRect.anchoredPosition;
            }

            if (underfill == null)
            {
                underfill = CreateLayer("Underfill");
                underfill.color = activeSettings.DamageColour;
                return;
            }

            Transform under = underfill.transform;
            Transform fill = healthFill.transform;
            if (under.parent != fill.parent || under.GetSiblingIndex() >= fill.GetSiblingIndex())
            {
                Debug.LogWarning($"{name}: the Underfill should be a sibling of the Health Fill and before it in the hierarchy, or it draws over the other layers.", this);
            }
        }

        /// <summary>Copies the fill's rect and art into a new layer directly behind it, in front of earlier layers.</summary>
        private Image CreateLayer(string layerName)
        {
            GameObject go = new GameObject(layerName, typeof(RectTransform));
            go.layer = healthFill.gameObject.layer;

            RectTransform source = healthFill.rectTransform;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(source.parent, false);
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.anchoredPosition3D = source.anchoredPosition3D;
            rect.sizeDelta = source.sizeDelta;
            rect.localRotation = source.localRotation;
            rect.localScale = source.localScale;
            rect.SetSiblingIndex(source.GetSiblingIndex());

            Image layer = go.AddComponent<Image>();
            layer.sprite = healthFill.sprite;
            layer.type = healthFill.type;
            layer.material = healthFill.material;
            layer.pixelsPerUnitMultiplier = healthFill.pixelsPerUnitMultiplier;
            layer.maskable = healthFill.maskable;
            layer.raycastTarget = false;
            return layer;
        }

        private void SetEnd(Image layer, float value)
        {
            Transform fill = layer.transform;
            Vector3 scale = fill.localScale;
            scale.x = maxHealth > 0 ? Mathf.Clamp01(value / maxHealth) : 0f;
            fill.localScale = scale;
        }

        private void DrawUnderfill()
        {
            // Empty when it has caught up, so it can't fringe around the layers in front.
            SetEnd(underfill, model.Underfill > model.Health ? model.Underfill : 0f);
        }

        private void StartSettling()
        {
            if (model.IsSettling && settleRoutine == null && isActiveAndEnabled) settleRoutine = StartCoroutine(AnimateSettle());
        }

        private void StopSettling()
        {
            if (settleRoutine != null) StopCoroutine(settleRoutine);
            settleRoutine = null;
        }

        private void StartAppear()
        {
            StopAppear();
            if (blockRoot == null || blockIconRect == null || !isActiveAndEnabled) return;
            if (activeSettings.BlockAppearDuration <= 0f) return;

            appearRoutine = StartCoroutine(AnimateAppear());
        }

        /// <summary>Ends any appear straight away, with the icon in place and the number showing.</summary>
        private void StopAppear()
        {
            bool wasAppearing = appearRoutine != null || appearInterrupted;
            if (appearRoutine != null) StopCoroutine(appearRoutine);
            appearRoutine = null;
            appearInterrupted = false;
            if (wasAppearing) FinishAppear();
        }

        private void FinishAppear()
        {
            PlaceIcon(0f, 1f);
            if (BlockTextHides) blockText.gameObject.SetActive(true);
        }

        private IEnumerator AnimateAppear()
        {
            if (BlockTextHides) blockText.gameObject.SetActive(false);

            float duration = activeSettings.BlockAppearDuration;
            float height = activeSettings.BlockAppearHeight;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = elapsed / duration;
                // Ease out: it drops in quickly and settles into place.
                float eased = 1f - (1f - t) * (1f - t);
                PlaceIcon((1f - eased) * height, t);
                yield return null;
            }

            appearRoutine = null;
            FinishAppear();
        }

        private void PlaceIcon(float height, float alpha)
        {
            if (blockIconRect != null) blockIconRect.anchoredPosition = iconRest + Vector2.up * height;
            // The CanvasRenderer's alpha, so the icon's own colour is kept.
            if (blockIconGraphic != null) blockIconGraphic.canvasRenderer.SetAlpha(alpha);
        }

        /// <summary>Ends any break straight away and puts the whole icon back. The caller decides whether the root shows.</summary>
        private void StopBreak()
        {
            bool wasBreaking = breakRoutine != null || breakInterrupted;
            if (breakRoutine != null) StopCoroutine(breakRoutine);
            breakRoutine = null;
            breakInterrupted = false;
            if (wasBreaking) RestoreBlockIcon();
        }

        private void RestoreBlockIcon()
        {
            if (!CanBreak) return;

            PlaceHalves(0f, 1f);
            brokenBlockLeft.gameObject.SetActive(false);
            brokenBlockRight.gameObject.SetActive(false);
            blockIcon.SetActive(true);
            if (BlockTextHides) blockText.gameObject.SetActive(true);
        }

        // Not when the text is the root itself, which has to stay active for the halves.
        private bool BlockTextHides => blockText != null && blockText.gameObject != blockRoot;

        private IEnumerator AnimateBreak()
        {
            blockIcon.SetActive(false);
            if (BlockTextHides) blockText.gameObject.SetActive(false);
            brokenBlockLeft.gameObject.SetActive(true);
            brokenBlockRight.gameObject.SetActive(true);

            float duration = activeSettings.BlockBreakDuration;
            float distance = activeSettings.BlockBreakDistance;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = elapsed / duration;
                // Ease out: the halves burst apart, then slow as they fade.
                float eased = 1f - (1f - t) * (1f - t);
                PlaceHalves(eased * distance, 1f - t);
                yield return null;
            }

            breakRoutine = null;
            RestoreBlockIcon();
            blockRoot.SetActive(false);
        }

        private void PlaceHalves(float offset, float alpha)
        {
            brokenBlockLeft.rectTransform.anchoredPosition = leftRest + Vector2.left * offset;
            brokenBlockRight.rectTransform.anchoredPosition = rightRest + Vector2.right * offset;
            // The CanvasRenderer's alpha, so the halves' own colours are kept.
            brokenBlockLeft.canvasRenderer.SetAlpha(alpha);
            brokenBlockRight.canvasRenderer.SetAlpha(alpha);
        }

        private IEnumerator AnimateSettle()
        {
            while (model.IsSettling)
            {
                yield return null;
                model.Advance(Time.deltaTime);
                DrawUnderfill();
            }
            settleRoutine = null;
        }
    }
}
