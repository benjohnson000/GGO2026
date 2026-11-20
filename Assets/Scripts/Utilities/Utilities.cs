using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace benjohnson
{
    /// <summary>
    /// Static helpers. Nested class names are suffixed with "Utils" so they don't shadow
    /// UnityEngine.Sprite / Input / Random.
    /// </summary>
    public static class Utilities
    {
        public static class SpriteUtils
        {
            /// <summary>Returns the sprite's pixels, or null if the sprite is missing or its texture isn't readable.</summary>
            public static Color[] GetPixels(Sprite sprite)
            {
                if (sprite == null || !sprite.texture.isReadable)
                {
                    Debug.LogError("Utilities.SpriteUtils.GetPixels: Sprite does not exist or texture is not readable");
                    return null;
                }

                Rect spriteRect = sprite.rect;
                return sprite.texture.GetPixels(
                    Mathf.RoundToInt(spriteRect.x),
                    Mathf.RoundToInt(spriteRect.y),
                    Mathf.RoundToInt(spriteRect.width),
                    Mathf.RoundToInt(spriteRect.height)
                );
            }
        }

        public static class InputUtils
        {
            /// <summary>Mouse position in screen pixels (Vector2.zero if there is no mouse).</summary>
            public static Vector2 MouseScreenPosition()
            {
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            }

            /// <summary>Mouse position in world space via the main camera. Best suited to orthographic 2D cameras.</summary>
            public static Vector2 MouseWorldPosition()
            {
                Camera cam = Camera.main;
                if (cam == null)
                {
                    Debug.LogError("Utilities.InputUtils.MouseWorldPosition: no main camera");
                    return Vector2.zero;
                }

                return cam.ScreenToWorldPoint(MouseScreenPosition());
            }
        }

        public static class RandomUtils
        {
            private static readonly System.Random _random = new System.Random();

            public static T RandomEnumValue<T>() where T : Enum
            {
                Array values = Enum.GetValues(typeof(T));
                int randomIndex = _random.Next(values.Length);
                return (T)values.GetValue(randomIndex);
            }
        }
    }
}
