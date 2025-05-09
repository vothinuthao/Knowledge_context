using UnityEngine;
using UnityEngine.Serialization;
using VikingRaven.Core.ECS;
using VikingRaven.Core.StateMachine;
using VikingRaven.Units.StateMachine;

namespace VikingRaven.Units.Components
{
    public class StateComponent : BaseComponent
    {
        [SerializeField] private StateMachineInGame stateMachineInGame;
        
        public IStateMachine StateMachineInGame => stateMachineInGame;
        public IState CurrentState => stateMachineInGame?.CurrentState;

        public override void Initialize()
        {
            if (stateMachineInGame == null)
            {
                // Create a new state machine if one doesn't exist
                GameObject machineObject = new GameObject($"StateMachine_{Entity.Id}");
                machineObject.transform.SetParent(transform);
                
                stateMachineInGame = machineObject.AddComponent<StateMachineInGame>();
                
                // Register default states
                InitializeDefaultStates();
            }
        }

        private void InitializeDefaultStates()
        {
            // Create and register idle state
            var idleState = new IdleState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<IdleState>(idleState);
            
            // Create and register aggro state
            var aggroState = new AggroState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<AggroState>(aggroState);
            
            // Create and register knockback state
            var knockbackState = new KnockbackState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<KnockbackState>(knockbackState);
            
            // Create and register stun state
            var stunState = new StunState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<StunState>(stunState);
            
            // Set initial state to idle
            stateMachineInGame.ChangeState(idleState);
        }

        private void Update()
        {
            if (IsActive && stateMachineInGame)
            {
                stateMachineInGame.Update();
            }
        }

        public override void Cleanup()
        {
            // Nothing specific to clean up
        }
    }
}