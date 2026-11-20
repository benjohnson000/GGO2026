using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace benjohnson
{
    public abstract class StateMachine<EState> : MonoBehaviour where EState : Enum
    {
        /// <summary>
        /// Subclasses must populate this (e.g. in Awake) before calling TransitionToState.
        /// </summary>
        protected Dictionary<EState, BaseState<EState>> states = new Dictionary<EState, BaseState<EState>>();
        protected BaseState<EState> currentState;

        protected bool isTransitioning = false;

        [SerializeField] private bool logState;

        private Coroutine transitionRoutine;

        /// <summary>True when a state is active (false before the first state and mid-transition).</summary>
        public bool HasState => currentState != null;

        /// <summary>Key of the active state, or default while there is none.</summary>
        public EState currentStateKey => currentState != null ? currentState.StateKey : default;

        protected void Update()
        {
            if (currentState == null) return;

            if (!isTransitioning) currentState.Update();
        }

        /// <summary>
        /// Exits the current state, waits <paramref name="time"/> seconds, then enters the new one.
        /// A new request replaces any transition still in progress.
        /// </summary>
        public void TransitionToState(EState stateKey, float time = 0f)
        {
            if (states == null || !states.ContainsKey(stateKey))
            {
                Debug.LogError($"{name}: no state registered for key '{stateKey}'.", this);
                return;
            }

            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(Transition(stateKey, time));
        }

        private IEnumerator Transition(EState stateKey, float time)
        {
            isTransitioning = true;

            // currentState is null if we interrupted an earlier transition mid-wait; it was already exited.
            currentState?.Exit();
            currentState = null;

            if (time > 0f) yield return new WaitForSeconds(time);
            else yield return null;

            currentState = states[stateKey];
            if (logState) Debug.Log($"{name} entered new state: {stateKey}");
            currentState.Enter();

            isTransitioning = false;
            transitionRoutine = null;
        }
    }
}
