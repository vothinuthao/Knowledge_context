using UnityEngine;
using VikingRaven.Core.Behavior;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class PhalanxBehavior : BaseBehavior
    {
        private Vector3 _squadCenter;
        private Quaternion _squadRotation;
        private float _baseWeight = 2.0f;
        private float _minDistance = 0.5f;
        private float _formationTightness = 0.8f;

        public PhalanxBehavior(IEntity entity) : base("Phalanx", entity)
        {
        }

        public void SetSquadInfo(Vector3 center, Quaternion rotation)
        {
            _squadCenter = center;
            _squadRotation = rotation;
        }

        public override float CalculateWeight()
        {
            // Check if unit type is appropriate for phalanx
            var unitTypeComponent = _entity.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent == null)
                return 0f;
                
            // Phalanx is primarily for pikemen
            if (unitTypeComponent.UnitType == UnitType.Pike)
            {
                _weight = _baseWeight * 1.5f;
            }
            else if (unitTypeComponent.UnitType == UnitType.Infantry)
            {
                _weight = _baseWeight;
            }
            else
            {
                _weight = _baseWeight * 0.3f;
            }
            
            // Check if enemies are in front - phalanx is better when facing enemies directly
            var transformComponent = _entity.GetComponent<TransformComponent>();
            var aggroComponent = _entity.GetComponent<AggroDetectionComponent>();
            
            if (transformComponent != null && aggroComponent != null && aggroComponent.HasEnemyInRange())
            {
                var enemy = aggroComponent.GetClosestEnemy();
                var enemyTransform = enemy.GetComponent<TransformComponent>();
                
                if (enemyTransform != null)
                {
                    // Calculate how directly the enemy is in front
                    Vector3 directionToEnemy = (enemyTransform.Position - transformComponent.Position).normalized;
                    float facingAlignment = Vector3.Dot(transformComponent.Forward, directionToEnemy);
                    
                    // Higher weight when enemy is directly in front (facing alignment close to 1)
                    if (facingAlignment > 0.7f)
                    {
                        _weight *= 1.5f;
                    }
                    else if (facingAlignment > 0.3f)
                    {
                        _weight *= 1.2f;
                    }
                }
            }
            
            // Geography check - if we're in a choke point or narrow pass,
            // phalanx is more effective
            // This would require terrain analysis in real implementation
            
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
            
            // Tighten the formation for phalanx
            formationOffset *= _formationTightness;
            
            // Calculate position in formation
            Vector3 rotatedOffset = _squadRotation * formationOffset;
            Vector3 targetPosition = _squadCenter + rotatedOffset;
            
            // Set destination
            navigationComponent.SetDestination(targetPosition);
            
            // Make sure we're facing the right direction for phalanx
            if (Vector3.Distance(transformComponent.Position, targetPosition) < _minDistance)
            {
                // Align with squad rotation (all face same direction)
                transformComponent.SetRotation(_squadRotation);
                
                // Combat readiness - hold pike forward
                var animationComponent = _entity.GetComponent<AnimationComponent>();
                if (animationComponent != null)
                {
                    animationComponent.PlayAnimation("PhalanxStance");
                }
            }
        }
    }
}