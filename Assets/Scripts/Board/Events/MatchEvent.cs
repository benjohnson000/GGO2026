namespace ggo2026
{
    /// <summary>A group of tiles matched. Followed by a <see cref="TileDestroyedEvent"/> for each of its tiles.</summary>
    public class MatchEvent : BoardEvent
    {
        public MatchGroup Group { get; }

        /// <summary>0 for matches made directly by the swap, 1 for the first cascade, and so on.</summary>
        public int Cascade { get; }

        public MatchEvent(MatchGroup group, int cascade)
        {
            Group = group;
            Cascade = cascade;
        }
    }
}
