using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ggo2026
{
    /// <summary>Shared IMGUI helpers for the tile inspector and the Tile Editor window: sheet loading, sprite pickers and the tile preview.</summary>
    public static class TileEditorGUI
    {
        // Sheets the pickers read from. Update these if the sheets move.
        public const string ShapeSheetPath = "Assets/Art/tile_shapes.png";
        public const string PatternSheetPath = "Assets/Art/tile_patterns.png";
        public const string IconSheetPath = "Assets/Art/tile_icons.png";

        private const float CellSize = 40f;
        private const float CellPadding = 3f;

        private static readonly Color CellColour = new Color(0.18f, 0.18f, 0.18f);
        private static readonly Color SelectedColour = new Color(0.24f, 0.49f, 0.9f);
        private static readonly Color PreviewBackground = new Color(0.12f, 0.12f, 0.12f);

        private static readonly Dictionary<string, List<Sprite>> sheetCache = new Dictionary<string, List<Sprite>>();
        private static readonly Dictionary<Texture2D, Color32[]> pixelCache = new Dictionary<Texture2D, Color32[]>();
        private static readonly List<Sprite> patternOptions = new List<Sprite>();
        private static PaletteSO previewPalette;
        private static bool searchedForPalette;

        private static Texture2D previewTexture;
        private static PreviewInputs previewInputs;

        /// <summary>Palette that colours the preview. Found automatically if not set.</summary>
        public static PaletteSO PreviewPalette
        {
            get
            {
                if (previewPalette == null && !searchedForPalette)
                {
                    searchedForPalette = true;
                    string[] guids = AssetDatabase.FindAssets("t:" + nameof(PaletteSO));
                    if (guids.Length > 0) previewPalette = AssetDatabase.LoadAssetAtPath<PaletteSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
                return previewPalette;
            }
            set
            {
                previewPalette = value;
                searchedForPalette = value != null;
            }
        }

        public static Color OutlineColor => PreviewPalette != null ? PreviewPalette.OutlineColor : Color.black;
        public static int OutlineThickness => PreviewPalette != null ? PreviewPalette.OutlineThickness : 0;

        /// <summary>Forgets cached sheets, pixels and palette so they are reloaded on the next draw.</summary>
        public static void ClearCache()
        {
            sheetCache.Clear();
            pixelCache.Clear();
            previewPalette = null;
            searchedForPalette = false;
            previewInputs = default;
        }

        /// <summary>Non-empty sprites sliced from the sheet at <paramref name="path"/>, in reading order.</summary>
        public static IReadOnlyList<Sprite> GetSheetSprites(string path)
        {
            if (sheetCache.TryGetValue(path, out List<Sprite> cached) && AllAlive(cached)) return cached;

            // The sheet was reimported (or never loaded), so cached pixels may be stale too.
            pixelCache.Clear();
            previewInputs = default;

            List<Sprite> sprites = new List<Sprite>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite) sprites.Add(sprite);
            }

            if (sprites.Count > 0)
            {
                Texture2D texture = sprites[0].texture;
                Color32[] pixels = GetPixels(texture);
                if (pixels != null) sprites.RemoveAll(sprite => !HasVisiblePixel(pixels, texture.width, sprite.textureRect));
            }

            // Top row first, then left to right, to match how the sheet looks.
            sprites.Sort((a, b) => a.rect.y != b.rect.y ? b.rect.y.CompareTo(a.rect.y) : a.rect.x.CompareTo(b.rect.x));
            sheetCache[path] = sprites;
            return sprites;
        }

        /// <summary>Pattern choices: every pattern slice, followed by every shape slice (shapes work as patterns too).</summary>
        public static IReadOnlyList<Sprite> GetPatternOptions()
        {
            patternOptions.Clear();
            patternOptions.AddRange(GetSheetSprites(PatternSheetPath));
            patternOptions.AddRange(GetSheetSprites(ShapeSheetPath));
            return patternOptions;
        }

        /// <summary>Clickable grid of sprite thumbnails. Returns the selected sprite (null for the "None" cell).</summary>
        public static Sprite SpriteGrid(Sprite current, IReadOnlyList<Sprite> sprites, Color tint, bool allowNone)
        {
            int count = sprites.Count + (allowNone ? 1 : 0);
            if (sprites.Count == 0)
            {
                EditorGUILayout.HelpBox("No sprites found in the sheet. Check the sheet path and that it is sliced.", MessageType.Warning);
                return current;
            }

            float availableWidth = EditorGUIUtility.currentViewWidth - 40f;
            int columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / CellSize));
            int rows = Mathf.CeilToInt(count / (float)columns);
            Rect area = GUILayoutUtility.GetRect(columns * CellSize, rows * CellSize, GUILayout.ExpandWidth(false));

            Sprite result = current;
            Event evt = Event.current;
            for (int i = 0; i < count; i++)
            {
                Rect cell = new Rect(area.x + (i % columns) * CellSize, area.y + (i / columns) * CellSize, CellSize, CellSize);
                Sprite sprite = allowNone ? (i == 0 ? null : sprites[i - 1]) : sprites[i];

                if (evt.type == EventType.Repaint)
                {
                    Rect background = new Rect(cell.x + 1f, cell.y + 1f, cell.width - 2f, cell.height - 2f);
                    EditorGUI.DrawRect(background, sprite == current ? SelectedColour : CellColour);

                    Rect inner = new Rect(cell.x + CellPadding, cell.y + CellPadding, cell.width - CellPadding * 2f, cell.height - CellPadding * 2f);
                    if (sprite == null) GUI.Label(inner, "None", EditorStyles.centeredGreyMiniLabel);
                    else DrawSpriteFitted(inner, sprite, tint);
                }
                else if (GUI.enabled && evt.type == EventType.MouseDown && evt.button == 0 && cell.Contains(evt.mousePosition))
                {
                    result = sprite;
                    GUI.changed = true;
                    evt.Use();
                }
            }
            return result;
        }

        /// <summary>
        /// Draws the tile as the board will: outlined shape (colour 1), pattern masked to the shape (colour 3), icon (colour 2).
        /// The layers are composited per texel on the CPU, so masking and outline match the shader exactly.
        /// </summary>
        public static void Preview(Sprite shape, Sprite pattern, Sprite icon, Color shapeTint, Color patternTint, Color iconTint, float size)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (Event.current.type != EventType.Repaint) return;

            EditorGUI.DrawRect(rect, PreviewBackground);

            PreviewInputs inputs = new PreviewInputs(shape, pattern, icon, shapeTint, patternTint, iconTint, OutlineColor, OutlineThickness);
            if (previewTexture == null || !inputs.Equals(previewInputs))
            {
                previewInputs = inputs;
                Compose(inputs);
            }
            if (previewTexture == null) return;

            Vector2 texels = new Vector2(previewTexture.width, previewTexture.height);
            float pointsPerTexel = PointsPerTexel(rect.size, texels);
            Vector2 drawSize = texels * pointsPerTexel;
            Rect drawRect = new Rect(Snap(rect.center.x - drawSize.x * 0.5f), Snap(rect.center.y - drawSize.y * 0.5f), drawSize.x, drawSize.y);
            GUI.DrawTexture(drawRect, previewTexture, ScaleMode.StretchToFill, true);
        }

        /// <summary>Draws one sprite centred in <paramref name="area"/> at the largest whole-number pixel scale that fits.</summary>
        public static void DrawSpriteFitted(Rect area, Sprite sprite, Color tint)
        {
            Texture2D texture = sprite.texture;
            Rect textureRect = sprite.textureRect;
            Rect uv = new Rect(textureRect.x / texture.width, textureRect.y / texture.height, textureRect.width / texture.width, textureRect.height / texture.height);

            float pointsPerTexel = PointsPerTexel(area.size, textureRect.size);
            Vector2 size = textureRect.size * pointsPerTexel;
            Rect drawRect = new Rect(Snap(area.center.x - size.x * 0.5f), Snap(area.center.y - size.y * 0.5f), size.x, size.y);

            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(drawRect, texture, uv, true);
            GUI.color = previous;
        }

        /// <summary>
        /// Builds the preview texture at the shape's texel resolution (or the icon's when there is no shape).
        /// Pattern and icon are centred on the shape texel for texel, as on the board (same pixels per unit assumed).
        /// </summary>
        private static void Compose(PreviewInputs inputs)
        {
            Sprite basis = inputs.Shape != null ? inputs.Shape : inputs.Icon;
            if (basis == null) return;

            int width = Mathf.RoundToInt(basis.textureRect.width);
            int height = Mathf.RoundToInt(basis.textureRect.height);
            Color[] canvas = new Color[width * height];

            if (inputs.Shape != null)
            {
                bool[] solid = new bool[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color texel = SampleCentred(inputs.Shape, x, y, width, height);
                        if (texel.a <= 0.5f) continue;

                        int index = y * width + x;
                        solid[index] = true;
                        Color fill = texel * inputs.ShapeTint;
                        if (inputs.Pattern != null) fill = AlphaOver(fill, SampleCentred(inputs.Pattern, x, y, width, height) * inputs.PatternTint);
                        canvas[index] = fill;
                    }
                }
                DrawOutline(canvas, solid, width, height, inputs.OutlineColor, inputs.OutlineThickness);
            }

            if (inputs.Icon != null)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        canvas[index] = AlphaOver(canvas[index], SampleCentred(inputs.Icon, x, y, width, height) * inputs.IconTint);
                    }
                }
            }

            if (previewTexture == null || previewTexture.width != width || previewTexture.height != height)
            {
                if (previewTexture != null) Object.DestroyImmediate(previewTexture);
                previewTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                };
            }
            previewTexture.SetPixels(canvas);
            previewTexture.Apply();
        }

        /// <summary>Same rule as the Tile Outline shader: a transparent texel within the rounded radius of a solid one becomes outline.</summary>
        private static void DrawOutline(Color[] canvas, bool[] solid, int width, int height, Color color, int thickness)
        {
            if (thickness <= 0) return;

            int limit = thickness * thickness + 1;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (solid[y * width + x] || !HasSolidNeighbour(solid, width, height, x, y, thickness, limit)) continue;
                    canvas[y * width + x] = color;
                }
            }
        }

        private static bool HasSolidNeighbour(bool[] solid, int width, int height, int x, int y, int thickness, int limit)
        {
            for (int dy = -thickness; dy <= thickness; dy++)
            {
                for (int dx = -thickness; dx <= thickness; dx++)
                {
                    if (dx * dx + dy * dy > limit) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height && solid[ny * width + nx]) return true;
                }
            }
            return false;
        }

        /// <summary>Texel of <paramref name="sprite"/> at canvas position (x, y), with the sprite centred on a canvas of the given size.</summary>
        private static Color SampleCentred(Sprite sprite, int x, int y, int canvasWidth, int canvasHeight)
        {
            Color32[] pixels = GetPixels(sprite.texture);
            if (pixels == null) return Color.clear;

            Rect rect = sprite.textureRect;
            int spriteWidth = Mathf.RoundToInt(rect.width);
            int spriteHeight = Mathf.RoundToInt(rect.height);
            int localX = x + (spriteWidth - canvasWidth) / 2;
            int localY = y + (spriteHeight - canvasHeight) / 2;
            if (localX < 0 || localX >= spriteWidth || localY < 0 || localY >= spriteHeight) return Color.clear;

            int textureX = Mathf.RoundToInt(rect.x) + localX;
            int textureY = Mathf.RoundToInt(rect.y) + localY;
            return pixels[textureY * sprite.texture.width + textureX];
        }

        private static Color AlphaOver(Color below, Color above)
        {
            float alpha = above.a + below.a * (1f - above.a);
            if (alpha <= 0f) return Color.clear;
            Color result = (above * above.a + below * below.a * (1f - above.a)) / alpha;
            result.a = alpha;
            return result;
        }

        /// <summary>Pixels of a texture, cached. The sheets are not readable, so they are read from a GPU copy.</summary>
        private static Color32[] GetPixels(Texture2D texture)
        {
            if (texture == null) return null;
            if (pixelCache.TryGetValue(texture, out Color32[] cached)) return cached;

            RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            Graphics.Blit(texture, renderTexture);
            RenderTexture.active = renderTexture;

            Texture2D copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            copy.Apply();

            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(renderTexture);

            Color32[] pixels = copy.GetPixels32();
            Object.DestroyImmediate(copy);
            pixelCache[texture] = pixels;
            return pixels;
        }

        /// <summary>
        /// GUI points per sprite texel for the largest whole number of screen pixels per texel that fits in <paramref name="area"/>.
        /// Works in screen pixels so display scaling (e.g. 125%) doesn't give uneven texel sizes.
        /// </summary>
        private static float PointsPerTexel(Vector2 area, Vector2 texels)
        {
            float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            float fit = Mathf.Min(area.x * pixelsPerPoint / texels.x, area.y * pixelsPerPoint / texels.y);
            return Mathf.Max(1, Mathf.FloorToInt(fit)) / pixelsPerPoint;
        }

        /// <summary>Rounds a GUI coordinate to the nearest screen pixel.</summary>
        private static float Snap(float points)
        {
            float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            return Mathf.Round(points * pixelsPerPoint) / pixelsPerPoint;
        }

        private static bool AllAlive(List<Sprite> sprites)
        {
            foreach (Sprite sprite in sprites)
            {
                if (sprite == null) return false;
            }
            return true;
        }

        private static bool HasVisiblePixel(Color32[] pixels, int textureWidth, Rect rect)
        {
            int xMin = Mathf.FloorToInt(rect.xMin);
            int xMax = Mathf.CeilToInt(rect.xMax);
            int yMin = Mathf.FloorToInt(rect.yMin);
            int yMax = Mathf.CeilToInt(rect.yMax);
            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    int index = y * textureWidth + x;
                    if (index >= 0 && index < pixels.Length && pixels[index].a > 0) return true;
                }
            }
            return false;
        }

        /// <summary>Everything the preview depends on, so it is only recomposed when something changes.</summary>
        private readonly struct PreviewInputs : System.IEquatable<PreviewInputs>
        {
            public readonly Sprite Shape;
            public readonly Sprite Pattern;
            public readonly Sprite Icon;
            public readonly Color ShapeTint;
            public readonly Color PatternTint;
            public readonly Color IconTint;
            public readonly Color OutlineColor;
            public readonly int OutlineThickness;

            public PreviewInputs(Sprite shape, Sprite pattern, Sprite icon, Color shapeTint, Color patternTint, Color iconTint, Color outlineColor, int outlineThickness)
            {
                Shape = shape;
                Pattern = pattern;
                Icon = icon;
                ShapeTint = shapeTint;
                PatternTint = patternTint;
                IconTint = iconTint;
                OutlineColor = outlineColor;
                OutlineThickness = outlineThickness;
            }

            public bool Equals(PreviewInputs other)
            {
                return Shape == other.Shape && Pattern == other.Pattern && Icon == other.Icon
                    && ShapeTint == other.ShapeTint && PatternTint == other.PatternTint && IconTint == other.IconTint
                    && OutlineColor == other.OutlineColor && OutlineThickness == other.OutlineThickness;
            }

            public override bool Equals(object obj) => obj is PreviewInputs other && Equals(other);

            public override int GetHashCode() => System.HashCode.Combine(Shape, Pattern, Icon, ShapeTint, PatternTint, IconTint, OutlineColor, OutlineThickness);
        }
    }
}
