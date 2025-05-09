using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
     public class AIDecisionSystem : BaseSystem
    {
        [SerializeField] private float _decisionUpdateInterval = 0.5f;
        
        private float _timer = 0f;
        private List<IEntity> _activeEntities = new List<IEntity>();
        
        public override void Initialize()
        {
            base.Initialize();
            Debug.Log("AIDecisionSystem initialized");
        }
        
        public override void Execute()
        {
            _timer += Time.deltaTime;
            
            // Only update decisions at specified intervals to save performance
            if (_timer >= _decisionUpdateInterval)
            {
                _timer = 0f;
                
                // Get all entities that have an AI component
                _activeEntities = EntityRegistry.GetEntitiesWithComponent<WeightedBehaviorComponent>();
                
                // Process each entity
                foreach (var entity in _activeEntities)
                {
                    ProcessEntityDecisions(entity);
                }
            }
        }
        
        private void ProcessEntityDecisions(IEntity entity)
        {
            var behaviorComponent = entity.GetComponent<WeightedBehaviorComponent>();
            if (behaviorComponent == null || !behaviorComponent.BehaviorManager)
                return;
                
            // Make sure the entity has a state component
            var stateComponent = entity.GetComponent<StateComponent>();
            if (stateComponent == null)
                return;
                
            // Skip decision making if the entity is in a controlled state like Stun or Knockback
            if (stateComponent.CurrentState != null)
            {
                string stateName = stateComponent.CurrentState.GetType().Name;
                if (stateName == "StunState" || stateName == "KnockbackState")
                {
                    return;
                }
            }
            
            // The BehaviorManager will handle selecting the highest weighted behavior
            // behaviorComponent.BehaviorManager.ExecuteHighestWeightedBehavior();
        }
    }
}