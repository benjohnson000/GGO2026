using System.Collections.Generic;

namespace ggo2026
{
    /// <summary>
    /// One physical tile. It lives either in the <see cref="TilePool"/> or on the <see cref="Board"/>.
    /// Compared by reference, so two copies of the same <see cref="TileDataSO"/> are different tiles.
    /// </summary>
    public class Tile
    {
        private static readonly TileEffect[] NoEffects = new TileEffect[0];

        /// <summary>Tile definition. Null only for a fallback filler created without data.</summary>
        public TileDataSO Data { get; }

        /// <summary>Colour this tile matches as.</summary>
        public TileColour Colour { get; }

        /// <summary>Fillers stand in when the pool is empty. They are not part of the deck.</summary>
        public bool IsFiller { get; }

        /// <summary>Combo tiles are made by special matches. They are temporary: gone for good when destroyed, never in the deck.</summary>
        public bool IsCombo { get; }

        /// <summary>What the tile does, from its data, in firing order. Empty for none; may contain null entries.</summary>
        public IReadOnlyList<TileEffect> Effects => Data != null ? Data.Effects : NoEffects;

        /// <summary>True if the tile has at least one effect.</summary>
        public bool HasEffects
        {
            get
            {
                IReadOnlyList<TileEffect> effects = Effects;
                for (int i = 0; i < effects.Count; i++)
                {
                    if (effects[i] != null) return true;
                }
                return false;
            }
        }

        /// <summary>True if any of the tile's effects fires when the tile is swapped (see <see cref="TileEffect.ActivatesOnSwap"/>).</summary>
        public bool ActivatesOnSwap
        {
            get
            {
                IReadOnlyList<TileEffect> effects = Effects;
                for (int i = 0; i < effects.Count; i++)
                {
                    if (effects[i] != null && effects[i].ActivatesOnSwap) return true;
                }
                return false;
            }
        }

        public Tile(TileDataSO data) : this(data, data != null ? data.Colour : TileColour.None, false, false)
        {
        }

        private Tile(TileDataSO data, TileColour colour, bool isFiller, bool isCombo)
        {
            Data = data;
            Colour = colour;
            IsFiller = isFiller;
            IsCombo = isCombo;
        }

        /// <summary>Creates a filler from a filler tile definition.</summary>
        public static Tile CreateFiller(TileDataSO data)
        {
            return new Tile(data, data != null ? data.Colour : TileColour.None, true, false);
        }

        /// <summary>Creates a plain filler of the given colour, used when no filler tile definitions are available.</summary>
        public static Tile CreateFiller(TileColour colour)
        {
            return new Tile(null, colour, true, false);
        }

        /// <summary>Creates a temporary combo tile. Its effects come from <paramref name="data"/>.</summary>
        public static Tile CreateCombo(TileDataSO data)
        {
            return new Tile(data, data != null ? data.Colour : TileColour.None, false, true);
        }

        /// <summary>True if one of the tile's effects is a <typeparamref name="T"/>.</summary>
        public bool HasEffect<T>() where T : TileEffect
        {
            IReadOnlyList<TileEffect> effects = Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] is T) return true;
            }
            return false;
        }
    }
}
