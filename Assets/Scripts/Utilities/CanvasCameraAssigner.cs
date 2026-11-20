using System.Collections;
using UnityEngine;

namespace benjohnson
{
    /// <summary>
    /// Responsible for assigning the camera component of canvas.
    /// The camera may live in an additively loaded scene, so this waits until Camera.main exists.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class CanvasCameraAssigner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            Canvas canvas = GetComponent<Canvas>();
            while (Camera.main == null)
                yield return null;

            canvas.worldCamera = Camera.main;
        }
    }
}
