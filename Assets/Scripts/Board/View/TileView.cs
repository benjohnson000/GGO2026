using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Placeholder visual for one <see cref="Tile"/>, built in code so no prefab is needed. Layers, back to front:
    /// outlined shape, pattern masked to the shape, icon. Each layer's tint comes from the tile's colour source for it
    /// (palette colour 1/2/3 or an override); by default shape = 2, pattern = 1, icon = 3.
    /// A tile with a custom sprite is drawn as that one sprite, untinted and (unless turned off on the tile) outlined.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TileView : MonoBehaviour
    {
        // Properties of the GGO2026/Tile Outline shader.
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");
        private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
        private static readonly int MaskRectId = Shader.PropertyToID("_MaskRect");
        private static readonly int SpriteRectId = Shader.PropertyToID("_SpriteRect");

        private const int PatternOrderOffset = 1;
        private const int IconOrderOffset = 2;

        private static MaterialPropertyBlock propertyBlock;

        public Tile Tile { get; private set; }

        /// <summary>Creates a tile view under <paramref name="parent"/> at the given local position.</summary>
        public static TileView Create(Transform parent, Tile tile, Vector3 localPosition, BoardViewSettingsSO settings)
        {
            string dataName = tile.Data != null ? tile.Data.name : "Plain";
            GameObject go = new GameObject(tile.IsFiller ? $"Tile (Filler: {dataName})" : $"Tile ({dataName})");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * (settings.CellSize * settings.TileScale);

            PaletteSO palette = settings.Palette;

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            if (settings.TileMaterial != null) spriteRenderer.sharedMaterial = settings.TileMaterial;

            // A custom sprite replaces every generated layer and keeps its own colours; only the outline is added (if enabled).
            if (tile.Data != null && tile.Data.HasCustomSprite)
            {
                spriteRenderer.sprite = tile.Data.CustomSprite;
                spriteRenderer.color = Color.white;
                if (!tile.Data.CustomSpriteOutline) SetProperties(spriteRenderer, Color.clear, 0f, null);
                else if (palette != null) SetProperties(spriteRenderer, palette.OutlineColor, palette.OutlineThickness, null);
                return AddView(go, tile);
            }

            Sprite shape = tile.Data != null && tile.Data.Shape != null ? tile.Data.Shape : settings.FallbackSprite;
            if (shape == null) Debug.LogError($"TileView: no sprite for {go.name}. Assign a fallback sprite in {settings.name}.", settings);
            spriteRenderer.sprite = shape;
            if (tile.Data != null) spriteRenderer.color = tile.Data.GetShapeColor(palette);
            else spriteRenderer.color = palette != null ? palette.GetSecondary(tile.Colour) : Color.white; // Same as the default shape source.
            if (palette != null) SetProperties(spriteRenderer, palette.OutlineColor, palette.OutlineThickness, null);

            if (tile.Data != null && tile.Data.Pattern != null && shape != null)
            {
                SpriteRenderer pattern = CreateLayer("Pattern", go.transform, spriteRenderer, tile.Data.Pattern, tile.Data.GetPatternColor(palette), PatternOrderOffset, settings);
                SetProperties(pattern, Color.clear, 0f, shape);
            }

            if (tile.Data != null && tile.Data.Icon != null)
            {
                SpriteRenderer icon = CreateLayer("Icon", go.transform, spriteRenderer, tile.Data.Icon, tile.Data.GetIconColor(palette), IconOrderOffset, settings);
                SetProperties(icon, Color.clear, 0f, null);
            }

            return AddView(go, tile);
        }

        private static TileView AddView(GameObject go, Tile tile)
        {
            TileView view = go.AddComponent<TileView>();
            view.Tile = tile;
            return view;
        }

        /// <summary>Adds a child sprite layer so it inherits the tile's position and scale (pop, fall, swap).</summary>
        private static SpriteRenderer CreateLayer(string layerName, Transform parent, SpriteRenderer shapeRenderer, Sprite sprite, Color colour, int orderOffset, BoardViewSettingsSO settings)
        {
            GameObject layer = new GameObject(layerName);
            layer.transform.SetParent(parent, false);

            SpriteRenderer layerRenderer = layer.AddComponent<SpriteRenderer>();
            layerRenderer.sprite = sprite;
            layerRenderer.color = colour;
            layerRenderer.sortingLayerID = shapeRenderer.sortingLayerID;
            layerRenderer.sortingOrder = shapeRenderer.sortingOrder + orderOffset;
            if (settings.TileMaterial != null) layerRenderer.sharedMaterial = settings.TileMaterial;
            return layerRenderer;
        }

        /// <summary>Sets the outline and optional shape mask on one renderer. Layers share the tile material, so these are per renderer.</summary>
        private static void SetProperties(SpriteRenderer target, Color outlineColor, float outlineThickness, Sprite mask)
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(OutlineColorId, outlineColor);
            propertyBlock.SetFloat(OutlineThicknessId, outlineThickness);
            if (mask != null && target.sprite != null)
            {
                propertyBlock.SetTexture(MaskTexId, mask.texture);
                propertyBlock.SetVector(MaskRectId, ToVector(mask.textureRect));
                propertyBlock.SetVector(SpriteRectId, ToVector(target.sprite.textureRect));
            }
            target.SetPropertyBlock(propertyBlock);
        }

        private static Vector4 ToVector(Rect rect) => new Vector4(rect.x, rect.y, rect.width, rect.height);
    }
}
