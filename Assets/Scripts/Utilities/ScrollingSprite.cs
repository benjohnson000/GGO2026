using UnityEngine;

namespace benjohnson
{
    /// <summary>
    /// Scrolls a Tiled-mode SpriteRenderer along X forever. The object moves and snaps back by
    /// exactly one tile width, so the loop is seamless as long as the renderer's width covers the
    /// camera view plus one tile.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ScrollingSprite : MonoBehaviour
    {
        [Tooltip("World units per second. Negative scrolls left.")]
        [SerializeField] private float speed = -0.5f;
        [Tooltip("Randomly speeds up or slows down by up to this fraction (0.1 = ±10%), picked once on Awake.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float speedVariance = 0.1f;
        [Tooltip("Start at a random point within one tile, so the layer doesn't look the same every time the scene loads.")]
        [SerializeField] private bool randomizeStartOffset = true;

        private float startX;
        private float tileWidth;
        private float offset;
        private float actualSpeed;

        private void Awake()
        {
            startX = transform.localPosition.x;

            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer.sprite == null)
            {
                Debug.LogError($"ScrollingSprite on '{name}' has no sprite.", this);
                enabled = false;
                return;
            }
            if (spriteRenderer.drawMode != SpriteDrawMode.Tiled)
                Debug.LogWarning($"ScrollingSprite on '{name}' expects Draw Mode = Tiled; the loop will show a seam.", this);

            // One tile in the parent's space (tiled sprites draw each tile at the sprite's native size).
            tileWidth = spriteRenderer.sprite.bounds.size.x * transform.localScale.x;

            actualSpeed = speed * (1f + Random.Range(-speedVariance, speedVariance));
            if (randomizeStartOffset) offset = Random.value * tileWidth;
        }

        private void Update()
        {
            offset = Mathf.Repeat(offset + actualSpeed * Time.deltaTime, tileWidth);
            Vector3 position = transform.localPosition;
            position.x = startX + offset;
            transform.localPosition = position;
        }
    }
}
