using UnityEngine;
using VikingRaven.Core.Behavior;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class TestudoBehavior : BaseBehavior
    {
        private Vector3 _squadCenter;
        private Quaternion _squadRotation;
        private float _baseWeight = 1.8f;
        private float _minDistance = 0.3f;
        private float _formationTightness = 0.7f;
        private float _movementPenalty = 0.5f;

        public TestudoBehavior(IEntity entity) : base("Testudo", entity)
        {
        }

        public void SetSquadInfo(Vector3 center, Quaternion rotation)
        {
            _squadCenter = center;
            _squadRotation = rotation;
        }

        public override float CalculateWeight()
        {
            // Check if unit type is appropriate for testudo
            var unitTypeComponent = _entity.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent == null)
                return 0f;
                
            // Testudo is primarily for infantry
            if (unitTypeComponent.UnitType == UnitType.Infantry)
            {
                _weight = _baseWeight * 1.5f;
            }
            else
            {
                _weight = _baseWeight * 0.3f;
            }
            
            // Check for ranged threats - testudo is better against archers
            var aggroComponent = _entity.GetComponent<AggroDetectionComponent>();
            if (aggroComponent != null && aggroComponent.HasEnemyInRange())
            {
                bool hasRangedEnemies = CheckForRangedEnemies(aggroComponent);
                
                if (hasRangedEnemies)
                {
                    _weight *= 2.0f;
                }
                else
                {
                    _weight *= 0.5f; // Not very effective against melee
                }
            }
            
            return _weight;
        }

        public override void Execute()
        {
            var transformComponent = _entity.GetComponent<TransformComponent>();
            var formationComponent = _entity.GetComponent<FormationComponent>();
            var navigationComponent = _entity.GetComponent<NavigationComponent>();
            
            if (transformComponent == null || formationComponent == null || navigationComponent == null)
                return;
                
            // Get formation offset based on slot index
            Vector3 formationOffset = formationComponent.FormationOffset;
            
            // Tighten the formation for testudo
            formationOffset *= _formationTightness;
            
            // Calculate position in formation
            Vector3 rotatedOffset = _squadRotation * formationOffset;
            Vector3 targetPosition = _squadCenter + rotatedOffset;
            
            // Set destination - move slower in testudo formation
            navigationComponent.SetDestination(targetPosition);
            
            // Apply movement penalty - simulating slow testudo movement
            // This would be implemented by modifying movement speed directly
            // in a real game rather than this simplified approach
            
            // Enter testudo stance if close enough to position
            if (Vector3.Distance(transformComponent.Position, targetPosition) < _minDistance)
            {
                // Align with squad formation
                transformComponent.SetRotation(_squadRotation);
                
                // Set testudo animation stance
                var animationComponent = _entity.GetComponent<AnimationComponent>();
                if (animationComponent != null)
                {
                    animationComponent.PlayAnimation("TestudoStance");
                }
            }
        }

        private bool CheckForRangedEnemies(AggroDetectionComponent aggroComponent)
        {
            // NOTE: This is a simplified implementation
            // In a real game, you'd need a more sophisticated approach
            
            foreach (var enemy in GetEnemiesInRange(aggroComponent))
            {
                var enemyUnitType = enemy.GetComponent<UnitTypeComponent>();
                if (enemyUnitType != null && enemyUnitType.UnitType == UnitType.Archer)
                {
                    return true;
                }
            }
            
            return false;
        }

        private System.Collections.Generic.List<IEntity> GetEnemiesInRange(AggroDetectionComponent aggroComponent)
        {
            // This is a simplified approach to get enemies
            // In a real implementation, you'd track this in the AggroDetectionComponent
            
            System.Collections.Generic.List<IEntity> enemies = new System.Collections.Generic.List<IEntity>();
            if (aggroComponent.HasEnemyInRange())
            {
                var enemy = aggroComponent.GetClosestEnemy();
                if (enemy != null)
                {
                    enemies.Add(enemy);
                }
            }
            
            return enemies;
        }
    }
}