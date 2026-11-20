using System;

namespace benjohnson
{
    public abstract class BaseState<EState> where EState : Enum
    {
        public BaseState(EState key, StateMachine<EState> ctx)
        {
            StateKey = key;
            this.ctx = ctx;
        }

        public EState StateKey { get; private set; }
        protected StateMachine<EState> ctx { get; private set; }

        public abstract void Enter();
        public abstract void Update();
        public abstract void Exit();
    }

    /*
    public class SampleState : BaseState<ENUM>
    {
        public SampleState(ENUM key, StateMachine<ENUM> ctx) : base(key, ctx) { }

        public override void Enter()
        {
                
        }

        public override void Exit()
        {
                
        }

        public override void Update()
        {
                
        }
    }
     */
}
