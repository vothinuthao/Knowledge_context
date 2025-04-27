using Core.ECS;
using Movement;
using Steering;
using UnityEngine;
using Combat;
using Components;
using Components.Steering;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes charge behavior (rush at enemies)
    /// </summary>
    public class ChargeSystem : ISystem
    {
        private World _world;
        
        public int Priority => 115; // Very high priority
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<ChargeComponent, SteeringDataComponent, PositionComponent>())
            {
                var chargeComponent = entity.GetComponent<ChargeComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!chargeComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                // Update cooldown
                if (chargeComponent.CooldownTimer > 0)
                {
                    chargeComponent.CooldownTimer -= deltaTime;
                    continue;
                }
                
                // Process preparation state
                if (chargeComponent.IsPreparing)
                {
                    ProcessChargePreparation(entity, chargeComponent, steeringData, deltaTime);
                    continue;
                }
                
                // Process charging state
                if (chargeComponent.IsCharging)
                {
                    ProcessCharging(entity, chargeComponent, steeringData, positionComponent, deltaTime);
                    continue;
                }
                
                // Check for charge opportunity
                TryStartCharge(entity, chargeComponent, steeringData, positionComponent);
            }
        }
        
        private void ProcessChargePreparation(Entity entity, ChargeComponent chargeComponent, 
            SteeringDataComponent steeringData, float deltaTime)
        {
            // Update preparation timer
            chargeComponent.PreparationTimer -= deltaTime;
            
            if (chargeComponent.PreparationTimer <= 0)
            {
                // Start charging
                chargeComponent.IsPreparing = false;
                chargeComponent.IsCharging = true;
                
                // Set charge direction
                if (entity.HasComponent<PositionComponent>())
                {
                    var position = entity.GetComponent<PositionComponent>().Position;
                    chargeComponent.ChargeDirection = (chargeComponent.ChargeTarget - position).normalized;
                }
                
                // Apply speed boost
                if (entity.HasComponent<VelocityComponent>())
                {
                    var velocityComponent = entity.GetComponent<VelocityComponent>();
                    velocityComponent.SpeedMultiplier = chargeComponent.ChargeSpeedMultiplier;
                }
            }
            else
            {
                // During preparation, slow down to a stop
                if (entity.HasComponent<VelocityComponent>())
                {
                    steeringData.AddForce(-entity.GetComponent<VelocityComponent>().Velocity * 2.0f);
                }
            }
        }
        
        private void ProcessCharging(Entity entity, ChargeComponent chargeComponent, 
            SteeringDataComponent steeringData, PositionComponent positionComponent, float deltaTime)
        {
            // Check if we've reached the target
            float distanceToTarget = Vector3.Distance(positionComponent.Position, chargeComponent.ChargeTarget);
            
            if (distanceToTarget < 0.5f)
            {
                // End charge
                EndCharge(entity, chargeComponent);
                return;
            }
            
            // Continue charging
            Vector3 chargeForce = chargeComponent.ChargeDirection * (chargeComponent.Weight * 4.0f);
            steeringData.AddForce(chargeForce);
            
            // Check for collision with enemies and deal damage
            CheckChargeCollisions(entity, chargeComponent, positionComponent);
        }
        
        private void TryStartCharge(Entity entity, ChargeComponent chargeComponent, 
            SteeringDataComponent steeringData, PositionComponent positionComponent)
        {
            // Look for enemy targets in charge range
            foreach (var enemyId in steeringData.NearbyEnemiesIds)
            {
                foreach (var enemyEntity in _world.GetEntitiesWith<PositionComponent>())
                {
                    if (enemyEntity.Id != enemyId)
                    {
                        continue;
                    }
                    
                    var enemyPosition = enemyEntity.GetComponent<PositionComponent>().Position;
                    float distance = Vector3.Distance(positionComponent.Position, enemyPosition);
                    
                    if (distance <= chargeComponent.ChargeDistance)
                    {
                        // Start preparing to charge
                        chargeComponent.IsPreparing = true;
                        chargeComponent.PreparationTimer = chargeComponent.ChargePreparationTime;
                        chargeComponent.ChargeTarget = enemyPosition;
                        
                        return; // Only charge at one enemy
                    }
                }
            }
        }
        
        private void EndCharge(Entity entity, ChargeComponent chargeComponent)
        {
            chargeComponent.IsCharging = false;
            chargeComponent.CooldownTimer = chargeComponent.ChargeCooldown;
            
            // Reset speed multiplier
            if (entity.HasComponent<VelocityComponent>())
            {
                entity.GetComponent<VelocityComponent>().SpeedMultiplier = 1.0f;
            }
        }
        
        private void CheckChargeCollisions(Entity entity, ChargeComponent chargeComponent, PositionComponent positionComponent)
        {
            // In a real implementation, this would check for collisions and deal damage
            // For now, this is a placeholder
            
            // Example of how it might look:
            /*
            float attackPower = 10.0f;
            if (entity.HasComponent<AttackComponent>())
            {
                attackPower = entity.GetComponent<AttackComponent>().AttackPower;
            }
            
            float attackRange = 2.0f;
            
            foreach (var enemyEntity in _world.GetEntitiesWith<HealthComponent, PositionComponent>())
            {
                var enemyPosition = enemyEntity.GetComponent<PositionComponent>().Position;
                
                // Check if enemy is in front of us in charge direction
                Vector3 toEnemy = enemyPosition - positionComponent.Position;
                float distance = toEnemy.magnitude;
                
                if (distance < attackRange && Vector3.Dot(toEnemy.normalized, chargeComponent.ChargeDirection) > 0.7f)
                {
                    // Calculate damage based on current speed
                    float speedFactor = 1.0f;
                    if (entity.HasComponent<VelocityComponent>())
                    {
                        var velocity = entity.GetComponent<VelocityComponent>();
                        speedFactor = velocity.Velocity.magnitude / velocity.MaxSpeed;
                    }
                    
                    float bonusDamage = Mathf.Clamp01(speedFactor) * chargeComponent.ChargeDamageMultiplier;
                    float damage = attackPower * (1.0f + bonusDamage);
                    
                    // Deal damage
                    enemyEntity.GetComponent<HealthComponent>().TakeDamage(damage);
                    
                    // Apply knockback
                    if (enemyEntity.HasComponent<VelocityComponent>())
                    {
                        enemyEntity.GetComponent<VelocityComponent>().Velocity += 
                            chargeComponent.ChargeDirection * damage * 0.5f;
                    }
                }
            }
            */
        }
    }
}