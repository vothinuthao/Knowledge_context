using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class SpecializedBehaviorSystem : BaseSystem
    {
        private Dictionary<int, Vector3> _squadCenters = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadRotations = new Dictionary<int, Quaternion>();
        
        public override void Execute()
        {
            // Get all entities with weighted behavior components
            var entities = EntityRegistry.GetEntitiesWithComponent<WeightedBehaviorComponent>();
            
            // First calculate squad centers and rotations
            CalculateSquadCenters(entities);
            
            // Then update specialized behaviors for each entity
            foreach (var entity in entities)
            {
                var weightedBehaviorComponent = entity.GetComponent<WeightedBehaviorComponent>();
                if (weightedBehaviorComponent == null)
                    continue;
                    
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent == null)
                    continue;
                    
                int squadId = formationComponent.SquadId;
                
                // Update formation-based behaviors
                UpdateFormationBehaviors(entity, weightedBehaviorComponent, squadId);
                
                // Update role-based behaviors
                UpdateRoleBasedBehaviors(entity, weightedBehaviorComponent);
            }
        }

        private void CalculateSquadCenters(List<IEntity> entities)
        {
            // Clear previous data
            _squadCenters.Clear();
            _squadRotations.Clear();
            
            // Group entities by squad
            Dictionary<int, List<Vector3>> squadPositions = new Dictionary<int, List<Vector3>>();
            Dictionary<int, List<Vector3>> squadForwards = new Dictionary<int, List<Vector3>>();
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                var transformComponent = entity.GetComponent<TransformComponent>();
                
                if (formationComponent != null && transformComponent != null)
                {
                    int squadId = formationComponent.SquadId;
                    
                    // Add position to squad positions
                    if (!squadPositions.ContainsKey(squadId))
                    {
                        squadPositions[squadId] = new List<Vector3>();
                        squadForwards[squadId] = new List<Vector3>();
                    }
                    
                    squadPositions[squadId].Add(transformComponent.Position);
                    squadForwards[squadId].Add(transformComponent.Forward);
                }
            }
            
            // Calculate center and average rotation for each squad
            foreach (var squadId in squadPositions.Keys)
            {
                var positions = squadPositions[squadId];
                var forwards = squadForwards[squadId];
                
                if (positions.Count > 0)
                {
                    // Calculate average position
                    Vector3 sum = Vector3.zero;
                    foreach (var pos in positions)
                    {
                        sum += pos;
                    }
                    
                    Vector3 center = sum / positions.Count;
                    _squadCenters[squadId] = center;
                    
                    // Calculate average forward direction
                    Vector3 averageForward = Vector3.zero;
                    foreach (var fwd in forwards)
                    {
                        averageForward += fwd;
                    }
                    
                    if (averageForward.magnitude > 0.01f)
                    {
                        averageForward.Normalize();
                        _squadRotations[squadId] = Quaternion.LookRotation(averageForward);
                    }
                    else
                    {
                        _squadRotations[squadId] = Quaternion.identity;
                    }
                }
            }
        }

        private void UpdateFormationBehaviors(IEntity entity, WeightedBehaviorComponent behaviorComponent, int squadId)
        {
            // Get formation type
            var formationComponent = entity.GetComponent<FormationComponent>();
            if (formationComponent == null)
                return;
                
            FormationType formationType = formationComponent.CurrentFormationType;
            
            // Find existing formation behaviors or create new ones based on formation type
            switch (formationType)
            {
                case FormationType.Phalanx:
                    UpdatePhalanxBehavior(entity, behaviorComponent, squadId);
                    break;
                    
                case FormationType.Testudo:
                    UpdateTestudoBehavior(entity, behaviorComponent, squadId);
                    break;
                    
                case FormationType.Circle:
                    UpdateSurroundBehavior(entity, behaviorComponent, squadId);
                    break;
                    
                // Could add more cases for other formation types
            }
        }

        private void UpdatePhalanxBehavior(IEntity entity, WeightedBehaviorComponent behaviorComponent, int squadId)
        {
            // Check if we have squad info
            if (!_squadCenters.TryGetValue(squadId, out Vector3 center) || 
                !_squadRotations.TryGetValue(squadId, out Quaternion rotation))
                return;
                
            // Find existing phalanx behavior or create a new one
            PhalanxBehavior phalanxBehavior = GetBehaviorOfType<PhalanxBehavior>(behaviorComponent);
            
            if (phalanxBehavior == null)
            {
                phalanxBehavior = new PhalanxBehavior(entity);
                behaviorComponent.AddBehavior(phalanxBehavior);
            }
            
            // Update squad info
            phalanxBehavior.SetSquadInfo(center, rotation);
        }

        private void UpdateTestudoBehavior(IEntity entity, WeightedBehaviorComponent behaviorComponent, int squadId)
        {
            // Check if we have squad info
            if (!_squadCenters.TryGetValue(squadId, out Vector3 center) || 
                !_squadRotations.TryGetValue(squadId, out Quaternion rotation))
                return;
                
            // Find existing testudo behavior or create a new one
            TestudoBehavior testudoBehavior = GetBehaviorOfType<TestudoBehavior>(behaviorComponent);
            
            if (testudoBehavior == null)
            {
                testudoBehavior = new TestudoBehavior(entity);
                behaviorComponent.AddBehavior(testudoBehavior);
            }
            
            // Update squad info
            testudoBehavior.SetSquadInfo(center, rotation);
        }

        private void UpdateSurroundBehavior(IEntity entity, WeightedBehaviorComponent behaviorComponent, int squadId)
        {
            // Get position index for even distribution
            var formationComponent = entity.GetComponent<FormationComponent>();
            if (formationComponent == null)
                return;
                
            int positionIndex = formationComponent.FormationSlotIndex;
            
            // Find existing surround behavior or create a new one
            SurroundBehavior surroundBehavior = GetBehaviorOfType<SurroundBehavior>(behaviorComponent);
            
            if (surroundBehavior == null)
            {
                surroundBehavior = new SurroundBehavior(entity, positionIndex);
                behaviorComponent.AddBehavior(surroundBehavior);
            }
            
            // Find target to surround (simplified)
            surroundBehavior.SetTargetEntity(FindSurroundTarget(entity));
        }

        private void UpdateRoleBasedBehaviors(IEntity entity, WeightedBehaviorComponent behaviorComponent)
        {
            // Get unit type to determine role
            var unitTypeComponent = entity.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent == null)
                return;
                
            // Role-based behavior assignments
            switch (unitTypeComponent.UnitType)
            {
                case UnitType.Infantry:
                    // Infantry often protects
                    UpdateProtectBehavior(entity, behaviorComponent);
                    
                    // Infantry can charge
                    UpdateChargeBehavior(entity, behaviorComponent);
                    break;
                    
                case UnitType.Archer:
                    // Archers often seek cover
                    UpdateCoverBehavior(entity, behaviorComponent);
                    
                    // Archers can use ambush
                    UpdateAmbushBehavior(entity, behaviorComponent);
                    break;
                    
                case UnitType.Pike:
                    // Pike can charge
                    UpdateChargeBehavior(entity, behaviorComponent);
                    break;
            }
        }

        private void UpdateProtectBehavior(IEntity entity, WeightedBehaviorComponent behaviorComponent)
        {
            // Find existing protect behavior or create a new one
            ProtectBehavior protectBehavior = GetBehaviorOfType<ProtectBehavior>(behaviorComponent);
            
            if (protectBehavior == null)
            {
                protectBehavior = new ProtectBehavior(entity);
                behaviorComponent.AddBehavior(protectBehavior);
            }
            
            // Find entity to protect (simplified)
            IEntity protectedEntity = FindEntityToProtect(entity);
            if (protectedEntity != null)
            {
                protectBehavior.SetProtectedEntity(protectedEntity);
            }
        }

        private void UpdateCoverBehavior(IEntity entity, WeightedBehaviorComponent behaviorComponent)
        {
            // Find existing cover behavior or create a new one
            CoverBehavior coverBehavior = GetBehaviorOfType<CoverBehavior>(behaviorComponent);
            
            if (coverBehavior == null)
            {
                coverBehavior = new CoverBehavior(entity);
                behaviorComponent.AddBehavior(coverBehavior);
            }
            
            // Find entity to use as cover (simplified)
            IEntity protectorEntity = FindProtectorEntity(entity);
            if (protectorEntity != null)
            {
                coverBehavior.SetProtectorEntity(protectorEntity);
            }
        }

        private void UpdateChargeBehavior(IEntity entity, WeightedBehaviorComponent behaviorComponent)
        {
            // Find existing charge behavior or create a new one
            ChargeBehavior chargeBehavior = GetBehaviorOfType<ChargeBehavior>(behaviorComponent);
            
            if (chargeBehavior == null)
            {
                chargeBehavior = new ChargeBehavior(entity);
                behaviorComponent.AddBehavior(chargeBehavior);
            }
            
            // Find charge target (simplified)
            Vector3 targetPosition = FindChargeTarget(entity);
            chargeBehavior.SetChargeTarget(targetPosition);
        }

        private void UpdateAmbushBehavior(IEntity entity, WeightedBehaviorComponent behaviorComponent)
        {
            // Find existing ambush behavior or create a new one
            AmbushMoveBehavior ambushBehavior = GetBehaviorOfType<AmbushMoveBehavior>(behaviorComponent);
            
            if (ambushBehavior == null)
            {
                ambushBehavior = new AmbushMoveBehavior(entity);
                behaviorComponent.AddBehavior(ambushBehavior);
            }
            
            // Find ambush target (simplified)
            Vector3 targetPosition = FindAmbushTarget(entity);
            ambushBehavior.SetTargetPosition(targetPosition);
        }

        // Helper methods to find targets/entities for behaviors
        private IEntity FindSurroundTarget(IEntity entity)
        {
            // Simplified implementation
            // In a real game, this would be more sophisticated
            
            var aggroComponent = entity.GetComponent<AggroDetectionComponent>();
            if (aggroComponent != null && aggroComponent.HasEnemyInRange())
            {
                return aggroComponent.GetClosestEnemy();
            }
            
            return null;
        }

        private IEntity FindEntityToProtect(IEntity entity)
        {
            // Simplified implementation
            // In a real game, you'd have a more sophisticated squad role system
            
            // Find an archer in the same squad to protect
            var formationComponent = entity.GetComponent<FormationComponent>();
            if (formationComponent == null)
                return null;
                
            int squadId = formationComponent.SquadId;
            
            // Find archers in the same squad
            var entityRegistry = EntityRegistry.GetEntitiesWithComponent<UnitTypeComponent>();
            
            foreach (var potentialTarget in entityRegistry)
            {
                var targetFormation = potentialTarget.GetComponent<FormationComponent>();
                var targetUnitType = potentialTarget.GetComponent<UnitTypeComponent>();
                
                if (targetFormation != null && targetUnitType != null && 
                    targetFormation.SquadId == squadId && targetUnitType.UnitType == UnitType.Archer)
                {
                    return potentialTarget;
                }
            }
            
            return null;
        }

        private IEntity FindProtectorEntity(IEntity entity)
        {
            // Simplified implementation
            // In a real game, you'd have a more sophisticated squad role system
            
            // Find an infantry in the same squad to use as protection
            var formationComponent = entity.GetComponent<FormationComponent>();
            if (formationComponent == null)
                return null;
                
            int squadId = formationComponent.SquadId;
            
            // Find infantry in the same squad
            var entityRegistry = EntityRegistry.GetEntitiesWithComponent<UnitTypeComponent>();
            
            foreach (var potentialProtector in entityRegistry)
            {
                var protectorFormation = potentialProtector.GetComponent<FormationComponent>();
                var protectorUnitType = potentialProtector.GetComponent<UnitTypeComponent>();
                
                if (protectorFormation != null && protectorUnitType != null && 
                    protectorFormation.SquadId == squadId && protectorUnitType.UnitType == UnitType.Infantry)
                {
                    return potentialProtector;
                }
            }
            
            return null;
        }

        private Vector3 FindChargeTarget(IEntity entity)
        {
            // Simplified implementation
            // In a real game, you'd implement strategic target selection
            
            var aggroComponent = entity.GetComponent<AggroDetectionComponent>();
            if (aggroComponent != null && aggroComponent.HasEnemyInRange())
            {
                var enemy = aggroComponent.GetClosestEnemy();
                var enemyTransform = enemy?.GetComponent<TransformComponent>();
                
                if (enemyTransform != null)
                {
                    return enemyTransform.Position;
                }
            }
            
            // Default - ahead of current position
            var transformComponent = entity.GetComponent<TransformComponent>();
            if (transformComponent != null)
            {
                return transformComponent.Position + transformComponent.Forward * 10f;
            }
            
            return Vector3.zero;
        }

        private Vector3 FindAmbushTarget(IEntity entity)
        {
            // Simplified implementation
            // In a real game, you'd implement strategic ambush position selection
            
            // Default - find a position away from enemies but towards potential targets
            var transformComponent = entity.GetComponent<TransformComponent>();
            if (transformComponent != null)
            {
                return transformComponent.Position + transformComponent.Forward * 15f;
            }
            
            return Vector3.zero;
        }

        private T GetBehaviorOfType<T>(WeightedBehaviorComponent behaviorComponent) where T : Core.Behavior.IBehavior
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
    }
}