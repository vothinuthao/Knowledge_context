using System.Collections.Generic;
using UnityEngine;
using Zenject;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Steering;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
     public class SteeringSystem : BaseSystem
    {
        [Inject] private SquadCoordinationSystem _squadCoordinationSystem;
        
        private Dictionary<int, Vector3> _squadCenters = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadRotations = new Dictionary<int, Quaternion>();
        
        public override void Execute()
        {
            // Get all entities with steering components
            var entities = EntityRegistry.GetEntitiesWithComponent<SteeringComponent>();
            
            // First, calculate squad centers and rotations
            CalculateSquadCentersAndRotations(entities);
            
            // Then update steering behaviors for each entity
            foreach (var entity in entities)
            {
                // Get steering component
                var steeringComponent = entity.GetComponent<SteeringComponent>();
                if (steeringComponent == null || steeringComponent.SteeringManager == null)
                    continue;
                    
                // Update formation following behavior
                UpdateFormationFollowingBehavior(entity, steeringComponent);
                
                // Steering manager will calculate and apply steering forces in its Update method
                // which is called by Unity's update system
            }
        }

        private void CalculateSquadCentersAndRotations(List<IEntity> entities)
        {
            // Clear previous data
            _squadCenters.Clear();
            _squadRotations.Clear();
            
            // Group entities by squad
            Dictionary<int, List<Vector3>> squadPositions = new Dictionary<int, List<Vector3>>();
            Dictionary<int, List<Vector3>> squadForwards = new Dictionary<int, List<Vector3>>();
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                var transformComponent = entity.GetComponent<TransformComponent>();
                
                if (formationComponent != null && transformComponent != null)
                {
                    int squadId = formationComponent.SquadId;
                    
                    // Add position to squad positions
                    if (!squadPositions.ContainsKey(squadId))
                    {
                        squadPositions[squadId] = new List<Vector3>();
                        squadForwards[squadId] = new List<Vector3>();
                    }
                    
                    squadPositions[squadId].Add(transformComponent.Position);
                    squadForwards[squadId].Add(transformComponent.Forward);
                }
            }
            
            // Calculate center and average rotation for each squad
            foreach (var squadId in squadPositions.Keys)
            {
                var positions = squadPositions[squadId];
                var forwards = squadForwards[squadId];
                
                if (positions.Count > 0)
                {
                    // Calculate average position
                    Vector3 sum = Vector3.zero;
                    foreach (var pos in positions)
                    {
                        sum += pos;
                    }
                    
                    Vector3 center = sum / positions.Count;
                    _squadCenters[squadId] = center;
                    
                    // Calculate average forward direction
                    Vector3 averageForward = Vector3.zero;
                    foreach (var fwd in forwards)
                    {
                        averageForward += fwd;
                    }
                    
                    if (averageForward.magnitude > 0.01f)
                    {
                        averageForward.Normalize();
                        _squadRotations[squadId] = Quaternion.LookRotation(averageForward);
                    }
                    else
                    {
                        _squadRotations[squadId] = Quaternion.identity;
                    }
                }
            }
        }

        private void UpdateFormationFollowingBehavior(IEntity entity, SteeringComponent steeringComponent)
        {
            var formationComponent = entity.GetComponent<FormationComponent>();
            if (formationComponent == null)
                return;
                
            int squadId = formationComponent.SquadId;
            
            // Find existing formation following behavior or create a new one
            FormationFollowingBehavior formationBehavior = null;
            
            foreach (var behavior in GetSteeringBehaviors(steeringComponent))
            {
                if (behavior is FormationFollowingBehavior)
                {
                    formationBehavior = behavior as FormationFollowingBehavior;
                    break;
                }
            }
            
            if (formationBehavior == null)
            {
                formationBehavior = new FormationFollowingBehavior();
                steeringComponent.AddBehavior(formationBehavior);
            }
            
            // Update squad center and rotation
            if (_squadCenters.TryGetValue(squadId, out Vector3 center) && 
                _squadRotations.TryGetValue(squadId, out Quaternion rotation))
            {
                formationBehavior.SquadCenter = center;
                formationBehavior.SquadRotation = rotation;
            }
        }

        private List<ISteeringBehavior> GetSteeringBehaviors(SteeringComponent steeringComponent)
        {
            // NOTE: This is a simplified implementation
            // In a real game, you would track behaviors within the SteeringManager
            
            List<ISteeringBehavior> behaviors = new List<ISteeringBehavior>();
            
            // Get behaviors using reflection (for demonstration only)
            var steeringManager = steeringComponent.SteeringManager;
            if (steeringManager == null)
                return behaviors;
                
            var field = steeringManager.GetType().GetField("_behaviors", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
                
            if (field != null)
            {
                var fieldValue = field.GetValue(steeringManager) as List<ISteeringBehavior>;
                if (fieldValue != null)
                {
                    behaviors = fieldValue;
                }
            }
            
            return behaviors;
        }
    }
}