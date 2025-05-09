using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Steering;

namespace VikingRaven.Units.Components
{
    public class CohesionBehavior : BaseSteeringBehavior
    {
        private float _cohesionRadius = 5.0f;
        
        public float CohesionRadius
        {
            get => _cohesionRadius;
            set => _cohesionRadius = value;
        }

        public CohesionBehavior() : base("Cohesion")
        {
        }

        public override SteeringOutput Calculate(IEntity entity)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                return SteeringOutput.Zero;
                
            SteeringOutput output = SteeringOutput.Zero;
            
            // Get all entities with the same squad ID
            var formationComponent = entity.GetComponent<FormationComponent>();
            if (formationComponent == null)
                return output;
                
            int squadId = formationComponent.SquadId;
            
            // In a real implementation, you would efficiently get nearby entities
            List<IEntity> neighbors = GetNearbyEntitiesInSquad(entity, squadId);
            
            if (neighbors.Count == 0)
                return output;
                
            Vector3 centerOfMass = Vector3.zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                if (neighbor == entity)
                    continue;
                    
                var neighborTransform = neighbor.GetComponent<TransformComponent>();
                if (neighborTransform == null)
                    continue;
                    
                float distance = Vector3.Distance(transformComponent.Position, neighborTransform.Position);
                
                if (distance < _cohesionRadius)
                {
                    centerOfMass += neighborTransform.Position;
                    count++;
                }
            }
            
            if (count > 0)
            {
                centerOfMass /= count;
                
                // Use the Seek behavior to move towards the center of mass
                var seekBehavior = new SeekBehavior();
                seekBehavior.TargetPosition = centerOfMass;
                
                output = seekBehavior.Calculate(entity);
            }
            
            return output;
        }

        private List<IEntity> GetNearbyEntitiesInSquad(IEntity entity, int squadId)
        {
            // NOTE: This is a simplified implementation
            // In a real game, you would use spatial partitioning or physics queries
            
            List<IEntity> neighbors = new List<IEntity>();
            
            // Get all entities with the same formation component
            var entityRegistry = GameObject.FindObjectOfType<EntityRegistry>();
            if (entityRegistry == null)
                return neighbors;
                
            var allEntities = entityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            foreach (var potentialNeighbor in allEntities)
            {
                var neighborFormation = potentialNeighbor.GetComponent<FormationComponent>();
                if (neighborFormation != null && neighborFormation.SquadId == squadId)
                {
                    neighbors.Add(potentialNeighbor);
                }
            }
            
            return neighbors;
        }
    }
}