using UnityEngine;
using VikingRaven.Core.Behavior;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class ProtectBehavior : BaseBehavior
    {
        private IEntity _protectedEntity;
        private IEntity _threatEntity;
        private float _protectDistance = 2.0f;
        private float _baseWeight = 2.0f;
        private float _checkThreatInterval = 0.5f;
        private float _lastThreatCheck = 0f;

        public ProtectBehavior(IEntity entity) : base("Protect", entity)
        {
        }

        public void SetProtectedEntity(IEntity entity)
        {
            _protectedEntity = entity;
        }

        public override float CalculateWeight()
        {
            if (_protectedEntity == null)
                return 0f;
                
            // Make sure protected entity is still active
            if (!_protectedEntity.IsActive)
            {
                _protectedEntity = null;
                return 0f;
            }
            
            // Check for threats periodically to save performance
            _lastThreatCheck += Time.deltaTime;
            if (_lastThreatCheck >= _checkThreatInterval)
            {
                _lastThreatCheck = 0f;
                _threatEntity = FindThreatToProtectedEntity();
            }
            
            // If no threat, reduce weight
            if (_threatEntity == null)
            {
                _weight = _baseWeight * 0.2f;
                return _weight;
            }
            
            // Get unit type - Infantry should prioritize protecting
            var unitTypeComponent = _entity.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null && unitTypeComponent.UnitType == UnitType.Infantry)
            {
                _weight = _baseWeight * 1.5f;
            }
            else
            {
                _weight = _baseWeight;
            }
            
            // Higher weight if protected entity has low health
            var protectedHealth = _protectedEntity.GetComponent<HealthComponent>();
            if (protectedHealth != null)
            {
                if (protectedHealth.HealthPercentage < 0.3f)
                {
                    _weight *= 2.0f;
                }
                else if (protectedHealth.HealthPercentage < 0.5f)
                {
                    _weight *= 1.5f;
                }
            }
            
            // Higher weight if protected entity is archer (high value target)
            var protectedUnitType = _protectedEntity.GetComponent<UnitTypeComponent>();
            if (protectedUnitType != null && protectedUnitType.UnitType == UnitType.Archer)
            {
                _weight *= 1.3f;
            }
            
            return _weight;
        }

        public override void Execute()
        {
            if (_protectedEntity == null)
                return;
                
            var transformComponent = _entity.GetComponent<TransformComponent>();
            var protectedTransform = _protectedEntity.GetComponent<TransformComponent>();
            var navigationComponent = _entity.GetComponent<NavigationComponent>();
            
            if (transformComponent == null || protectedTransform == null || navigationComponent == null)
                return;
                
            // Determine position between protected entity and threat
            Vector3 protectPosition;
            
            if (_threatEntity != null)
            {
                var threatTransform = _threatEntity.GetComponent<TransformComponent>();
                if (threatTransform != null)
                {
                    // Get direction from protected to threat
                    Vector3 threatDirection = (threatTransform.Position - protectedTransform.Position).normalized;
                    
                    // Position between protected entity and threat
                    protectPosition = protectedTransform.Position + threatDirection * _protectDistance;
                    
                    // Look at the threat
                    transformComponent.LookAt(threatTransform.Position);
                }
                else
                {
                    // If threat transform not available, just stay close to protected entity
                    protectPosition = protectedTransform.Position + 
                                    (transformComponent.Position - protectedTransform.Position).normalized * _protectDistance;
                }
            }
            else
            {
                // No threat, stay close to protected entity
                protectPosition = protectedTransform.Position + 
                                (transformComponent.Position - protectedTransform.Position).normalized * _protectDistance;
            }
            
            // Set destination
            navigationComponent.SetDestination(protectPosition);
        }

        private IEntity FindThreatToProtectedEntity()
        {
            if (_protectedEntity == null)
                return null;
                
            var protectedAggroComponent = _protectedEntity.GetComponent<AggroDetectionComponent>();
            if (protectedAggroComponent != null && protectedAggroComponent.HasEnemyInRange())
            {
                return protectedAggroComponent.GetClosestEnemy();
            }
            
            return null;
        }
    }
}