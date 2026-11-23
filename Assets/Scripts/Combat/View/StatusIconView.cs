using System.Collections;
using benjohnson;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ggo2026
{
    /// <summary>
    /// One icon in a <see cref="StatusRowView"/>, on the root of the row's icon prefab. The prefab decides the size,
    /// layout, font and badge; this fills in the status's sprite, colour and stacks, and outlines the icon. Pops in,
    /// pulses when its count changes and shrinks out when removed.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class StatusIconView : MonoBehaviour
    {
        [Tooltip("Gets the status's icon and colour, and an outline (add a Pixel Outline to it to tune that). A status without an icon is a square of its colour.")]
        [SerializeField] private Image iconImage;

        [Tooltip("Shows the stacks.")]
        [SerializeField] private TMP_Text countText;

        [Tooltip("Optional. Shows the status's first letter while it has no icon.")]
        [SerializeField] private TMP_Text letterText;

        private StatusRowView row;
        private Vector3 restScale;
        private float currentScale = 1f;
        private Coroutine routine;
        private int count;
        private bool removing;

        public RectTransform Rect { get; private set; }

        /// <summary>The space the icon takes in a row, from the prefab root's size and scale.</summary>
        public Vector2 Size { get; private set; }

        /// <summary>Creates a copy of <paramref name="prefab"/> under <paramref name="row"/>, showing <paramref name="status"/>.</summary>
        public static StatusIconView Create(StatusIconView prefab, StatusRowView row, StatusDataSO status)
        {
            StatusIconView view = Instantiate(prefab, row.transform, false);
            view.name = $"Status ({status.DisplayName})";
            view.Init(row, status);
            return view;
        }

        /// <summary>Shows <paramref name="newCount"/>, popping in from nothing or pulsing if it changed.</summary>
        public void SetCount(int newCount, bool animate)
        {
            if (newCount == count) return;

            bool appearing = count == 0;
            count = newCount;
            if (countText != null) countText.text = count.ToString();

            if (!animate || !isActiveAndEnabled)
            {
                SetScale(1f);
                return;
            }

            if (appearing) SetScale(0f);
            StartAnimation(1f, row.PulseScale - 1f, row.PulseDuration);
        }

        /// <summary>Shrinks the icon away, then destroys it.</summary>
        public void Remove()
        {
            if (!isActiveAndEnabled)
            {
                Destroy(gameObject);
                return;
            }

            removing = true;
            transform.SetAsFirstSibling(); // Behind the icons that move into its place.
            StartAnimation(0f, 0f, row.RemoveDuration);
        }

        private void OnDisable()
        {
            // Disabling stops coroutines, so jump to where the animation was heading.
            routine = null;
            if (removing) Destroy(gameObject);
            else if (row != null) SetScale(1f);
        }

        private void Init(StatusRowView owner, StatusDataSO status)
        {
            row = owner;
            restScale = transform.localScale;

            Rect = (RectTransform)transform;
            Size = Vector2.Scale(Rect.rect.size, restScale);
            // The row places icons by their centre, measured from its top centre.
            Rect.anchorMin = Rect.anchorMax = new Vector2(0.5f, 1f);
            Rect.pivot = new Vector2(0.5f, 0.5f);

            if (iconImage != null) SetUpIcon(status);

            if (letterText == null) return;

            string label = status.DisplayName;
            bool showLetter = status.Icon == null && !string.IsNullOrEmpty(label);
            letterText.gameObject.SetActive(showLetter);
            if (showLetter) letterText.text = label.Substring(0, 1).ToUpperInvariant();
        }

        private void SetUpIcon(StatusDataSO status)
        {
            iconImage.sprite = status.Icon;
            iconImage.color = status.Colour;

            // One added to the prefab keeps its own settings. Otherwise the defaults match the tiles' outline.
            if (!iconImage.TryGetComponent(out PixelOutline _)) iconImage.gameObject.AddComponent<PixelOutline>();
        }

        private void SetScale(float scale)
        {
            currentScale = scale;
            transform.localScale = restScale * scale;
        }

        private void StartAnimation(float targetScale, float bump, float duration)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Animate(targetScale, bump, duration));
        }

        /// <summary>Scales to the target, relative to the prefab's scale, swelling by <paramref name="bump"/> on the way.</summary>
        private IEnumerator Animate(float targetScale, float bump, float duration)
        {
            float startScale = currentScale;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = elapsed / duration;
                SetScale(Mathf.SmoothStep(startScale, targetScale, t) + Mathf.Sin(t * Mathf.PI) * bump);
                yield return null;
            }

            SetScale(targetScale);
            routine = null;
            if (removing) Destroy(gameObject);
        }
    }
}
