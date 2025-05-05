using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class FormationSystem : BaseSystem
    {
        private Dictionary<int, Vector3> _squadCenters = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadRotations = new Dictionary<int, Quaternion>();
        
        public override void Execute()
        {
            // Get all entities with formation components
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            // First, calculate the center of each squad
            CalculateSquadCenters(entities);
            
            // Then, update formation positions for each entity
            foreach (var entity in entities)
            {
                // Get required components
                var formationComponent = entity.GetComponent<FormationComponent>();
                var transformComponent = entity.GetComponent<TransformComponent>();
                
                if (transformComponent != null)
                {
                    // Get squad center and rotation
                    if (_squadCenters.TryGetValue(formationComponent.SquadId, out Vector3 squadCenter) &&
                        _squadRotations.TryGetValue(formationComponent.SquadId, out Quaternion squadRotation))
                    {
                        // Calculate formation position
                        var formationOffset = formationComponent.FormationOffset;
                        
                        // Apply rotation to the offset
                        var rotatedOffset = squadRotation * formationOffset;
                        
                        // Calculate target position
                        var targetPosition = squadCenter + rotatedOffset;
                        
                        // Set target position for the unit to move to
                        var navigationComponent = entity.GetComponent<NavigationComponent>();
                        if (navigationComponent != null)
                        {
                            navigationComponent.SetDestination(targetPosition);
                        }
                    }
                }
            }
        }

        private void CalculateSquadCenters(List<IEntity> entities)
        {
            // Clear previous data
            _squadCenters.Clear();
            _squadRotations.Clear();
            
            // First, calculate centers
            Dictionary<int, List<Vector3>> squadPositions = new Dictionary<int, List<Vector3>>();
            Dictionary<int, Vector3> squadForwardDirections = new Dictionary<int, Vector3>();
            
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
                    }
                    
                    squadPositions[squadId].Add(transformComponent.Position);
                    
                    // Update squad forward direction
                    if (!squadForwardDirections.ContainsKey(squadId))
                    {
                        squadForwardDirections[squadId] = transformComponent.Forward;
                    }
                }
            }
            
            // Calculate center for each squad
            foreach (var kvp in squadPositions)
            {
                int squadId = kvp.Key;
                List<Vector3> positions = kvp.Value;
                
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
                    
                    // Get rotation from forward direction
                    if (squadForwardDirections.TryGetValue(squadId, out Vector3 forward))
                    {
                        _squadRotations[squadId] = Quaternion.LookRotation(forward);
                    }
                    else
                    {
                        _squadRotations[squadId] = Quaternion.identity;
                    }
                }
            }
        }
    }
}