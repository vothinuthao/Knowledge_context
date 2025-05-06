using System;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Events;
using VikingRaven.Core.StateMachine;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class EventIntegrationSystem : BaseSystem
    {
        public override void Initialize()
        {
            // Register for component events and forward to event system
            RegisterHealthEvents();
            RegisterStateEvents();
            RegisterAggroEvents();
            RegisterFormationEvents();
        }
        
        public override void Execute()
        {
            // This system mainly handles event registration and forwarding
            // Most of the work is done in the event handlers
        }
        
        private void RegisterHealthEvents()
        {
            // Find all health components and register for their events
            var entities = EntityRegistry.GetEntitiesWithComponent<HealthComponent>();
            
            foreach (var entity in entities)
            {
                var healthComponent = entity.GetComponent<HealthComponent>();
                
                // Register damage event
                healthComponent.OnDamageTaken += (amount, source) =>
                {
                    var damageEvent = new DamageEvent(entity, source, amount, DamageType.Physical);
                    EventManager.Instance.QueueEvent(damageEvent);
                };
                
                // Register death event
                healthComponent.OnDeath += () =>
                {
                    // Try to get the last entity that damaged this entity
                    // This is simplified - in a real game you'd track this more accurately
                    IEntity killer = null;
                    
                    var deathEvent = new DeathEvent(entity, killer);
                    EventManager.Instance.QueueEvent(deathEvent);
                };
            }
        }
        
        private void RegisterStateEvents()
        {
            // Find all state components and register for their events
            var entities = EntityRegistry.GetEntitiesWithComponent<StateComponent>();
            
            foreach (var entity in entities)
            {
                var stateComponent = entity.GetComponent<StateComponent>();
                var stateMachine = stateComponent.StateMachineInGame as StateMachineInGame;
                
                if (stateMachine != null)
                {
                    // Use reflection to add state change handler (simplified)
                    // In a real implementation, StateMachine would expose an event
                    
                    // Simplified monitoring of state changes
                    IState previousState = null;
                    
                    // We'll track state changes in Update instead
                    var tracker = entity.GetComponentBehavior<MonoBehaviour>().gameObject.AddComponent<StateChangeTracker>();
                    tracker.Initialize(entity, stateMachine);
                }
            }
        }
        
        private void RegisterAggroEvents()
        {
            // Find all aggro detection components and register for their events
            var entities = EntityRegistry.GetEntitiesWithComponent<AggroDetectionComponent>();
            
            foreach (var entity in entities)
            {
                var aggroComponent = entity.GetComponent<AggroDetectionComponent>();
                
                // Register enemy detected event
                aggroComponent.OnEnemyDetected += (enemy) =>
                {
                    var detectedEvent = new EnemyDetectedEvent(entity, enemy);
                    EventManager.Instance.QueueEvent(detectedEvent);
                };
                
                // Register enemy lost event
                aggroComponent.OnEnemyLost += (enemy) =>
                {
                    var lostEvent = new EnemyLostEvent(entity, enemy);
                    EventManager.Instance.QueueEvent(lostEvent);
                };
            }
        }
        
        private void RegisterFormationEvents()
        {
            // This would be handled via SquadCoordinationSystem
            // When formation changes occur
        }
    }
    
    // Helper class to track state changes
    public class StateChangeTracker : MonoBehaviour
    {
        private IEntity _entity;
        private IStateMachine _stateMachine;
        private IState _previousState;
        
        public void Initialize(IEntity entity, IStateMachine stateMachine)
        {
            _entity = entity;
            _stateMachine = stateMachine;
            _previousState = stateMachine.CurrentState;
        }
        
        private void Update()
        {
            if (_stateMachine == null || _entity == null)
                return;
                
            // Check for state change
            if (_previousState != _stateMachine.CurrentState)
            {
                Type oldStateType = _previousState?.GetType();
                Type newStateType = _stateMachine.CurrentState?.GetType();
                
                var stateChangedEvent = new UnitStateChangedEvent(_entity, oldStateType, newStateType);
                EventManager.Instance.QueueEvent(stateChangedEvent);
                
                _previousState = _stateMachine.CurrentState;
            }
        }
    }
}