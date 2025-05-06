using UnityEngine;
using VikingRaven.Core.Behavior;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Behaviors
{
    public class MoveBehavior : BaseBehavior
    {
        private Vector3 _targetPosition;
        private float _minDistanceToTarget = 0.1f;
        private float _baseWeight = 2.0f;

        public MoveBehavior(IEntity entity) : base("Move", entity)
        {
        }

        public void SetTargetPosition(Vector3 position)
        {
            _targetPosition = position;
        }

        public override float CalculateWeight()
        {
            var transformComponent = _entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                return 0f;

            var distanceToTarget = Vector3.Distance(transformComponent.Position, _targetPosition);
            
            // The farther from target, the higher the weight
            _weight = _baseWeight * Mathf.Clamp(distanceToTarget, 0.1f, 10f);
            
            // If very close to target, reduce weight dramatically
            if (distanceToTarget < _minDistanceToTarget)
            {
                _weight = 0f;
            }
            
            // Check if there are enemies nearby, if so reduce weight
            var aggroDetectionComponent = _entity.GetComponent<AggroDetectionComponent>();
            if (aggroDetectionComponent != null && aggroDetectionComponent.HasEnemyInRange())
            {
                _weight *= 0.5f;
            }
            
            return _weight;
        }

        public override void Execute()
        {
            var transformComponent = _entity.GetComponent<TransformComponent>();
            var navigationComponent = _entity.GetComponent<NavigationComponent>();
            
            if (transformComponent == null || navigationComponent == null)
                return;

            // Set the target in the navigation component
            navigationComponent.SetDestination(_targetPosition);
            
            // Let the navigation component handle the actual movement
            navigationComponent.UpdatePathfinding();
            
            // Check if we've reached the destination
            if (Vector3.Distance(transformComponent.Position, _targetPosition) < _minDistanceToTarget)
            {
                Debug.Log($"Entity {_entity.Id} reached destination");
            }
        }
    }
}