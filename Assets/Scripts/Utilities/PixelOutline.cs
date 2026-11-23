using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace benjohnson
{
    /// <summary>
    /// A pixel-art outline for a UI <see cref="Graphic"/>, made by drawing dark copies of it behind itself, one for
    /// every pixel offset within a rounded radius. Needs no material, transparent margin or mesh type, and also
    /// works on an Image without a sprite.
    /// <para>The copies multiply the art by the outline colour, so they are exact on white art and darker otherwise.</para>
    /// </summary>
    public class PixelOutline : BaseMeshEffect
    {
        [SerializeField] private Color colour = new Color32(37, 19, 26, 255);

        [Range(0, 3)]
        [Tooltip("In pixels. 0 = no outline.")]
        [SerializeField] private int thickness = 2;

        [Min(1f)]
        [Tooltip("Pixels per canvas unit: the size of one pixel of the game's art.")]
        [SerializeField] private float pixelsPerUnit = 16f;

        private readonly List<UIVertex> source = new List<UIVertex>();
        private readonly List<UIVertex> result = new List<UIVertex>();

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || thickness <= 0) return;

            source.Clear();
            result.Clear();
            helper.GetUIVertexStream(source);

            float pixel = 1f / pixelsPerUnit;
            // The +1 keeps a 2px outline round: includes (2,1) but not the (2,2) corner.
            int limit = thickness * thickness + 1;

            for (int y = -thickness; y <= thickness; y++)
            {
                for (int x = -thickness; x <= thickness; x++)
                {
                    if ((x != 0 || y != 0) && x * x + y * y <= limit) AddCopy(new Vector3(x * pixel, y * pixel));
                }
            }

            // Last, so it draws over the copies.
            result.AddRange(source);

            helper.Clear();
            helper.AddUIVertexTriangleStream(result);
        }

        private void AddCopy(Vector3 offset)
        {
            for (int i = 0; i < source.Count; i++)
            {
                UIVertex vertex = source[i];
                Color copyColour = colour;
                // Fades with the graphic.
                copyColour.a *= vertex.color.a / 255f;

                vertex.position += offset;
                vertex.color = copyColour;
                result.Add(vertex);
            }
        }
    }
}
