using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Behaviors;
using VikingRaven.Units.Components;

namespace VikingRaven.Combat.Components
{
    public class TacticalExecutionSystem : BaseSystem
    {
        public override void Execute()
        {
            // Execute tactical objectives for all units with tactical components
            var entities = EntityRegistry.GetEntitiesWithComponent<TacticalComponent>();
            
            foreach (var entity in entities)
            {
                var tacticalComponent = entity.GetComponent<TacticalComponent>();
                
                if (tacticalComponent != null && tacticalComponent.IsActive)
                {
                    ExecuteTacticalObjective(entity, tacticalComponent);
                }
            }
        }
        
        private void ExecuteTacticalObjective(IEntity entity, TacticalComponent tacticalComponent)
        {
            // Execute the assigned tactical objective by adjusting behaviors
            var behaviorComponent = entity.GetComponent<WeightedBehaviorComponent>();
            
            if (behaviorComponent == null || behaviorComponent.BehaviorManager == null)
                return;
                
            switch (tacticalComponent.CurrentObjective)
            {
                case TacticalObjective.Attack:
                    ExecuteAttackObjective(entity, tacticalComponent, behaviorComponent);
                    break;
                    
                case TacticalObjective.Defend:
                    ExecuteDefendObjective(entity, tacticalComponent, behaviorComponent);
                    break;
                    
                case TacticalObjective.Move:
                    ExecuteMoveObjective(entity, tacticalComponent, behaviorComponent);
                    break;
                    
                case TacticalObjective.Hold:
                    ExecuteHoldObjective(entity, tacticalComponent, behaviorComponent);
                    break;
                    
                case TacticalObjective.Retreat:
                    ExecuteRetreatObjective(entity, tacticalComponent, behaviorComponent);
                    break;
                    
                case TacticalObjective.Scout:
                    ExecuteScoutObjective(entity, tacticalComponent, behaviorComponent);
                    break;
            }
        }
        
        private void ExecuteAttackObjective(IEntity entity, TacticalComponent tacticalComponent, 
                                           WeightedBehaviorComponent behaviorComponent)
        {
            // Get or create attack behavior
            AttackBehavior attackBehavior = GetBehaviorOrCreate<AttackBehavior>(entity, behaviorComponent);
            
            if (attackBehavior != null)
            {
                // Increase attack weight
                attackBehavior.CalculateWeight(); // Recalculate base weight
                attackBehavior.Weight *= 1.5f; // Boost attack priority
            }
            
            // For flankers or with specific target, use Surround behavior
            if (tacticalComponent.AssignedRole == TacticalRole.Flanker && 
                tacticalComponent.ObjectiveTarget != null)
            {
                SurroundBehavior surroundBehavior = GetBehaviorOrCreate<SurroundBehavior>(entity, behaviorComponent);
                
                if (surroundBehavior != null)
                {
                    surroundBehavior.SetTargetEntity(tacticalComponent.ObjectiveTarget);
                    surroundBehavior.Weight *= 1.3f;
                }
            }
            
            // For infantry with a clear target, consider using Charge
            if (entity.GetComponent<UnitTypeComponent>()?.UnitType == UnitType.Infantry &&
                tacticalComponent.ObjectiveTarget != null)
            {
                ChargeBehavior chargeBehavior = GetBehaviorOrCreate<ChargeBehavior>(entity, behaviorComponent);
                
                if (chargeBehavior != null)
                {
                    var targetTransform = tacticalComponent.ObjectiveTarget.GetComponent<TransformComponent>();
                    
                    if (targetTransform != null)
                    {
                        chargeBehavior.SetChargeTarget(targetTransform.Position);
                        // Weight will be calculated by the behavior
                    }
                }
            }
        }
        
