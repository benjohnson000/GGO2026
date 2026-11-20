using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Definition of a deck tile: what it matches as and how it looks. Effects will be added here later.
    /// </summary>
    [CreateAssetMenu(menuName = "GGO2026/Tile", fileName = "new Tile")]
    public class TileDataSO : ScriptableObject
    {
        [Tooltip("Colours this tile matches as. More than one makes it a wildcard across those colours.")]
        [SerializeField] private TileColour colours = TileColour.Red;

        [Tooltip("Shape sprite. Identifies the effect; the colour palette is applied by the board view.")]
        [SerializeField] private Sprite shape;

        public TileColour Colours => TileColourUtils.Sanitize(colours);
        public Sprite Shape => shape;
    }
}
