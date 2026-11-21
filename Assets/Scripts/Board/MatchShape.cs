namespace ggo2026
{
    /// <summary>
    /// The best shape found in a <see cref="MatchGroup"/>, used to decide which combo tile it makes.
    /// Priority is set by <see cref="MatchFinder"/>, not by these values: 5-line, then cross, then 4-line,
    /// then square, then plain. Serialized in <see cref="ComboRule"/>, so append new values; never reorder.
    /// </summary>
    public enum MatchShape
    {
        /// <summary>Only runs of 3, with no crossing.</summary>
        Plain = 0,

        /// <summary>A 2x2 square of one colour.</summary>
        Square = 1,

        /// <summary>A straight run of exactly 4.</summary>
        Line4 = 2,

        /// <summary>T, L or + shape: a row run and a column run that share a tile.</summary>
        Cross = 3,

        /// <summary>A straight run of 5 or more.</summary>
        Line5 = 4,
    }
}
