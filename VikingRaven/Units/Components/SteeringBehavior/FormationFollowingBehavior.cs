using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Steering;

namespace VikingRaven.Units.Components
{
    public class FormationFollowingBehavior : BaseSteeringBehavior
    {
        private Vector3 _squadCenter;
        private Quaternion _squadRotation;
        private float _maxDistance = 5.0f;
        
        public Vector3 SquadCenter
        {
            get => _squadCenter;
            set => _squadCenter = value;
        }
        
        public Quaternion SquadRotation
        {
            get => _squadRotation;
            set => _squadRotation = value;
        }

        public FormationFollowingBehavior() : base("FormationFollowing")
        {
        }

        public override SteeringOutput Calculate(IEntity entity)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var formationComponent = entity.GetComponent<FormationComponent>();
            
            if (transformComponent == null || formationComponent == null)
                return SteeringOutput.Zero;
                
            SteeringOutput output = SteeringOutput.Zero;
            
            // Calculate the target position based on squad center, rotation and formation offset
            Vector3 formationOffset = formationComponent.FormationOffset;
            Vector3 rotatedOffset = _squadRotation * formationOffset;
            Vector3 targetPosition = _squadCenter + rotatedOffset;
            
            // Calculate distance to target position
            float distanceToTarget = Vector3.Distance(transformComponent.Position, targetPosition);
            
            // If we're far away from our formation position, use a Seek behavior
            if (distanceToTarget > 0.1f)
            {
                var seekBehavior = new SeekBehavior();
                seekBehavior.TargetPosition = targetPosition;
                
                // If we're very far, increase weight to prioritize getting back into formation
                if (distanceToTarget > _maxDistance)
                {
                    seekBehavior.Weight = 2.0f;
                }
                
                output = seekBehavior.Calculate(entity);
            }
            
            return output;
        }
    }
}