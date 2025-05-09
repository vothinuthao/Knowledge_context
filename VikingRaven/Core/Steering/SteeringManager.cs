using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.Steering
{
    public class SteeringManager : MonoBehaviour
    {
        [SerializeField] private float _maxAcceleration = 10.0f;
        [SerializeField] private float _maxAngularAcceleration = 5.0f;
        
        private readonly List<ISteeringBehavior> _behaviors = new List<ISteeringBehavior>();
        private IEntity _entity;
        
        public float MaxAcceleration => _maxAcceleration;
        public float MaxAngularAcceleration => _maxAngularAcceleration;

        public void SetEntity(IEntity entity)
        {
            _entity = entity;
        }

        public void RegisterBehavior(ISteeringBehavior behavior)
        {
            if (!_behaviors.Contains(behavior))
            {
                _behaviors.Add(behavior);
            }
        }

        public void UnregisterBehavior(ISteeringBehavior behavior)
        {
            _behaviors.Remove(behavior);
        }

        public SteeringOutput CalculateSteering()
        {
            if (_entity == null || !_entity.IsActive)
                return SteeringOutput.Zero;
                
            SteeringOutput steering = SteeringOutput.Zero;
            
            // Calculate weighted sum of all active behaviors
            foreach (var behavior in _behaviors)
            {
                if (behavior.IsActive)
                {
                    // Calculate raw steering output
                    SteeringOutput output = behavior.Calculate(_entity);
                    
                    // Apply weight and accumulate
                    steering += output * behavior.Weight;
                }
            }
            
            // Clamp the results
            if (steering.LinearAcceleration.magnitude > _maxAcceleration)
            {
                steering.LinearAcceleration = steering.LinearAcceleration.normalized * _maxAcceleration;
            }
            
            steering.AngularAcceleration = Mathf.Clamp(steering.AngularAcceleration, -_maxAngularAcceleration, _maxAngularAcceleration);
            
            return steering;
        }

        public void ApplySteering(SteeringOutput steering, float deltaTime)
        {
            if (_entity == null)
                return;
                
            var transformComponent = _entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                return;
            
            // Apply linear acceleration
            transformComponent.Move(steering.LinearAcceleration * (deltaTime * deltaTime * 0.5f));
            
            // Apply angular acceleration (simplified version)
            if (Mathf.Abs(steering.AngularAcceleration) > 0.01f)
            {
                float rotationAngle = steering.AngularAcceleration * deltaTime * deltaTime * 0.5f;
                Quaternion rotation = Quaternion.Euler(0, rotationAngle, 0);
                transformComponent.SetRotation(transformComponent.Rotation * rotation);
            }
        }

        public void Update()
        {
            if (_entity == null || !_entity.IsActive)
                return;
                
            SteeringOutput steering = CalculateSteering();
            ApplySteering(steering, Time.deltaTime);
        }
    }
}