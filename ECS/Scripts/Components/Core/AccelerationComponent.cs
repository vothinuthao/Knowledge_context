using Core.ECS;
using UnityEngine;

namespace Movement
{
    /// <summary>
    /// Component for entity acceleration
    /// </summary>
    public class AccelerationComponent : IComponent
    {
        public Vector3 Acceleration { get; set; } = Vector3.zero;
        public float MaxAcceleration { get; set; } = 30.0f;
        
        public AccelerationComponent()
        {
        }
        
        public AccelerationComponent(float maxAcceleration)
        {
            MaxAcceleration = maxAcceleration;
        }
        
        /// <summary>
        /// Limit acceleration magnitude to max acceleration
        /// </summary>
        public void LimitAcceleration()
        {
            if (Acceleration.magnitude > MaxAcceleration)
            {
                Acceleration = Acceleration.normalized * MaxAcceleration;
            }
        }
    }
}