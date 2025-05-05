using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Steering;

namespace VikingRaven.Units.Components
{
    public class SeekBehavior : BaseSteeringBehavior
    {
        private Vector3 _targetPosition;
        private float _arriveSlowingDistance = 3.0f;
        private float _minDistance = 0.1f;
        
        public Vector3 TargetPosition
        {
            get => _targetPosition;
            set => _targetPosition = value;
        }
        
        public float ArriveSlowingDistance
        {
            get => _arriveSlowingDistance;
            set => _arriveSlowingDistance = value;
        }

        public SeekBehavior() : base("Seek")
        {
        }

        public override SteeringOutput Calculate(IEntity entity)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                return SteeringOutput.Zero;
                
            SteeringOutput output = SteeringOutput.Zero;
            
            Vector3 direction = _targetPosition - transformComponent.Position;
            float distance = direction.magnitude;
            
            // If we're already at the target, return no acceleration
            if (distance < _minDistance)
                return output;
                
            // Calculate desired velocity
            Vector3 desiredVelocity = direction.normalized;
            
            // Slow down as we approach the target (arrive behavior)
            if (distance < _arriveSlowingDistance)
            {
                desiredVelocity *= distance / _arriveSlowingDistance;
            }
            
            // Scale to max acceleration
            float maxAccel = entity.GetComponent<SteeringComponent>()?.SteeringManager?.MaxAcceleration ?? 10.0f;
            output.LinearAcceleration = desiredVelocity * maxAccel;
            
            return output;
        }
    }
}