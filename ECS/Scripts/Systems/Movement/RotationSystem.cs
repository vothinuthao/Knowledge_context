using Core.ECS;
using Movement;
using UnityEngine;

namespace Systems.Movement
{
    /// <summary>
    /// System that handles entity rotation based on velocity
    /// </summary>
    public class RotationSystem : ISystem
    {
        private World _world;
        
        public int Priority => 90; // Lower than movement but still high
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            // Process entities with rotation and velocity
            foreach (var entity in _world.GetEntitiesWith<RotationComponent, VelocityComponent>())
            {
                var rotationComponent = entity.GetComponent<RotationComponent>();
                var velocityComponent = entity.GetComponent<VelocityComponent>();
                
                // Only rotate if moving fast enough
                if (velocityComponent.Velocity.magnitude > 0.1f)
                {
                    // Calculate target rotation based on velocity direction
                    Vector3 direction = velocityComponent.Velocity.normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    
                    // Smoothly interpolate to target rotation
                    rotationComponent.Rotation = Quaternion.Slerp(
                        rotationComponent.Rotation,
                        targetRotation,
                        rotationComponent.RotationSpeed * deltaTime
                    );
                }
            }
        }
    }
}