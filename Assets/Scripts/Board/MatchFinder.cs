using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Finds matches on a grid of tiles. A match is a run of 3 or more tiles of the same colour in a row or column,
    /// or a 2x2 square of one colour.
    /// Grids are indexed [x, y] with (0, 0) at the bottom left; null cells and colourless tiles never match.
    /// </summary>
    public class MatchFinder
    {
        /// <summary>Shortest run that counts as a match.</summary>
        public const int MinRun = 3;

        private struct Run
        {
            public int X;
            public int Y;
            public int Length;
            public bool Horizontal;

            /// <summary>A 2x2 block with (X, Y) as its bottom-left cell. Length is 4; Horizontal is unused.</summary>
            public bool Square;
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

            FindRuns(cells, width, height, true);
            FindRuns(cells, width, height, false);
            FindSquares(cells, width, height);

            if (runs.Count == 0) return;

            if (owner == null || owner.GetLength(0) != width || owner.GetLength(1) != height) owner = new int[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++) owner[x, y] = -1;
            }

            parents.Clear();
            for (int r = 0; r < runs.Count; r++) parents.Add(r);

            // Runs and squares that share a cell (L, T and block shapes) belong to the same group.
            for (int r = 0; r < runs.Count; r++)
            {
                Run run = runs[r];
                for (int i = 0; i < run.Length; i++)
                {
                    if (run.Square)
                    {
                        Claim(run.X + i % 2, run.Y + i / 2, r);
                        continue;
                    }

                    int x = run.Horizontal ? run.X + i : run.X;
                    int y = run.Horizontal ? run.Y : run.Y + i;
                    Claim(x, y, r);
                }
            }

            groupOfRoot.Clear();
            for (int r = 0; r < runs.Count; r++) groupOfRoot.Add(-1);

            for (int r = 0; r < runs.Count; r++)
            {
                int root = Find(r);
                if (groupOfRoot[root] >= 0) continue;

                groupOfRoot[root] = results.Count;
                results.Add(new MatchGroup(cells[runs[r].X, runs[r].Y].Colour));
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
            if (tile == null || tile.Colour == TileColour.None) return false;

            TileColour colour = tile.Colour;

            int horizontal = 1;
            for (int x = cell.x - 1; x >= 0 && Is(cells[x, cell.y], colour); x--) horizontal++;
            for (int x = cell.x + 1; x < width && Is(cells[x, cell.y], colour); x++) horizontal++;
            if (horizontal >= MinRun) return true;

            int vertical = 1;
            for (int y = cell.y - 1; y >= 0 && Is(cells[cell.x, y], colour); y--) vertical++;
            for (int y = cell.y + 1; y < height && Is(cells[cell.x, y], colour); y++) vertical++;
            if (vertical >= MinRun) return true;

            // Any of the four 2x2 blocks that contain this cell.
            for (int dx = -1; dx <= 0; dx++)
            {
                for (int dy = -1; dy <= 0; dy++)
                {
                    if (IsSquare(cells, width, height, cell.x + dx, cell.y + dy, colour)) return true;
                }
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

        private void FindRuns(Tile[,] cells, int width, int height, bool horizontal)
        {
            int lines = horizontal ? height : width;
            int length = horizontal ? width : height;

            for (int line = 0; line < lines; line++)
            {
                int i = 0;
                while (i < length)
                {
                    Tile first = Get(cells, horizontal, line, i);
                    if (first == null || first.Colour == TileColour.None)
                    {
                        i++;
                        continue;
                    }

                    int start = i;
                    while (i < length && Is(Get(cells, horizontal, line, i), first.Colour)) i++;

                    if (i - start >= MinRun)
                    {
                        runs.Add(new Run
                        {
                            X = horizontal ? start : line,
                            Y = horizontal ? line : start,
                            Length = i - start,
                            Horizontal = horizontal,
                        });
                    }
                }
            }
        }

        private void FindSquares(Tile[,] cells, int width, int height)
        {
            for (int x = 0; x < width - 1; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    Tile tile = cells[x, y];
                    if (tile == null || tile.Colour == TileColour.None) continue;
                    if (!IsSquare(cells, width, height, x, y, tile.Colour)) continue;

                    runs.Add(new Run { X = x, Y = y, Length = 4, Square = true });
                }
            }
        }

        /// <summary>True if the 2x2 block with bottom-left (x, y) is in bounds and all <paramref name="colour"/>.</summary>
        private static bool IsSquare(Tile[,] cells, int width, int height, int x, int y, TileColour colour)
        {
            if (colour == TileColour.None) return false;
            if (x < 0 || y < 0 || x + 1 >= width || y + 1 >= height) return false;
            return Is(cells[x, y], colour) && Is(cells[x + 1, y], colour)
                && Is(cells[x, y + 1], colour) && Is(cells[x + 1, y + 1], colour);
        }

        private void Claim(int x, int y, int r)
        {
            if (owner[x, y] < 0) owner[x, y] = r;
            else Union(r, owner[x, y]);
        }

        private static Tile Get(Tile[,] cells, bool horizontal, int line, int i)
        {
            return horizontal ? cells[i, line] : cells[line, i];
        }

        private static bool Is(Tile tile, TileColour colour)
        {
            return tile != null && tile.Colour == colour;
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
