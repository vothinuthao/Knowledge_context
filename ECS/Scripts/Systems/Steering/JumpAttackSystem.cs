using Core.ECS;
using Movement;
using Steering;
using UnityEngine;
using Combat;
using Components;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes jump attack behavior
    /// </summary>
    public class JumpAttackSystem : ISystem
    {
        private World _world;
        
        public int Priority => 110; // Very high priority
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<JumpAttackComponent, SteeringDataComponent, PositionComponent>())
            {
                var jumpComponent = entity.GetComponent<JumpAttackComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!jumpComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                // Update cooldown
                if (jumpComponent.CooldownTimer > 0)
                {
                    jumpComponent.CooldownTimer -= deltaTime;
                    continue;
                }
                
                // Process jumping state
                if (jumpComponent.IsJumping)
                {
                    ProcessJumping(entity, jumpComponent, steeringData, positionComponent, deltaTime);
                    continue;
                }
                
                // Check for jump opportunity
                TryStartJump(entity, jumpComponent, steeringData, positionComponent);
            }
        }
        
        private void ProcessJumping(Entity entity, JumpAttackComponent jumpComponent, 
            SteeringDataComponent steeringData, PositionComponent positionComponent, float deltaTime)
        {
            // Update jump progress
            jumpComponent.JumpProgress += deltaTime * jumpComponent.JumpSpeed;
            
            // If jump complete
            if (jumpComponent.JumpProgress >= 1.0f)
            {
                // Complete jump
                jumpComponent.IsJumping = false;
                jumpComponent.CooldownTimer = jumpComponent.Cooldown;
                
                // Check for damage to enemies near landing spot
                DealJumpDamage(entity, jumpComponent, positionComponent);
                
                return;
            }
            
            // Calculate jump position along a curve
            Vector3 start = positionComponent.Position;
            Vector3 target = jumpComponent.JumpTarget;
            float height = jumpComponent.JumpHeight * Mathf.Sin(jumpComponent.JumpProgress * Mathf.PI);
            
            // Interpolate between start and target
            Vector3 horizontalPos = Vector3.Lerp(start, target, jumpComponent.JumpProgress);
            
            // Add height component
            Vector3 nextPos = new Vector3(horizontalPos.x, horizontalPos.y + height, horizontalPos.z);
            
            // Calculate velocity needed to reach nextPos
            Vector3 velocity = (nextPos - positionComponent.Position) / deltaTime;
            
            // Create steering force
            Vector3 jumpForce = velocity;
            if (entity.HasComponent<VelocityComponent>())
            {
                jumpForce -= entity.GetComponent<VelocityComponent>().Velocity;
            }
            
            // Add force to steering data (with high weight to override other behaviors)
            steeringData.AddForce(jumpForce * 3.0f);
        }
        
        private void TryStartJump(Entity entity, JumpAttackComponent jumpComponent, 
            SteeringDataComponent steeringData, PositionComponent positionComponent)
        {
            // Look for enemy targets in jump range
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
                    
                    // Check if in jump range but outside melee range
                    // For simplicity, we'll assume attack range is 2 units
                    float attackRange = 2.0f;
                    if (entity.HasComponent<AttackComponent>())
                    {
                        // In a real implementation, you'd get the attack range from the component
                        // attackRange = entity.GetComponent<AttackComponent>().AttackRange;
                    }
                    
                    if (distance <= jumpComponent.JumpRange && distance > attackRange)
                    {
                        // Start jump
                        jumpComponent.IsJumping = true;
                        jumpComponent.JumpTarget = enemyPosition;
                        jumpComponent.JumpProgress = 0.0f;
                        
                        // Apply initial jump force
                        Vector3 jumpDirection = (enemyPosition - positionComponent.Position).normalized;
                        Vector3 jumpForce = jumpDirection * jumpComponent.JumpSpeed * 2.0f;
                        jumpForce.y = jumpComponent.JumpHeight;
                        
                        steeringData.AddForce(jumpForce * 3.0f); // High weight to override others
                        
                        return; // Only jump at one enemy
                    }
                }
            }
        }
        
        private void DealJumpDamage(Entity entity, JumpAttackComponent jumpComponent, PositionComponent positionComponent)
        {
            // In a real implementation, this would deal damage to nearby enemies
            // For now, this is a placeholder
            
            // Example of how it might look:
            /*
            float attackPower = 10.0f;
            if (entity.HasComponent<AttackComponent>())
            {
                attackPower = entity.GetComponent<AttackComponent>().AttackPower;
            }
            
            float attackRange = 3.0f; // Slightly larger than normal
            
            foreach (var enemyEntity in _world.GetEntitiesWith<HealthComponent, PositionComponent>())
            {
                var enemyPosition = enemyEntity.GetComponent<PositionComponent>().Position;
                float distance = Vector3.Distance(positionComponent.Position, enemyPosition);
                
                if (distance <= attackRange)
                {
                    // Deal damage with jump attack multiplier
                    float damage = attackPower * jumpComponent.DamageMultiplier;
                    enemyEntity.GetComponent<HealthComponent>().TakeDamage(damage);
                    
                    // Apply knockback
                    if (enemyEntity.HasComponent<VelocityComponent>())
                    {
                        Vector3 knockbackDirection = (enemyPosition - positionComponent.Position).normalized;
                        enemyEntity.GetComponent<VelocityComponent>().Velocity += knockbackDirection * damage * 0.5f;
                    }
                }
            }
            */
        }
    }
}