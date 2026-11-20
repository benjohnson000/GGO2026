using UnityEngine;

namespace benjohnson
{
    [CreateAssetMenu(menuName = "Transition", fileName = "new TransitionInfo")]
    public class TransitionInfoSO : ScriptableObject
    {
        public Transition close;
        public Transition open;

        [System.Serializable]
        public class Transition
        {
            public TransitionBehavior behavior;
            public Color color = Color.black;
            public float duration = 1f;
        }
    }
}