        private void ExecuteDefendObjective(IEntity entity, TacticalComponent tacticalComponent, 
                                           WeightedBehaviorComponent behaviorComponent)
        {
            // Get unit type
            var unitTypeComponent = entity.GetComponent<UnitTypeComponent>();
            
            if (unitTypeComponent == null)
                return;
                
            switch (unitTypeComponent.UnitType)
            {
                case UnitType.Infantry:
                    // Infantry protects others
                    ProtectBehavior protectBehavior = GetBehaviorOrCreate<ProtectBehavior>(entity, behaviorComponent);
                    
                    if (protectBehavior != null)
                    {
                        // Find nearby archers or vulnerable units to protect
                        IEntity unitToProtect = FindNearbyUnitToProtect(entity);
                        
                        if (unitToProtect != null)
                        {
                            protectBehavior.SetProtectedEntity(unitToProtect);
                            protectBehavior.Weight *= 1.5f;
                        }
                    }
                    break;
                    
                case UnitType.Pike:
                    // Pike units form defensive formations
                    PhalanxBehavior phalanxBehavior = GetBehaviorOrCreate<PhalanxBehavior>(entity, behaviorComponent);
                    
                    if (phalanxBehavior != null)
                    {
                        // Get squad info
                        var formationComponent = entity.GetComponent<FormationComponent>();
                        
                        if (formationComponent != null)
                        {
                            Vector3 squadCenter = tacticalComponent.ObjectivePosition;
                            Quaternion squadRotation = GetSquadFacingRotation(entity, formationComponent.SquadId);
                            
                            phalanxBehavior.SetSquadInfo(squadCenter, squadRotation);
                            phalanxBehavior.Weight *= 1.5f;
                        }
                    }
                    break;
                    
                case UnitType.Archer:
                    // Archers seek cover
                    CoverBehavior coverBehavior = GetBehaviorOrCreate<CoverBehavior>(entity, behaviorComponent);
                    
                    if (coverBehavior != null)
                    {
                        // Find nearby infantry or pike for cover
                        IEntity protector = FindNearbyProtector(entity);
                        
                        if (protector != null)
                        {
                            coverBehavior.SetProtectorEntity(protector);
                            coverBehavior.Weight *= 1.5f;
                        }
                    }
                    break;
            }
        }
        
        private void ExecuteMoveObjective(IEntity entity, TacticalComponent tacticalComponent, 
                                         WeightedBehaviorComponent behaviorComponent)
        {
            // Get or create move behavior
            MoveBehavior moveBehavior = GetBehaviorOrCreate<MoveBehavior>(entity, behaviorComponent);
            
            if (moveBehavior != null)
            {
                // Set target position
                moveBehavior.SetTargetPosition(tacticalComponent.ObjectivePosition);
                moveBehavior.Weight *= 1.3f;
            }
            
            // If this is for stealth movement, use Ambush
            if (tacticalComponent.AssignedRole == TacticalRole.Scout)
            {
                AmbushMoveBehavior ambushBehavior = GetBehaviorOrCreate<AmbushMoveBehavior>(entity, behaviorComponent);
                
                if (ambushBehavior != null)
                {
                    ambushBehavior.SetTargetPosition(tacticalComponent.ObjectivePosition);
                    ambushBehavior.Weight *= 1.2f;
                }
            }
        }
        
