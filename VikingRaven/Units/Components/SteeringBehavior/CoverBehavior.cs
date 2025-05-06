using UnityEngine;
using VikingRaven.Core.Behavior;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class CoverBehavior : BaseBehavior
    {
        private IEntity _protectorEntity;
        private IEntity _threatEntity;
        private float _coverDistance = 2.0f;
        private float _baseWeight = 1.8f;
        private float _checkThreatInterval = 0.5f;
        private float _lastThreatCheck = 0f;

        public CoverBehavior(IEntity entity) : base("Cover", entity)
        {
        }

        public void SetProtectorEntity(IEntity entity)
        {
            _protectorEntity = entity;
        }

        public override float CalculateWeight()
        {
            if (_protectorEntity == null)
                return 0f;
                
            // Make sure protector entity is still active
            if (!_protectorEntity.IsActive)
            {
                _protectorEntity = null;
                return 0f;
            }
            
            // Check for threats periodically to save performance
            _lastThreatCheck += Time.deltaTime;
            if (_lastThreatCheck >= _checkThreatInterval)
            {
                _lastThreatCheck = 0f;
                _threatEntity = FindThreatToSelf();
            }
            
            // If no threat, reduce weight
            if (_threatEntity == null)
            {
                _weight = _baseWeight * 0.3f;
                return _weight;
            }
            
            // Get unit type - Archer should prioritize finding cover
            var unitTypeComponent = _entity.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                if (unitTypeComponent.UnitType == UnitType.Archer)
                {
                    _weight = _baseWeight * 1.8f;
                }
                else if (unitTypeComponent.UnitType == UnitType.Pike)
                {
                    _weight = _baseWeight * 1.2f;
                }
                else
                {
                    _weight = _baseWeight * 0.8f;
                }
            }
            else
            {
                _weight = _baseWeight;
            }
            
            // Higher weight if self has low health
            var healthComponent = _entity.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                if (healthComponent.HealthPercentage < 0.3f)
                {
                    _weight *= 2.0f;
                }
                else if (healthComponent.HealthPercentage < 0.5f)
                {
                    _weight *= 1.5f;
                }
            }
            
            // Higher weight if protector is actually in Protect behavior
            var protectorBehaviorComponent = _protectorEntity.GetComponent<WeightedBehaviorComponent>();
            if (protectorBehaviorComponent != null && protectorBehaviorComponent.BehaviorManager != null)
            {
                // This is a simplified check - in real implementation you'd check the active behavior
                var protectorUnitType = _protectorEntity.GetComponent<UnitTypeComponent>();
                if (protectorUnitType != null && protectorUnitType.UnitType == UnitType.Infantry)
                {
                    _weight *= 1.3f;
                }
            }
            
            return _weight;
        }

        public override void Execute()
        {
            if (_protectorEntity == null)
                return;
                
            var transformComponent = _entity.GetComponent<TransformComponent>();
            var protectorTransform = _protectorEntity.GetComponent<TransformComponent>();
            var navigationComponent = _entity.GetComponent<NavigationComponent>();
            
            if (transformComponent == null || protectorTransform == null || navigationComponent == null)
                return;
                
            // Determine position behind the protector, away from the threat
            Vector3 coverPosition;
            
            if (_threatEntity != null)
            {
                var threatTransform = _threatEntity.GetComponent<TransformComponent>();
                if (threatTransform != null)
                {
                    // Get direction from protector to threat
                    Vector3 threatDirection = (threatTransform.Position - protectorTransform.Position).normalized;
                    
                    // Position behind protector, away from threat
                    coverPosition = protectorTransform.Position - threatDirection * _coverDistance;
                    
                    // Look at the threat (to still be able to attack)
                    transformComponent.LookAt(threatTransform.Position);
                }
                else
                {
                    // If threat transform not available, just stay behind protector
                    coverPosition = protectorTransform.Position - protectorTransform.Forward * _coverDistance;
                }
            }
            else
            {
                // No threat, stay behind protector
                coverPosition = protectorTransform.Position - protectorTransform.Forward * _coverDistance;
            }
            
            // Set destination
            navigationComponent.SetDestination(coverPosition);
        }

        private IEntity FindThreatToSelf()
        {
            var aggroComponent = _entity.GetComponent<AggroDetectionComponent>();
            if (aggroComponent != null && aggroComponent.HasEnemyInRange())
            {
                return aggroComponent.GetClosestEnemy();
            }
            
            return null;
        }
    }

}