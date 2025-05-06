using UnityEngine;

namespace VikingRaven.Core.Steering
{
    public struct SteeringOutput
    {
        public Vector3 LinearAcceleration;
        public float AngularAcceleration;
        
        public static SteeringOutput Zero => new SteeringOutput 
        { 
            LinearAcceleration = Vector3.zero, 
            AngularAcceleration = 0f 
        };
        
        public static SteeringOutput operator +(SteeringOutput a, SteeringOutput b)
        {
            return new SteeringOutput
            {
                LinearAcceleration = a.LinearAcceleration + b.LinearAcceleration,
                AngularAcceleration = a.AngularAcceleration + b.AngularAcceleration
            };
        }
        
        public static SteeringOutput operator *(SteeringOutput a, float weight)
        {
            return new SteeringOutput
            {
                LinearAcceleration = a.LinearAcceleration * weight,
                AngularAcceleration = a.AngularAcceleration * weight
            };
        }
    }
}