        private void ExecuteHoldObjective(IEntity entity, TacticalComponent tacticalComponent, 
                                         WeightedBehaviorComponent behaviorComponent)
        {
            // Similar to defend but emphasizes staying in position
            // Lower movement behavior weights
            MoveBehavior moveBehavior = GetBehavior<MoveBehavior>(behaviorComponent);
            
            if (moveBehavior != null)
            {
                moveBehavior.Weight *= 0.5f; // Reduce movement priority
            }
            
            // Increase defensive formation weights
            var unitTypeComponent = entity.GetComponent<UnitTypeComponent>();
            
            if (unitTypeComponent != null)
            {
                switch (unitTypeComponent.UnitType)
                {
                    case UnitType.Infantry:
                        TestudoBehavior testudoBehavior = GetBehaviorOrCreate<TestudoBehavior>(entity, behaviorComponent);
                        
                        if (testudoBehavior != null)
                        {
                            // Set up defensive formation
                            var formationComponent = entity.GetComponent<FormationComponent>();
                            
                            if (formationComponent != null)
                            {
                                Vector3 squadCenter = tacticalComponent.ObjectivePosition;
                                Quaternion squadRotation = GetSquadFacingRotation(entity, formationComponent.SquadId);
                                
                                testudoBehavior.SetSquadInfo(squadCenter, squadRotation);
                                testudoBehavior.Weight *= 1.8f;
                            }
                        }
                        break;
                        
                    case UnitType.Pike:
                        PhalanxBehavior phalanxBehavior = GetBehaviorOrCreate<PhalanxBehavior>(entity, behaviorComponent);
                        
                        if (phalanxBehavior != null)
                        {
                            // Set up defensive formation
                            var formationComponent = entity.GetComponent<FormationComponent>();
                            
                            if (formationComponent != null)
                            {
                                Vector3 squadCenter = tacticalComponent.ObjectivePosition;
                                Quaternion squadRotation = GetSquadFacingRotation(entity, formationComponent.SquadId);
                                
                                phalanxBehavior.SetSquadInfo(squadCenter, squadRotation);
                                phalanxBehavior.Weight *= 1.8f;
                            }
                        }
                        break;
                }
            }
        }
        
        private void ExecuteRetreatObjective(IEntity entity, TacticalComponent tacticalComponent, 
                                           WeightedBehaviorComponent behaviorComponent)
        {
            // Get threat assessment to determine retreat direction
            var threatComponent = entity.GetComponent<ThreatAssessmentComponent>();
            Vector3 retreatDirection = Vector3.zero;
            
            if (threatComponent != null)
            {
                // Get safest direction
                retreatDirection = threatComponent.GetSafestDirection(20f);
            }
            
            if (retreatDirection == Vector3.zero)
            {
                // Fallback - retreat away from primary threat direction
                retreatDirection = -tacticalComponent.ObjectivePosition.normalized;
            }
            
            // Get entity position
            var transformComponent = entity.GetComponent<TransformComponent>();
            
            if (transformComponent != null)
            {
                // Calculate retreat position
                Vector3 retreatPosition = transformComponent.Position + retreatDirection * 20f;
                
                // Set move behavior to retreat position
                MoveBehavior moveBehavior = GetBehaviorOrCreate<MoveBehavior>(entity, behaviorComponent);
                
                if (moveBehavior != null)
                {
                    moveBehavior.SetTargetPosition(retreatPosition);
                    moveBehavior.Weight *= 2.0f; // High priority for retreat
                }
                
                // Reduce attack behavior weights
                AttackBehavior attackBehavior = GetBehavior<AttackBehavior>(behaviorComponent);
                
                if (attackBehavior != null)
                {
                    attackBehavior.Weight *= 0.3f; // Reduce attack priority
                }
            }
        }
        
        private void ExecuteScoutObjective(IEntity entity, TacticalComponent tacticalComponent, 
                                         WeightedBehaviorComponent behaviorComponent)
        {
            // Scouts move stealthily to observe enemy positions
            AmbushMoveBehavior ambushBehavior = GetBehaviorOrCreate<AmbushMoveBehavior>(entity, behaviorComponent);
            
            if (ambushBehavior != null)
            {
                ambushBehavior.SetTargetPosition(tacticalComponent.ObjectivePosition);
                ambushBehavior.Weight *= 1.8f; // High priority for scouts
            }
            
            // Reduce attack weights
            AttackBehavior attackBehavior = GetBehavior<AttackBehavior>(behaviorComponent);
            
            if (attackBehavior != null)
            {
                attackBehavior.Weight *= 0.2f; // Scouts avoid combat
            }
        }
        
