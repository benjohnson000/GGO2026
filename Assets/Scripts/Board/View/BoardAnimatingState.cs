using System.Collections;
using System.Collections.Generic;
using benjohnson;
using UnityEngine;

namespace ggo2026
{
    /// <summary>Plays back the events queued on the <see cref="BoardView"/>, then returns to Idle.</summary>
    public class BoardAnimatingState : BaseState<EBoardState>
    {
        private readonly BoardView view;
        private Coroutine routine;

        public BoardAnimatingState(EBoardState key, BoardView view) : base(key, view)
        {
            this.view = view;
        }

        public override void Enter()
        {
            routine = view.StartCoroutine(Run(view.TakePendingEvents()));
        }

        public override void Update() { }

        public override void Exit()
        {
            if (routine != null) view.StopCoroutine(routine);
            routine = null;
        }

        private IEnumerator Run(List<BoardEvent> events)
        {
            yield return view.PlayEvents(events);
            // Cleared first so Exit doesn't stop this coroutine while it is finishing.
            routine = null;
            view.TransitionToState(EBoardState.Idle);
        }
    }
}
