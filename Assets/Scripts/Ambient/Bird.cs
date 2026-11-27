using System;
using System.Collections;
using benjohnson;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ggo2026
{
    /// <summary>
    /// An ambient bird that flies in from off screen to a landing point, perches for a while, then flies off screen in
    /// a random direction and destroys itself. Spawned by <see cref="BirdSpawner"/>.
    /// The root's position is the bird's feet; the art sits on a child <see cref="SpriteRenderer"/>.
    /// Assumes an orthographic camera.
    /// </summary>
    public class Bird : MonoBehaviour
    {
        // Slowest the bird gets while gliding in to land, as a fraction of flySpeed, so it never stalls.
        private const float MinApproachSpeed = 0.2f;

        [Tooltip("The bird's art. Found in children if left empty.")]
        [SerializeField] private SpriteRenderer gfx;

        [Tooltip("Tick if the art faces right when not flipped.")]
        [SerializeField] private bool spriteFacesRight = true;

        [Header("Sprites (optional)")]
        [Tooltip("Frames cycled while flying. Leave empty to keep the prefab's sprite.")]
        [SerializeField] private Sprite[] flapFrames;

        [Min(0.01f)]
        [SerializeField] private float flapFramesPerSecond = 12f;

        [Tooltip("Frames cycled while perched. Leave empty to keep the prefab's sprite.")]
        [SerializeField] private Sprite[] idleFrames;

        [Min(0.01f)]
        [SerializeField] private float idleFramesPerSecond = 6f;

        [Header("Flight")]
        [Tooltip("Units per second.")]
        [Min(0.01f)]
        [SerializeField] private float flySpeed = 6f;

        [Tooltip("Within this distance of the landing point the bird slows down to land. 0 lands at full speed.")]
        [Min(0f)]
        [SerializeField] private float landingSlowRadius = 2f;

        [Tooltip("Angles (degrees, 0 = right, 90 = up) the bird can arrive from, measured from the landing point.")]
        [SerializeField] private Vector2 arriveAngleRange = new Vector2(20f, 160f);

        [Tooltip("Angles (degrees, 0 = right, 90 = up) the bird can leave towards.")]
        [SerializeField] private Vector2 leaveAngleRange = new Vector2(10f, 170f);

        [Tooltip("Extra distance past the screen edge to start and finish flights, so the sprite is fully hidden.")]
        [Min(0f)]
        [SerializeField] private float offscreenMargin = 1f;

        [Header("Perched")]
        [Tooltip("Seconds (min, max) between turning to look the other way while perched. (0, 0) disables it.")]
        [SerializeField] private Vector2 lookAroundSeconds = new Vector2(1f, 3f);

        [Tooltip("The area the pointer scares the bird away from while perched. Found in children if left empty; " +
                 "with none, the bird can't be scared.")]
        [SerializeField] private Collider2D hoverArea;

        private Sprite restSprite;
        private Camera viewCamera;
        private Action onGone;

        /// <summary>True while the bird is sitting on its landing point. Counted by <see cref="BirdFlock"/>.</summary>
        public bool IsPerched { get; private set; }

        private void Awake()
        {
            if (hoverArea == null) hoverArea = GetComponentInChildren<Collider2D>();
            if (gfx == null) gfx = GetComponentInChildren<SpriteRenderer>();
            if (gfx == null)
            {
                Debug.LogError($"Bird '{name}' has no SpriteRenderer in its children.", this);
                return;
            }

            restSprite = gfx.sprite;
        }

        private void OnDestroy()
        {
            SetPerched(false);
            onGone?.Invoke();
            onGone = null;
        }

        /// <summary>
        /// Moves the bird off screen and starts its flight to <paramref name="landAt"/>. It perches for
        /// <paramref name="perchSeconds"/> (or until the pointer hovers over it), flies away, then destroys itself.
        /// <paramref name="gone"/> is called when the bird is destroyed for any reason.
        /// </summary>
        public void Land(Vector3 landAt, float perchSeconds, Camera viewCamera, Action gone)
        {
            onGone = gone;
            this.viewCamera = viewCamera;
            if (gfx == null || viewCamera == null)
            {
                Debug.LogError($"Bird '{name}' can't fly without a SpriteRenderer and a camera.", this);
                Destroy(gameObject);
                return;
            }

            // From any point on screen, travelling one full screen diagonal in any direction ends up off screen.
            float halfHeight = viewCamera.orthographicSize;
            float halfWidth = halfHeight * viewCamera.aspect;
            float offscreenDistance = 2f * Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + offscreenMargin;

            transform.position = landAt + (Vector3)RandomDirection(arriveAngleRange) * offscreenDistance;
            StartCoroutine(Run(landAt, perchSeconds, offscreenDistance));
        }

        private IEnumerator Run(Vector3 landAt, float perchSeconds, float offscreenDistance)
        {
            yield return FlyTo(landAt, true);

            SetPerched(true);
            yield return Perch(perchSeconds);
            SetPerched(false);

            Vector3 leaveTo = landAt + (Vector3)RandomDirection(leaveAngleRange) * offscreenDistance;
            yield return FlyTo(leaveTo, false);

            Destroy(gameObject);
        }

        private IEnumerator FlyTo(Vector3 target, bool slowOnApproach)
        {
            Face(target.x - transform.position.x);
            float flightTime = 0f;

            while (transform.position != target)
            {
                Vector3 position = transform.position;
                float speedScale = 1f;
                if (slowOnApproach && landingSlowRadius > 0f)
                {
                    float distance = Vector3.Distance(position, target);
                    speedScale = Mathf.Clamp(distance / landingSlowRadius, MinApproachSpeed, 1f);
                }

                transform.position = Vector3.MoveTowards(position, target, flySpeed * speedScale * Time.deltaTime);
                flightTime += Time.deltaTime;
                PlayFrames(flapFrames, flapFramesPerSecond, flightTime);

                yield return null;
            }
        }

        private IEnumerator Perch(float seconds)
        {
            bool lookAround = lookAroundSeconds.y > 0f;
            float nextLook = lookAround ? Random.Range(lookAroundSeconds.x, lookAroundSeconds.y) : float.MaxValue;

            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
            {
                if (IsPointerOver()) yield break;

                PlayFrames(idleFrames, idleFramesPerSecond, elapsed);
                if (elapsed >= nextLook)
                {
                    gfx.flipX = !gfx.flipX;
                    nextLook = elapsed + Random.Range(lookAroundSeconds.x, lookAroundSeconds.y);
                }

                yield return null;
            }
        }

        private void SetPerched(bool perched)
        {
            if (IsPerched == perched) return;

            IsPerched = perched;
            if (perched) BirdFlock.NotifyPerched(this);
            else BirdFlock.NotifyLeftPerch(this);
        }

        // Polled rather than using OnMouseEnter, which doesn't fire with the Input System. Also catches a bird that
        // lands under a pointer that isn't moving.
        private bool IsPointerOver()
        {
            if (hoverArea == null || viewCamera == null || InputManager.instance == null) return false;

            Vector3 screen = InputManager.instance.PointerPosition;
            screen.z = transform.position.z - viewCamera.transform.position.z;
            return hoverArea.OverlapPoint(viewCamera.ScreenToWorldPoint(screen));
        }

        // Shows the frame for this moment of a looping animation, or the prefab's sprite when there are no frames.
        private void PlayFrames(Sprite[] frames, float framesPerSecond, float time)
        {
            gfx.sprite = frames != null && frames.Length > 0
                ? frames[(int)(time * framesPerSecond) % frames.Length]
                : restSprite;
        }

        private void Face(float horizontalDirection)
        {
            if (Mathf.Abs(horizontalDirection) < 0.001f) return;
            bool facingRight = horizontalDirection > 0f;
            gfx.flipX = facingRight != spriteFacesRight;
        }

        private static Vector2 RandomDirection(Vector2 angleRange)
        {
            float radians = Random.Range(angleRange.x, angleRange.y) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }
    }
}
