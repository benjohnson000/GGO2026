using UnityEngine;

namespace benjohnson
{
    /// <summary>
    /// Gently bobs an object up and down on a sine wave around its starting local position.
    /// Put it on a parent of anything that is moved by other code, so the two don't fight.
    /// </summary>
    public class Bob : MonoBehaviour
    {
        [Tooltip("Distance above and below the start position, in local units.")]
        [SerializeField] private float amplitude = 0.1f;
        [Tooltip("Seconds for one full up-and-down cycle.")]
        [SerializeField] private float period = 3f;
        [Tooltip("Randomly lengthens or shortens the period by up to this fraction (0.1 = ±10%), picked once on Awake.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float periodVariance = 0.1f;
        [Tooltip("Start each object at a random point in the cycle so several of them don't move in sync.")]
        [SerializeField] private bool randomizePhase = true;

        private Vector3 startPosition;
        private float phase;
        private float actualPeriod;

        private void Awake()
        {
            startPosition = transform.localPosition;
            actualPeriod = period * (1f + Random.Range(-periodVariance, periodVariance));
            if (randomizePhase) phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            if (actualPeriod <= 0f) return;

            float y = Mathf.Sin(Time.time * Mathf.PI * 2f / actualPeriod + phase) * amplitude;
            transform.localPosition = startPosition + new Vector3(0f, y, 0f);
        }

        private void OnDisable()
        {
            transform.localPosition = startPosition;
        }
    }
}
