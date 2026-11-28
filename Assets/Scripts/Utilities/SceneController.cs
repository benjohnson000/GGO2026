using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace benjohnson
{
    /// <summary>
    /// Responsible for loading/unloading any utility scenes: camera, objectPooling, etc.
    /// Responsible for transitioning to new environment scenes.
    /// </summary>
    public class SceneController : Singleton<SceneController>
    {
        [SerializeField] private TransitionInfoSO defaultTransition;

        [Header("Startup")]
        [SerializeField] private string[] startupUtilityScenes = { "Camera", "GameUtilities", "UI" };
        [SerializeField] private string startupEnvironmentScene = "Game";

        private string currentEnvironmentScene;
        private bool transitioning;

        private void Start()
        {
            foreach (string utility in startupUtilityScenes)
                LoadUtility(utility);

            if (!string.IsNullOrEmpty(startupEnvironmentScene))
                LoadEnvironment(startupEnvironmentScene);
        }

        /// <summary>
        /// Loads a utility scene additively if not already loaded.
        /// </summary>
        public void LoadUtility(string sceneName)
        {
            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        /// <summary>
        /// Unloads a utility scene if it's loaded and isn't the current environment scene.
        /// </summary>
        public void UnloadUtility(string sceneName)
        {
            if (sceneName == currentEnvironmentScene) return;
            if (!SceneManager.GetSceneByName(sceneName).isLoaded) return;

            SceneManager.UnloadSceneAsync(sceneName);
        }

        /// <summary>
        /// SHOULD BE CALLED WHEN CHANGING ENVIRONMENT SCENES, TRIGGERS TRANSITIONS
        /// Loads a new environment scene and unloads the current one if present.
        /// </summary>
        public void LoadEnvironment(string sceneName, TransitionInfoSO transitionInfo = null)
        {
            if (transitioning) return;
            if (transitionInfo == null) transitionInfo = defaultTransition;
            StartCoroutine(TransitionToScene(sceneName, transitionInfo));
        }

        /// <summary>
        /// LoadEnvironment helper method, triggers transition and switch scene coroutines
        /// </summary>
        private IEnumerator TransitionToScene(string sceneName, TransitionInfoSO transitionInfo)
        {
            transitioning = true;
            try
            {
                yield return StartCoroutine(
                    TransitionController.instance.Transition(
                        transitionInfo,
                        SwitchEnvironmentScene(sceneName)
                    )
                );
            }
            finally
            {
                // Always release the lock, even if the transition or scene load fails
                transitioning = false;
            }
        }

        /// <summary>
        /// Should be called during scene transition, switches scene "while curtains are closed"
        /// </summary>
        private IEnumerator SwitchEnvironmentScene(string newScene)
        {
            // Unload current environment if it exists
            if (currentEnvironmentScene != null)
            {
                var unload = SceneManager.UnloadSceneAsync(currentEnvironmentScene);
                if (unload != null)
                {
                    while (!unload.isDone)
                        yield return null;
                }
                currentEnvironmentScene = null;
            }

            // Load new environment scene
            var load = SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"SceneController: could not load scene '{newScene}'. Is it in Build Settings?");
                yield break;
            }

            while (!load.isDone)
                yield return null;

            currentEnvironmentScene = newScene;
        }
    }
}
