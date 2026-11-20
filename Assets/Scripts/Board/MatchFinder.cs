using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Finds matches on a grid of tiles. A run is 3 or more tiles in a line that all share at least one colour.
    /// Checking adjacent pairs is not enough for multi-colour tiles (Red|Blue, Blue, Red shares no single colour),
    /// so every scan is done once per colour.
    /// Grids are indexed [x, y] with (0, 0) at the bottom left; null cells never match.
    /// </summary>
    public class MatchFinder
    {
        /// <summary>Shortest run that counts as a match.</summary>
        public const int MinRun = 3;

        private struct Run
        {
            public TileColour Colour;
            public int X;
            public int Y;
            public int Length;
            public bool Horizontal;
        }

        // Buffers reused between scans to avoid garbage during cascades.
        private readonly List<Run> runs = new List<Run>();
        private readonly List<int> parents = new List<int>();
        private readonly List<int> groupOfRoot = new List<int>();
        private int[,] owner;

        /// <summary>
        /// Replaces <paramref name="results"/> with every match group on the grid.
        /// Groups are new objects each call, so earlier results stay valid.
        /// </summary>
        public void FindMatches(Tile[,] cells, List<MatchGroup> results)
        {
            results.Clear();
            runs.Clear();

            int width = cells.GetLength(0);
            int height = cells.GetLength(1);

            for (int bit = 1; bit <= (int)TileColourUtils.All; bit <<= 1)
            {
                TileColour colour = (TileColour)bit;
                FindRuns(cells, width, height, colour, true);
                FindRuns(cells, width, height, colour, false);
            }

            if (runs.Count == 0) return;

            if (owner == null || owner.GetLength(0) != width || owner.GetLength(1) != height) owner = new int[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++) owner[x, y] = -1;
            }

            parents.Clear();
            for (int r = 0; r < runs.Count; r++) parents.Add(r);

            // Runs that share a cell belong to the same group.
            for (int r = 0; r < runs.Count; r++)
            {
                Run run = runs[r];
                for (int i = 0; i < run.Length; i++)
                {
                    int x = run.Horizontal ? run.X + i : run.X;
                    int y = run.Horizontal ? run.Y : run.Y + i;
                    if (owner[x, y] < 0) owner[x, y] = r;
                    else Union(r, owner[x, y]);
                }
            }

            groupOfRoot.Clear();
            for (int r = 0; r < runs.Count; r++) groupOfRoot.Add(-1);

            for (int r = 0; r < runs.Count; r++)
            {
                int root = Find(r);
                if (groupOfRoot[root] < 0)
                {
                    groupOfRoot[root] = results.Count;
                    results.Add(new MatchGroup());
                }
                results[groupOfRoot[root]].AddColour(runs[r].Colour);
            }

            // Walking the grid (not the runs) adds each cell once, even where runs overlap.
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (owner[x, y] < 0) continue;
                    results[groupOfRoot[Find(owner[x, y])]].AddCell(new Vector2Int(x, y), cells[x, y]);
                }
            }
        }

        /// <summary>True if the tile at <paramref name="cell"/> is part of any run.</summary>
        public static bool HasMatchThrough(Tile[,] cells, Vector2Int cell)
        {
            int width = cells.GetLength(0);
            int height = cells.GetLength(1);
            Tile tile = cells[cell.x, cell.y];
            if (tile == null) return false;

            for (int bit = 1; bit <= (int)TileColourUtils.All; bit <<= 1)
            {
                TileColour colour = (TileColour)bit;
                if ((tile.Colours & colour) == 0) continue;

                int horizontal = 1;
                for (int x = cell.x - 1; x >= 0 && Has(cells[x, cell.y], colour); x--) horizontal++;
                for (int x = cell.x + 1; x < width && Has(cells[x, cell.y], colour); x++) horizontal++;
                if (horizontal >= MinRun) return true;

                int vertical = 1;
                for (int y = cell.y - 1; y >= 0 && Has(cells[cell.x, y], colour); y--) vertical++;
                for (int y = cell.y + 1; y < height && Has(cells[cell.x, y], colour); y++) vertical++;
                if (vertical >= MinRun) return true;
            }

            return false;
        }

        /// <summary>True if any run exists on the grid.</summary>
        public static bool HasAnyMatch(Tile[,] cells)
        {
            int width = cells.GetLength(0);
            int height = cells.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (HasMatchThrough(cells, new Vector2Int(x, y))) return true;
                }
            }
            return false;
        }

        private void FindRuns(Tile[,] cells, int width, int height, TileColour colour, bool horizontal)
        {
            int lines = horizontal ? height : width;
            int length = horizontal ? width : height;

            for (int line = 0; line < lines; line++)
            {
                int i = 0;
                while (i < length)
                {
                    if (!Has(Get(cells, horizontal, line, i), colour))
                    {
                        i++;
                        continue;
                    }

                    int start = i;
                    while (i < length && Has(Get(cells, horizontal, line, i), colour)) i++;

                    if (i - start >= MinRun)
                    {
                        runs.Add(new Run
                        {
                            Colour = colour,
                            X = horizontal ? start : line,
                            Y = horizontal ? line : start,
                            Length = i - start,
                            Horizontal = horizontal,
                        });
                    }
                }
            }
        }

        private static Tile Get(Tile[,] cells, bool horizontal, int line, int i)
        {
            return horizontal ? cells[i, line] : cells[line, i];
        }

        private static bool Has(Tile tile, TileColour colour)
        {
            return tile != null && (tile.Colours & colour) != 0;
        }

        private int Find(int r)
        {
            while (parents[r] != r)
            {
                parents[r] = parents[parents[r]];
                r = parents[r];
            }
            return r;
        }

        private void Union(int a, int b)
        {
            int rootA = Find(a);
            int rootB = Find(b);
            if (rootA != rootB) parents[rootA] = rootB;
        }
    }
}
