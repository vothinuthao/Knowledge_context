using Core.ECS;
using UnityEngine;

namespace Components
{
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
        
        public float GetEffectiveMaxSpeed()
        {
            return MaxSpeed * SpeedMultiplier;
        }
        
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