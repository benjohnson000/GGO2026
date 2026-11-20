using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace benjohnson
{
    /// <summary>Scales an Image along one axis to wipe the screen closed or open.</summary>
    [RequireComponent(typeof(Image))]
    public class WipeTransition : TransitionBehavior
    {
        private enum Type { open, close }
        [SerializeField] private Type type;

        private enum Direction { horizontal, vertical }
        [SerializeField] private Direction direction;

        public override IEnumerator Trigger(float duration, Color color)
        {
            GetComponent<Image>().color = color;

            float from = (type == Type.open) ? 1f : 0f;
            float to = 1f - from;

            Vector3 startScale = Vector3.one;
            Vector3 endScale = Vector3.one;
            if (direction == Direction.horizontal)
            {
                startScale.x = from;
                endScale.x = to;
            }
            else
            {
                startScale.y = from;
                endScale.y = to;
            }

            transform.localScale = startScale;

            float time = 0f;
            while (time < duration)
            {
                // Unscaled so transitions still play while the game is paused (timeScale 0)
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            transform.localScale = endScale;
        }
    }
}
