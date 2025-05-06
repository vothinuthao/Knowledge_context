using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Steering;

namespace VikingRaven.Units.Components
{
    public class ObstacleAvoidanceBehavior : BaseSteeringBehavior
    {
        private float _ahead = 3.0f; // Look ahead distance
        private float _avoidForce = 10.0f;
        private LayerMask _obstacleLayer;
        
        public float Ahead
        {
            get => _ahead;
            set => _ahead = value;
        }
        
        public float AvoidForce
        {
            get => _avoidForce;
            set => _avoidForce = value;
        }
        
        public LayerMask ObstacleLayer
        {
            get => _obstacleLayer;
            set => _obstacleLayer = value;
        }

        public ObstacleAvoidanceBehavior() : base("ObstacleAvoidance")
        {
            // Default obstacle layer
            _obstacleLayer = LayerMask.GetMask("Obstacle");
        }

        public override SteeringOutput Calculate(IEntity entity)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                return SteeringOutput.Zero;
                
            SteeringOutput output = SteeringOutput.Zero;
            
            // Cast a ray ahead of the entity
            Vector3 ahead = transformComponent.Position + transformComponent.Forward * _ahead;
            
            // Check for obstacles with raycast
            if (Physics.Raycast(transformComponent.Position, transformComponent.Forward, out RaycastHit hit, _ahead, _obstacleLayer))
            {
                // Calculate avoidance force
                Vector3 avoidanceForce = Vector3.zero;
                
                // Calculate closest point on the obstacle surface
                Vector3 closestPoint = hit.point;
                
                // Calculate avoidance direction away from the obstacle
                Vector3 avoidDirection = (ahead - closestPoint).normalized;
                
                // Apply avoidance force
                avoidanceForce = avoidDirection * _avoidForce;
                
                output.LinearAcceleration = avoidanceForce;
            }
            
            return output;
        }
    }
}