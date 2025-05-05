using System;
using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Steering;

namespace VikingRaven.Units.Components
{
    public class SeparationBehavior : BaseSteeringBehavior
    {
        private float _separationRadius = 2.0f;
        private float _decayCoefficient = 0.5f;
        
        public float SeparationRadius
        {
            get => _separationRadius;
            set => _separationRadius = value;
        }
        
        public float DecayCoefficient
        {
            get => _decayCoefficient;
            set => _decayCoefficient = value;
        }

        public SeparationBehavior() : base("Separation")
        {
        }

        public override SteeringOutput Calculate(IEntity entity)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                return SteeringOutput.Zero;
                
            SteeringOutput output = SteeringOutput.Zero;
            
            // Get all entities with the same squad ID (simplified approach)
            var formationComponent = entity.GetComponent<FormationComponent>();
            if (formationComponent == null)
                return output;
                
            int squadId = formationComponent.SquadId;
            
            // In a real implementation, you would efficiently get nearby entities
            // here using spatial partitioning, physics overlaps, or querying the entity registry
            
            List<IEntity> neighbors = GetNearbyEntitiesInSquad(entity, squadId);
            
            if (neighbors.Count == 0)
                return output;
                
            Vector3 separationForce = Vector3.zero;
            
            foreach (var neighbor in neighbors)
            {
                if (neighbor == entity)
                    continue;
                    
                var neighborTransform = neighbor.GetComponent<TransformComponent>();
                if (neighborTransform == null)
                    continue;
                    
                Vector3 direction = transformComponent.Position - neighborTransform.Position;
                float distance = direction.magnitude;
                
                if (distance < _separationRadius && distance > 0)
                {
                    // Calculate repulsion strength (stronger when closer)
                    float strength = 1.0f / (distance * distance) * _decayCoefficient;
                    
                    // Normalize the direction and apply strength
                    direction.Normalize();
                    direction *= strength;
                    
                    separationForce += direction;
                }
            }
            
            // Scale to max acceleration
            float maxAccel = entity.GetComponent<SteeringComponent>()?.SteeringManager?.MaxAcceleration ?? 10.0f;
            if (separationForce.magnitude > 0)
            {
                separationForce = separationForce.normalized * maxAccel;
            }
            
            output.LinearAcceleration = separationForce;
            
            return output;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        [Obsolete("Obsolete")]
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