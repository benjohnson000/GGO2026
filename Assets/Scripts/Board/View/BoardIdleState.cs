using benjohnson;

namespace ggo2026
{
    /// <summary>The board accepts input. <see cref="BoardView"/> checks for this state before starting a drag.</summary>
    public class BoardIdleState : BaseState<EBoardState>
    {
        public BoardIdleState(EBoardState key, StateMachine<EBoardState> ctx) : base(key, ctx) { }

        public override void Enter() { }

        public override void Update() { }

        public override void Exit() { }
    }
}
