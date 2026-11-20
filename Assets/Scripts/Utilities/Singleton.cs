using UnityEngine;

namespace benjohnson
{
    /// <summary>
    /// Scene-based singleton. The first instance to wake up wins; any duplicate destroys its own GameObject.
    /// Subclasses that override Awake MUST call base.Awake().
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        [Tooltip("Call DontDestroyOnLoad on this object. Only works on root objects; not needed for additively loaded persistent scenes.")]
        [SerializeField] private bool dontDestroyOnLoad;

        public static T instance { get; private set; }

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = (T)this;

            if (dontDestroyOnLoad)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
