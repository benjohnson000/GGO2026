using System.Collections.Generic;

namespace ggo2026
{
    /// <summary>Looks up and creates combo tiles for match groups, from the board config's combo rules.</summary>
    public class ComboTable
    {
        private readonly List<ComboRule> rules = new List<ComboRule>();

        public ComboTable(IReadOnlyList<ComboRule> rules)
        {
            if (rules == null) return;
            foreach (ComboRule rule in rules)
            {
                if (rule != null) this.rules.Add(rule);
            }
        }

        /// <summary>
        /// Creates the combo tile for a group's shape and colour. The first rule for the shape that has a tile
        /// of that colour wins. Returns false if no rule covers it (plain matches normally have no rule).
        /// </summary>
        public bool TryCreate(MatchShape shape, TileColour colour, out Tile tile)
        {
            foreach (ComboRule rule in rules)
            {
                if (rule.Shape != shape) continue;

                TileDataSO data = rule.GetTile(colour);
                if (data == null) continue;

                tile = Tile.CreateCombo(data, rule.Effect);
                return true;
            }

            tile = null;
            return false;
        }
    }
}