        private IEntity FindNearbyUnitToProtect(IEntity entity)
        {
            // Find nearby archers or vulnerable units to protect
            // In a real implementation, you would use spatial partitioning for efficiency
            
            var transformComponent = entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                return null;
                
            Vector3 position = transformComponent.Position;
            float protectRadius = 15f;
            IEntity bestTarget = null;
            float bestScore = 0f;
            
            // Get all entities with health component
            var entityRegistry = GameObject.FindObjectOfType<Core.ECS.EntityRegistry>();
            if (entityRegistry == null)
                return null;
                
            var potentialTargets = entityRegistry.GetEntitiesWithComponent<HealthComponent>();
            
            foreach (var target in potentialTargets)
            {
                // Skip self
                if (target == entity)
                    continue;
                    
                // Check faction (simplified)
                bool isAlly = true; // Replace with proper faction check
                
                if (!isAlly)
                    continue;
                    
                var targetTransform = target.GetComponent<TransformComponent>();
                var targetHealth = target.GetComponent<HealthComponent>();
                var targetUnitType = target.GetComponent<UnitTypeComponent>();
                
                if (targetTransform == null || targetHealth == null || targetUnitType == null)
                    continue;
                    
                // Check distance
                float distance = Vector3.Distance(position, targetTransform.Position);
                
                if (distance > protectRadius)
                    continue;
                    
                // Score based on unit type, health, and distance
                float score = 0f;
                
                // Prioritize archers
                if (targetUnitType.UnitType == UnitType.Archer)
                {
                    score += 2f;
                }
                
                // Prioritize low health units
                if (targetHealth.HealthPercentage < 0.5f)
                {
                    score += (1f - targetHealth.HealthPercentage) * 2f;
                }
                
                // Prioritize closer units
                score += (1f - distance / protectRadius);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                }
            }
            
            return bestTarget;
        }
        
        private IEntity FindNearbyProtector(IEntity entity)
        {
            // Find nearby infantry or pike for cover
            // Similar to FindNearbyUnitToProtect but looking for protectors
            
            var transformComponent = entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                return null;
                
            Vector3 position = transformComponent.Position;
            float protectRadius = 15f;
            IEntity bestProtector = null;
            float bestScore = 0f;
            
            // Get all entities with unit type component
            var entityRegistry = GameObject.FindObjectOfType<Core.ECS.EntityRegistry>();
            if (entityRegistry == null)
                return null;
                
            var potentialProtectors = entityRegistry.GetEntitiesWithComponent<UnitTypeComponent>();
            
            foreach (var protector in potentialProtectors)
            {
                // Skip self
                if (protector == entity)
                    continue;
                    
                // Check faction (simplified)
                bool isAlly = true; // Replace with proper faction check
                
                if (!isAlly)
                    continue;
                    
                var protectorTransform = protector.GetComponent<TransformComponent>();
                var protectorUnitType = protector.GetComponent<UnitTypeComponent>();
                
                if (protectorTransform == null || protectorUnitType == null)
                    continue;
                    
                // Only infantry and pike can provide cover
                if (protectorUnitType.UnitType != UnitType.Infantry && 
                    protectorUnitType.UnitType != UnitType.Pike)
                    continue;
                    
                // Check distance
                float distance = Vector3.Distance(position, protectorTransform.Position);
                
                if (distance > protectRadius)
                    continue;
                    
                // Score based on unit type and distance
                float score = 0f;
                
                // Infantry is better for cover
                if (protectorUnitType.UnitType == UnitType.Infantry)
                {
                    score += 1.5f;
                }
                
                // Prioritize closer units
                score += (1f - distance / protectRadius) * 2f;
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestProtector = protector;
                }
            }
            
