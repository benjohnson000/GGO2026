using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace benjohnson
{
    /// <summary>Fades the screen image from opaque to transparent (the "curtains open").</summary>
    public class FadeOpenTransition : TransitionBehavior
    {
        [SerializeField] private Image screen;

        public override IEnumerator Trigger(float duration, Color color)
        {
            float time = duration;
            while (time > 0f)
            {
                // Unscaled so transitions still play while the game is paused (timeScale 0)
                time -= Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(time / duration);
                screen.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            screen.color = new Color(color.r, color.g, color.b, 0f);
        }
    }
}
