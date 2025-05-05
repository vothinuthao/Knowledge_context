using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.StateMachine;
using VikingRaven.Units.StateMachine;

namespace VikingRaven.Units.Components
{
    public class StateComponent : BaseComponent
    {
        [SerializeField] private StateMachine _stateMachine;
        
        public IStateMachine StateMachine => _stateMachine;
        public IState CurrentState => _stateMachine?.CurrentState;

        public override void Initialize()
        {
            if (_stateMachine == null)
            {
                // Create a new state machine if one doesn't exist
                GameObject machineObject = new GameObject($"StateMachine_{Entity.Id}");
                machineObject.transform.SetParent(transform);
                
                _stateMachine = machineObject.AddComponent<StateMachine>();
                
                // Register default states
                InitializeDefaultStates();
            }
        }

        private void InitializeDefaultStates()
        {
            // Create and register idle state
            var idleState = new IdleState(Entity, _stateMachine);
            _stateMachine.RegisterState<IdleState>(idleState);
            
            // Create and register aggro state
            var aggroState = new AggroState(Entity, _stateMachine);
            _stateMachine.RegisterState<AggroState>(aggroState);
            
            // Create and register knockback state
            var knockbackState = new KnockbackState(Entity, _stateMachine);
            _stateMachine.RegisterState<KnockbackState>(knockbackState);
            
            // Create and register stun state
            var stunState = new StunState(Entity, _stateMachine);
            _stateMachine.RegisterState<StunState>(stunState);
            
            // Set initial state to idle
            _stateMachine.ChangeState(idleState);
        }

        private void Update()
        {
            if (IsActive && _stateMachine != null)
            {
                _stateMachine.Update();
            }
        }

        public override void Cleanup()
        {
            // Nothing specific to clean up
        }
    }
}