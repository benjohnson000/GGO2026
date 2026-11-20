namespace ggo2026
{
    /// <summary>States of <see cref="BoardView"/>.</summary>
    public enum EBoardState
    {
        /// <summary>Waiting for the player to drag a tile.</summary>
        Idle,
        /// <summary>Playing back board events. Input is ignored.</summary>
        Animating,
    }
}
