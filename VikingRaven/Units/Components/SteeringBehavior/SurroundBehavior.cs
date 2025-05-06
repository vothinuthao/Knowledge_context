using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.Behavior;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class SurroundBehavior : BaseBehavior
    {
        private IEntity _targetEntity;
        private float _surroundRadius = 3.0f;
        private float _baseWeight = 2.0f;
        private int _positionIndex;
        private float _rotationOffset = 0f;

        public SurroundBehavior(IEntity entity, int positionIndex) : base("Surround", entity)
        {
            _positionIndex = positionIndex;
        }

        public void SetTargetEntity(IEntity target)
        {
            _targetEntity = target;
        }

        public void SetSurroundRadius(float radius)
        {
            _surroundRadius = radius;
        }

        public override float CalculateWeight()
        {
            if (_targetEntity == null)
                return 0f;
                
            // Make sure target is still active
            if (!_targetEntity.IsActive)
            {
                _targetEntity = null;
                return 0f;
            }
            
            var transformComponent = _entity.GetComponent<TransformComponent>();
            var targetTransform = _targetEntity.GetComponent<TransformComponent>();
            
            if (transformComponent == null || targetTransform == null)
                return 0f;
                
            // Calculate distance to target
            float distanceToTarget = Vector3.Distance(transformComponent.Position, targetTransform.Position);
            
            // Higher weight when target is visible and in medium range
            if (distanceToTarget > _surroundRadius * 0.5f && distanceToTarget < _surroundRadius * 3.0f)
            {
                _weight = _baseWeight * 1.5f;
            }
            else if (distanceToTarget <= _surroundRadius * 0.5f)
            {
                // Already surrounding, maintain position
                _weight = _baseWeight;
            }
            else
            {
                // Target too far away, reduce weight
                _weight = _baseWeight * 0.5f;
            }
            
            // If multiple enemies are present, reduce surround weight
            var aggroDetectionComponent = _entity.GetComponent<AggroDetectionComponent>();
            if (aggroDetectionComponent != null)
            {
                int enemyCount = CountEnemiesInRange(aggroDetectionComponent);
                
                // Against multiple enemies, surround is less effective
                if (enemyCount > 1)
                {
                    _weight *= (1.0f / enemyCount);
                }
            }
            
            return _weight;
        }

        public override void Execute()
        {
            if (_targetEntity == null)
                return;
                
            var transformComponent = _entity.GetComponent<TransformComponent>();
            var targetTransform = _targetEntity.GetComponent<TransformComponent>();
            var navigationComponent = _entity.GetComponent<NavigationComponent>();
            
            if (transformComponent == null || targetTransform == null || navigationComponent == null)
                return;
                
            // Calculate evenly spaced positions around the target
            int totalPositions = 8; // Default to 8 positions around the target
            
            // Get actual count of squad members for better distribution
            var formationComponent = _entity.GetComponent<FormationComponent>();
            if (formationComponent != null)
            {
                var squadMembers = GetSquadMembers(formationComponent.SquadId);
                if (squadMembers.Count > 0)
                {
                    totalPositions = squadMembers.Count;
                }
            }
            
            // Calculate angle based on position index
            float angle = (_positionIndex * Mathf.PI * 2) / totalPositions;
            
            // Add rotation offset that changes slowly over time for dynamic movement
            _rotationOffset += Time.deltaTime * 0.1f;
            if (_rotationOffset > Mathf.PI * 2)
            {
                _rotationOffset -= Mathf.PI * 2;
            }
            
            angle += _rotationOffset;
            
            // Calculate position around target
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 targetPosition = targetTransform.Position + direction * _surroundRadius;
            
            // Set destination
            navigationComponent.SetDestination(targetPosition);
            
            // Make sure to look at the target
            transformComponent.LookAt(targetTransform.Position);
        }

        private int CountEnemiesInRange(AggroDetectionComponent aggroComponent)
        {
            // NOTE: This is a simplified implementation
            // In a real game, you would use spatial partitioning or efficient queries
            
            int count = 0;
            
            // Get all entities with aggro components
            var entityRegistry = GameObject.FindObjectOfType<EntityRegistry>();
            if (entityRegistry == null)
                return count;
                
            var allEntities = entityRegistry.GetEntitiesWithComponent<AggroDetectionComponent>();
            
            foreach (var entity in allEntities)
            {
                if (entity == _entity)
                    continue;
                    
                var aggroComp = entity.GetComponent<AggroDetectionComponent>();
                if (aggroComp != null && aggroComp.HasEnemyInRange())
                {
                    count++;
                }
            }
            
            return count;
        }

        private List<IEntity> GetSquadMembers(int squadId)
        {
            List<IEntity> squadMembers = new List<IEntity>();
            
            // Get all entities with formation components
            var entityRegistry = GameObject.FindObjectOfType<EntityRegistry>();
            if (entityRegistry == null)
                return squadMembers;
                
            var allEntities = entityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            foreach (var entity in allEntities)
            {
                var formationComp = entity.GetComponent<FormationComponent>();
                if (formationComp != null && formationComp.SquadId == squadId)
                {
                    squadMembers.Add(entity);
                }
            }
            
            return squadMembers;
        }
    }
}