            return bestProtector;
        }
        
        private Quaternion GetSquadFacingRotation(IEntity entity, int squadId)
        {
            // Determine which way the squad should face
            
            // First check for nearby threats
            Vector3 threatDirection = Vector3.zero;
            
            // Get all entities with the same squad ID
            var entityRegistry = GameObject.FindObjectOfType<Core.ECS.EntityRegistry>();
            if (entityRegistry == null)
                return Quaternion.identity;
                
            var squadMembers = entityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            foreach (var member in squadMembers)
            {
                var formationComponent = member.GetComponent<FormationComponent>();
                
                if (formationComponent != null && formationComponent.SquadId == squadId)
                {
                    var threatComponent = member.GetComponent<ThreatAssessmentComponent>();
                    
                    if (threatComponent != null)
                    {
                        // Get highest threat
                        IEntity highestThreat = threatComponent.GetHighestThreat();
                        
                        if (highestThreat != null)
                        {
                            var memberTransform = member.GetComponent<TransformComponent>();
                            var threatTransform = highestThreat.GetComponent<TransformComponent>();
                            
                            if (memberTransform != null && threatTransform != null)
                            {
                                Vector3 direction = (threatTransform.Position - memberTransform.Position).normalized;
                                threatDirection += direction;
                            }
                        }
                    }
                }
            }
            
            // If there's a clear threat direction, face that way
            if (threatDirection.magnitude > 0.1f)
            {
                threatDirection.Normalize();
                return Quaternion.LookRotation(threatDirection);
            }
            
            // Otherwise use the transform's current rotation
            var entityTransform = entity.GetComponent<TransformComponent>();
            
            return (entityTransform != null) ? entityTransform.Rotation : Quaternion.identity;
        }
        
        private T GetBehavior<T>(WeightedBehaviorComponent behaviorComponent) where T : Core.Behavior.IBehavior
        {
            // NOTE: This is a simplified implementation
            // In a real game, you'd track behaviors within WeightedBehaviorManager
            
            // Get behaviors using reflection (for demonstration only)
            var behaviorManager = behaviorComponent.BehaviorManager;
            if (behaviorManager == null)
                return default(T);
                
            var field = behaviorManager.GetType().GetField("_behaviors", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
                
            if (field != null)
            {
                var fieldValue = field.GetValue(behaviorManager) as List<Core.Behavior.IBehavior>;
                if (fieldValue != null)
                {
                    foreach (var behavior in fieldValue)
                    {
                        if (behavior is T)
                        {
                            return (T)behavior;
                        }
                    }
                }
            }
            
            return default(T);
        }
        
        private T GetBehaviorOrCreate<T>(IEntity entity, WeightedBehaviorComponent behaviorComponent) where T : Core.Behavior.IBehavior
        {
            // Try to get existing behavior
            T behavior = GetBehavior<T>(behaviorComponent);
            
            if (behavior != null)
                return behavior;
                
            // If not found, create new instance
            if (typeof(T) == typeof(MoveBehavior))
            {
                behavior = (T)(object)new MoveBehavior(entity);
            }
            else if (typeof(T) == typeof(AttackBehavior))
            {
                behavior = (T)(object)new AttackBehavior(entity);
            }
            else if (typeof(T) == typeof(StrafeBehavior))
            {
                behavior = (T)(object)new StrafeBehavior(entity);
            }
            else if (typeof(T) == typeof(AmbushMoveBehavior))
            {
                behavior = (T)(object)new AmbushMoveBehavior(entity);
            }
            else if (typeof(T) == typeof(SurroundBehavior))
            {
                // Get formation component for position index
                var formationComponent = entity.GetComponent<FormationComponent>();
                int posIndex = (formationComponent != null) ? formationComponent.FormationSlotIndex : 0;
                
                behavior = (T)(object)new SurroundBehavior(entity, posIndex);
            }
            else if (typeof(T) == typeof(ProtectBehavior))
            {
                behavior = (T)(object)new ProtectBehavior(entity);
            }
            else if (typeof(T) == typeof(CoverBehavior))
            {
                behavior = (T)(object)new CoverBehavior(entity);
            }
            else if (typeof(T) == typeof(PhalanxBehavior))
            {
                behavior = (T)(object)new PhalanxBehavior(entity);
            }
            else if (typeof(T) == typeof(TestudoBehavior))
            {
                behavior = (T)(object)new TestudoBehavior(entity);
            }
            else if (typeof(T) == typeof(ChargeBehavior))
            {
                behavior = (T)(object)new ChargeBehavior(entity);
            }
            
            // Add the new behavior to the manager
            if (behavior != null)
            {
                behaviorComponent.AddBehavior(behavior);
            }
            
            return behavior;
        }
    }
}