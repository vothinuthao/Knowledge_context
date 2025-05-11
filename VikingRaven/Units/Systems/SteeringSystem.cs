using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Steering;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
     public class SteeringSystem : BaseSystem
    {
        private SquadCoordinationSystem _squadCoordinationSystem;
        
        private Dictionary<int, Vector3> _squadCenters = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadRotations = new Dictionary<int, Quaternion>();
        
        public override void Execute()
        {
            var entities = EntityRegistry.GetEntitiesWithComponent<SteeringComponent>();
            CalculateSquadCentersAndRotations(entities);
            foreach (var entity in entities)
            {
                var steeringComponent = entity.GetComponent<SteeringComponent>();
                if (steeringComponent == null || steeringComponent.SteeringManager == null)
                    continue;
                UpdateFormationFollowingBehavior(entity, steeringComponent);
            }
        }

        private void CalculateSquadCentersAndRotations(List<IEntity> entities)
        {
            _squadCenters.Clear();
            _squadRotations.Clear();
            
            Dictionary<int, List<Vector3>> squadPositions = new Dictionary<int, List<Vector3>>();
            Dictionary<int, List<Vector3>> squadForwards = new Dictionary<int, List<Vector3>>();
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                var transformComponent = entity.GetComponent<TransformComponent>();
                
                if (formationComponent != null && transformComponent != null)
                {
                    int squadId = formationComponent.SquadId;
                    if (!squadPositions.ContainsKey(squadId))
                    {
                        squadPositions[squadId] = new List<Vector3>();
                        squadForwards[squadId] = new List<Vector3>();
                    }
                    
                    squadPositions[squadId].Add(transformComponent.Position);
                    squadForwards[squadId].Add(transformComponent.Forward);
                }
            }
            
            foreach (var squadId in squadPositions.Keys)
            {
                var positions = squadPositions[squadId];
                var forwards = squadForwards[squadId];
                
                if (positions.Count > 0)
                {
                    Vector3 sum = Vector3.zero;
                    foreach (var pos in positions)
                    {
                        sum += pos;
                    }
                    
                    Vector3 center = sum / positions.Count;
                    _squadCenters[squadId] = center;
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