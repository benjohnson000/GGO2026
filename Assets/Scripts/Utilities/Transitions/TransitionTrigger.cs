using UnityEngine;

namespace benjohnson
{
    /// <summary>
    /// Script should be attached to an object that will trigger a scene transition: door, portal, etc.
    /// </summary>
    public class TransitionTrigger : MonoBehaviour
    {
        public string sceneName;
        public TransitionInfoSO transition;

        /// <summary>
        /// To be called from a unityEvent, triggers environment transition.
        /// </summary>
        public void TransitionToEnvironment()
        {
            if (SceneController.instance == null)
            {
                Debug.LogError("TransitionTrigger: no SceneController in the loaded scenes.", this);
                return;
            }

            SceneController.instance.LoadEnvironment(sceneName, transition);
        }
    }
}
