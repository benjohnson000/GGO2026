using UnityEngine;

namespace ggo2026
{
    /// <summary>Placeholder visual for one <see cref="Tile"/>: a tinted shape sprite. Built in code, so no prefab is needed.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TileView : MonoBehaviour
    {
        public Tile Tile { get; private set; }

        /// <summary>Creates a tile view under <paramref name="parent"/> at the given local position.</summary>
        public static TileView Create(Transform parent, Tile tile, Vector3 localPosition, BoardViewSettingsSO settings)
        {
            GameObject go = new GameObject(tile.IsFiller ? "Tile (Filler)" : $"Tile ({tile.Data.name})");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * (settings.CellSize * settings.TileScale);

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            Sprite shape = tile.Data != null && tile.Data.Shape != null ? tile.Data.Shape : settings.FillerSprite;
            if (shape == null) Debug.LogError($"TileView: no sprite for {go.name}. Assign a filler sprite in {settings.name}.", settings);
            spriteRenderer.sprite = shape;
            spriteRenderer.color = settings.GetColor(tile.Colours);
            if (settings.TileMaterial != null) spriteRenderer.sharedMaterial = settings.TileMaterial;

            TileView view = go.AddComponent<TileView>();
            view.Tile = tile;
            return view;
        }
    }
}
