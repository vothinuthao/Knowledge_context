using Core.ECS;
using UnityEngine;

namespace Movement
{
    /// <summary>
    /// Component for entity velocity
    /// </summary>
    public class VelocityComponent : IComponent
    {
        public Vector3 Velocity { get; set; }
        public float MaxSpeed { get; set; }
        public float SpeedMultiplier { get; set; } = 1.0f;
        
        public VelocityComponent(float maxSpeed)
        {
            Velocity = Vector3.zero;
            MaxSpeed = maxSpeed;
        }
        
        /// <summary>
        /// Get the current effective max speed
        /// </summary>
        public float GetEffectiveMaxSpeed()
        {
            return MaxSpeed * SpeedMultiplier;
        }
        
        /// <summary>
        /// Limit velocity to max speed
        /// </summary>
        public void LimitVelocity()
        {
            float maxSpeed = GetEffectiveMaxSpeed();
            if (Velocity.magnitude > maxSpeed)
            {
                Velocity = Velocity.normalized * maxSpeed;
            }
        }
    }
}