using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Steering;

namespace VikingRaven.Units.Components
{
    public class FleeBehavior : BaseSteeringBehavior
    {
        private Vector3 _targetPosition;
        private float _panicDistance = 5.0f;
        
        public Vector3 TargetPosition
        {
            get => _targetPosition;
            set => _targetPosition = value;
        }
        
        public float PanicDistance
        {
            get => _panicDistance;
            set => _panicDistance = value;
        }

        public FleeBehavior() : base("Flee")
        {
        }

        public override SteeringOutput Calculate(IEntity entity)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                return SteeringOutput.Zero;
                
            SteeringOutput output = SteeringOutput.Zero;
            
            Vector3 direction = transformComponent.Position - _targetPosition;
            float distance = direction.magnitude;
            
            // If we're far enough away, no need to flee
            if (distance > _panicDistance)
                return output;
                
            // Calculate desired velocity away from target
            Vector3 desiredVelocity = direction.normalized;
            
            // Scale to max acceleration
            float maxAccel = entity.GetComponent<SteeringComponent>()?.SteeringManager?.MaxAcceleration ?? 10.0f;
            output.LinearAcceleration = desiredVelocity * maxAccel;
            
            return output;
        }
    }
}