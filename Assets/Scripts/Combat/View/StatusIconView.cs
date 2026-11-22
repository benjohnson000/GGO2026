using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ggo2026
{
    /// <summary>
    /// One icon in a <see cref="StatusRowView"/>: a sprite (or a lettered square without one) and a stack badge.
    /// Built in code so no prefab is needed. Pops in, pulses when its count changes and shrinks out when removed.
    /// </summary>
    public class StatusIconView : MonoBehaviour
    {
        // Proportions of the icon's size.
        private const float LetterSize = 0.65f;
        private const float BadgeHeight = 0.48f;
        private const float BadgeOverhang = 0.12f;
        private const float DigitWidth = 0.62f; // Of the badge's height.

        private StatusRowView row;
        private RectTransform badge;
        private TMP_Text countText;
        private Coroutine routine;
        private int count;
        private bool removing;

        public RectTransform Rect { get; private set; }

        /// <summary>Creates an icon under <paramref name="row"/>, which supplies the size, colours, font and timings.</summary>
        public static StatusIconView Create(StatusRowView row, string label, Sprite sprite, Color colour)
        {
            RectTransform rect = CreateRect($"Status ({label})", row.transform);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.sizeDelta = Vector2.one * row.IconSize;

            StatusIconView view = rect.gameObject.AddComponent<StatusIconView>();
            view.Rect = rect;
            view.row = row;
            view.Build(label, sprite, colour);
            return view;
        }

        /// <summary>Shows <paramref name="newCount"/>, popping in from nothing or pulsing if it changed.</summary>
        public void SetCount(int newCount, bool animate)
        {
            if (newCount == count) return;

            bool appearing = count == 0;
            count = newCount;
            countText.text = count.ToString();

            float height = row.IconSize * BadgeHeight;
            badge.sizeDelta = new Vector2(Mathf.Max(height, height * DigitWidth * (countText.text.Length + 0.5f)), height);

            if (!animate || !isActiveAndEnabled)
            {
                transform.localScale = Vector3.one;
                return;
            }

            if (appearing) transform.localScale = Vector3.zero;
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
            else transform.localScale = Vector3.one;
        }

        private void Build(string label, Sprite sprite, Color colour)
        {
            Image image = CreateImage("Icon", transform, colour);
            image.sprite = sprite;
            image.preserveAspect = true;
            Stretch(image.rectTransform);

            if (sprite == null && !string.IsNullOrEmpty(label))
            {
                TMP_Text letter = CreateText("Letter", image.transform, row.IconSize * LetterSize, row.BadgeColour);
                letter.text = label.Substring(0, 1).ToUpperInvariant();
            }

            Image badgeImage = CreateImage("Stacks", transform, row.BadgeColour);
            badge = badgeImage.rectTransform;
            badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(1f, 0f);
            badge.anchoredPosition = new Vector2(1f, -1f) * (row.IconSize * BadgeOverhang);

            countText = CreateText("Count", badge, row.IconSize * BadgeHeight, row.CountColour);
        }

        private void StartAnimation(float targetScale, float bump, float duration)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Animate(targetScale, bump, duration));
        }

        /// <summary>Scales to the target, swelling by <paramref name="bump"/> on the way.</summary>
        private IEnumerator Animate(float targetScale, float bump, float duration)
        {
            float startScale = transform.localScale.x;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = elapsed / duration;
                float scale = Mathf.SmoothStep(startScale, targetScale, t) + Mathf.Sin(t * Mathf.PI) * bump;
                transform.localScale = Vector3.one * scale;
                yield return null;
            }

            transform.localScale = Vector3.one * targetScale;
            routine = null;
            if (removing) Destroy(gameObject);
        }

        private static Image CreateImage(string objectName, Transform parent, Color colour)
        {
            Image image = CreateRect(objectName, parent).gameObject.AddComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
            return image;
        }

        private TMP_Text CreateText(string objectName, Transform parent, float size, Color colour)
        {
            TextMeshProUGUI text = CreateRect(objectName, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (row.Font != null) text.font = row.Font;
            text.fontSize = size;
            text.color = colour;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            Stretch(text.rectTransform);
            return text;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            child.layer = parent.gameObject.layer;

            RectTransform rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
