using System.Collections;
using UnityEngine;

namespace benjohnson
{
    public abstract class TransitionBehavior : MonoBehaviour
    {
        public abstract IEnumerator Trigger(float duration, Color color);
    }
}
