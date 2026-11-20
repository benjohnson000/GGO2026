using UnityEngine;
using UnityEngine.SceneManagement;

namespace benjohnson
{
    /// <summary>
    /// Responsible for loading the persistent scene (build index 0) if it is not loaded.
    /// Lets you press Play from any scene in the editor.
    /// </summary>
    public class PersistenceCheck : MonoBehaviour
    {
        private void Awake()
        {
            if (SceneController.instance == null) SceneManager.LoadScene(0);
        }
    }
}
