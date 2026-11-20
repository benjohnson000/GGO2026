using System.Collections;
using UnityEngine;

namespace benjohnson
{
    /// <summary>
    /// Should be attached to a canvas component, responsible for instantiating transition prefabs.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class TransitionController : Singleton<TransitionController>
    {
        public IEnumerator Transition(TransitionInfoSO transition, IEnumerator switchSceneCallback)
        {
            // Transition close
            TransitionBehavior tb = Instantiate(transition.close.behavior, transform);
            try
            {
                yield return StartCoroutine(tb.Trigger(transition.close.duration, transition.close.color));

                // Switch scene
                yield return StartCoroutine(switchSceneCallback);
            }
            finally
            {
                // Destroy transition close (also if the scene switch throws, so the screen isn't left covered)
                Destroy(tb.gameObject);
            }

            // Transition open
            tb = Instantiate(transition.open.behavior, transform);
            try
            {
                yield return StartCoroutine(tb.Trigger(transition.open.duration, transition.open.color));
            }
            finally
            {
                Destroy(tb.gameObject);
            }
        }
    }
}
