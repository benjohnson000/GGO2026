using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Every so often sends a <see cref="Bird"/> in from off screen to a random free landing point.
    /// Landing points are the listed transforms, or this object's children if the list is empty. A point holds one bird
    /// at a time.
    /// </summary>
    public class BirdSpawner : MonoBehaviour
    {
        [Tooltip("Each arrival picks one of these at random.")]
        [SerializeField] private Bird[] birdPrefabs;

        [Tooltip("Where birds land (their feet). Leave empty to use this object's children.")]
        [SerializeField] private Transform[] landingPoints;

        [Tooltip("Used to work out where off screen is. Waits for Camera.main if left empty.")]
        [SerializeField] private Camera viewCamera;

        [Tooltip("Seconds (min, max) between bird arrivals.")]
        [SerializeField] private Vector2 spawnIntervalSeconds = new Vector2(3f, 8f);

        [Tooltip("Seconds (min, max) a bird stays perched before flying away, before BirdFlock.BonusPerchSeconds.")]
        [SerializeField] private Vector2 perchSeconds = new Vector2(4f, 10f);

        [Tooltip("Most birds in the scene at once, before BirdFlock.BonusMaxBirds. Never more than the landing points.")]
        [Min(0)]
        [SerializeField] private int maxBirds = 3;

        private Bird[] occupants;
        private readonly List<int> freePoints = new List<int>();

        private void Awake()
        {
            if (landingPoints == null || landingPoints.Length == 0)
            {
                landingPoints = new Transform[transform.childCount];
                for (int i = 0; i < landingPoints.Length; i++)
                    landingPoints[i] = transform.GetChild(i);
            }

            occupants = new Bird[landingPoints.Length];

            if (birdPrefabs == null || birdPrefabs.Length == 0)
                Debug.LogError($"BirdSpawner '{name}' has no bird prefabs assigned.", this);
            else if (System.Array.IndexOf(birdPrefabs, null) >= 0)
                Debug.LogWarning($"BirdSpawner '{name}' has an empty slot in its bird prefabs; it will be skipped.", this);
            if (landingPoints.Length == 0) Debug.LogWarning($"BirdSpawner '{name}' has no landing points.", this);
        }

        private void OnEnable()
        {
            if (birdPrefabs == null || birdPrefabs.Length == 0 || landingPoints.Length == 0) return;
            StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            // Camera.main lives in the additively loaded Camera scene, which may not be loaded yet.
            while (viewCamera == null)
            {
                viewCamera = Camera.main;
                if (viewCamera == null) yield return null;
            }

            while (true)
            {
                yield return new WaitForSeconds(Random.Range(spawnIntervalSeconds.x, spawnIntervalSeconds.y));
                TrySpawn();
            }
        }

        private void TrySpawn()
        {
            // The camera can be unloaded with its scene, so find the current one again rather than fail.
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;

            int liveBirds = 0;
            freePoints.Clear();
            for (int i = 0; i < occupants.Length; i++)
            {
                if (occupants[i] != null) liveBirds++;
                else if (landingPoints[i] != null) freePoints.Add(i);
            }

            if (liveBirds >= maxBirds + BirdFlock.BonusMaxBirds || freePoints.Count == 0) return;

            Bird prefab = birdPrefabs[Random.Range(0, birdPrefabs.Length)];
            if (prefab == null) return;

            int index = freePoints[Random.Range(0, freePoints.Count)];
            Vector3 landAt = landingPoints[index].position;
            Bird bird = Instantiate(prefab, landAt, Quaternion.identity);
            occupants[index] = bird;
            float perch = Mathf.Max(0f, Random.Range(perchSeconds.x, perchSeconds.y) + BirdFlock.BonusPerchSeconds);
            bird.Land(landAt, perch, viewCamera, () => FreePoint(index, bird));
        }

        private void FreePoint(int index, Bird bird)
        {
            if (occupants != null && occupants[index] == bird) occupants[index] = null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            if (landingPoints != null && landingPoints.Length > 0)
            {
                foreach (Transform point in landingPoints)
                    if (point != null) Gizmos.DrawWireSphere(point.position, 0.2f);
            }
            else
            {
                foreach (Transform child in transform)
                    Gizmos.DrawWireSphere(child.position, 0.2f);
            }
        }
    }
}
