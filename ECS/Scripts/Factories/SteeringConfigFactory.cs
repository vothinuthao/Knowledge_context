using UnityEngine;
using Core.ECS;
using Steering;
using Steering.Config;
using Movement;
using Combat;

namespace Factories
{
    /// <summary>
    /// Factory for creating steering components from config
    /// </summary>
    public class SteeringConfigFactory
    {
        /// <summary>
        /// Apply steering configuration to an entity
        /// </summary>
        public static void ApplyConfig(Entity entity, SteeringBehaviorConfig config)
        {
            if (entity == null || config == null)
            {
                Debug.LogError("Cannot apply steering config: entity or config is null");
                return;
            }
            
            // Add steering data component if it doesn't exist
            if (!entity.HasComponent<SteeringDataComponent>())
            {
                entity.AddComponent(new SteeringDataComponent());
            }
            
            // Apply basic behaviors
            ApplySeekBehavior(entity, config.SeekSettings);
            ApplyFleeBehavior(entity, config.FleeSettings);
            ApplyArrivalBehavior(entity, config.ArrivalSettings);
            ApplySeparationBehavior(entity, config.SeparationSettings);
            ApplyCohesionBehavior(entity, config.CohesionSettings);
            ApplyAlignmentBehavior(entity, config.AlignmentSettings);
            ApplyObstacleAvoidanceBehavior(entity, config.ObstacleAvoidanceSettings);
            ApplyPathFollowingBehavior(entity, config.PathFollowingSettings);
            
            // Apply advanced behaviors
            ApplyJumpAttackBehavior(entity, config.JumpAttackSettingsBehavior);
            ApplyAmbushMoveBehavior(entity, config.AmbushMoveSettingsBehavior);
            ApplyChargeBehavior(entity, config.ChargeSettingsBehavior);
            ApplyFormationBehavior(entity, config.PhalanxSettingsBehavior);
            ApplyFormationBehavior(entity, config.TestudoSettingsBehavior);
            ApplyProtectBehavior(entity, config.ProtectSettingsBehavior);
            ApplyCoverBehavior(entity, config.CoverSettingsBehavior);
            ApplySurroundBehavior(entity, config.SurroundSettingsBehavior);
            
        }
            // Basic behaviors
        private static void ApplySeekBehavior(Entity entity, SteeringBehaviorConfig.BehaviorSettings settings)
        {
            if (!settings.Enabled) return;
            
            if (entity.HasComponent<SeekComponent>())
            {
                var component = entity.GetComponent<SeekComponent>();
                component.Weight = settings.Weight;
                component.IsEnabled = settings.Enabled;
            }
            else
            {
                entity.AddComponent(new SeekComponent(settings.Weight));
            }
        }
        
        private static void ApplyFleeBehavior(Entity entity, SteeringBehaviorConfig.BehaviorSettings settings)
        {
            if (!settings.Enabled) return;
            
            if (entity.HasComponent<FleeComponent>())
            {
                var component = entity.GetComponent<FleeComponent>();
                component.Weight = settings.Weight;
                component.IsEnabled = settings.Enabled;
                component.PanicDistance = settings.Parameter1;
            }
            else
            {
                entity.AddComponent(new FleeComponent(settings.Weight, settings.Parameter1));
            }
        }
        
        private static void ApplyArrivalBehavior(Entity entity, SteeringBehaviorConfig.BehaviorSettings settings)
        {
            if (!settings.Enabled) return;
            
            if (entity.HasComponent<ArrivalComponent>())
            {
                var component = entity.GetComponent<ArrivalComponent>();
                component.Weight = settings.Weight;
                component.IsEnabled = settings.Enabled;
                component.SlowingDistance = settings.Parameter1;
            }
            else
            {
                entity.AddComponent(new ArrivalComponent(settings.Weight, settings.Parameter1));
            }
        }
        
        private static void ApplySeparationBehavior(Entity entity, SteeringBehaviorConfig.BehaviorSettings settings)
        {
            if (!settings.Enabled) return;
            
            if (entity.HasComponent<SeparationComponent>())
            {
                var component = entity.GetComponent<SeparationComponent>();
                component.Weight = settings.Weight;
                component.IsEnabled = settings.Enabled;
                component.SeparationRadius = settings.Parameter1;
            }
            else
            {
                entity.AddComponent(new SeparationComponent(settings.Weight, settings.Parameter1));
            }
        }
        
        private static void ApplyCohesionBehavior(Entity entity, SteeringBehaviorConfig.BehaviorSettings settings)
        {
            if (!settings.Enabled) return;
            
            if (entity.HasComponent<CohesionComponent>())
            {
                var component = entity.GetComponent<CohesionComponent>();
                component.Weight = settings.Weight;
                component.IsEnabled = settings.Enabled;
                component.CohesionRadius = settings.Parameter1;
            }
            else
            {
                entity.AddComponent(new CohesionComponent(settings.Weight, settings.Parameter1));
            }
        }
        
        private static void ApplyAlignmentBehavior(Entity entity, SteeringBehaviorConfig.BehaviorSettings settings)
        {
            if (!settings.Enabled) return;
            
            if (entity.HasComponent<AlignmentComponent>())
            {
                var component = entity.GetComponent<AlignmentComponent>();
                component.Weight = settings.Weight;
                component.IsEnabled = settings.Enabled;
                component.AlignmentRadius = settings.Parameter1;
            }
            else
            {
                entity.AddComponent(new AlignmentComponent(settings.Weight, settings.Parameter1));
            }
        }
        
        private static void ApplyObstacleAvoidanceBehavior(Entity entity, SteeringBehaviorConfig.BehaviorSettings settings)
        {
            if (!settings.Enabled) return;
            
            if (entity.HasComponent<ObstacleAvoidanceComponent>())
            {
                var component = entity.GetComponent<ObstacleAvoidanceComponent>();
                component.Weight = settings.Weight;
                component.IsEnabled = settings.Enabled;
                component.AvoidDistance = settings.Parameter1;
                component.LookAheadDistance = settings.Parameter2;
            }
            else
            {
                entity.AddComponent(new ObstacleAvoidanceComponent(
                    settings.Weight, settings.Parameter1, settings.Parameter2));
            }
        }
        
        private static void ApplyPathFollowingBehavior(Entity entity, SteeringBehaviorConfig.BehaviorSettings settings)
        {
            if (!settings.Enabled) return;
            
            if (entity.HasComponent<PathFollowingComponent>())
            {
                var component = entity.GetComponent<PathFollowingComponent>();
                component.Weight = settings.Weight;
                component.IsEnabled = settings.Enabled;
                component.PathRadius = settings.Parameter1;
                component.ArrivalDistance = settings.Parameter2;
            }
            else
            {
                entity.AddComponent(new PathFollowingComponent(
                    settings.Weight, settings.Parameter1, settings.Parameter2));
            }
        }
        
        // Advanced behaviors
        
        private static void ApplyJumpAttackBehavior(Entity entity, SteeringBehaviorConfig.JumpAttackSettings settings)
        {
            if (!settings.Enabled) return;
            
            if (entity.HasComponent<JumpAttackComponent>())
            {
                var component = entity.GetComponent<JumpAttackComponent>();
                component.Weight = settings.Weight;
                component.IsEnabled = settings.Enabled;
                component.JumpRange = settings.JumpRange;
                component.JumpSpeed = settings.JumpSpeed;
                component.JumpHeight = settings.JumpHeight;
                component.DamageMultiplier = settings.DamageMultiplier;
                component.Cooldown = settings.Cooldown;
            }
            else
            {
                entity.AddComponent(new JumpAttackComponent(
                    settings.Weight, settings.JumpRange, settings.JumpSpeed,
                    settings.JumpHeight, settings.DamageMultiplier, settings.Cooldown));
            }
        }
        
        private static void ApplyAmbushMoveBehavior(Entity entity, SteeringBehaviorConfig.AmbushMoveSettings settings)
        {
            if (!settings.Enabled) return;
            
            if (entity.HasComponent<AmbushMoveComponent>())
            {
                var component = entity.GetComponent<AmbushMoveComponent>();
                component.Weight = settings.Weight;
                component.IsEnabled = settings.Enabled;
                component.MoveSpeedMultiplier = settings.MoveSpeedMultiplier;
                component.DetectionRadiusMultiplier = settings.DetectionRadiusMultiplier;
            }
            else
            {
                entity.AddComponent(new AmbushMoveComponent(
                    settings.Weight, settings.MoveSpeedMultiplier, settings.DetectionRadiusMultiplier));
            }
        }
        
        private static void ApplyChargeBehavior(Entity entity, SteeringBehaviorConfig.ChargeSettings settings)
        {
            if (!settings.Enabled) return;
            
            if (entity.HasComponent<ChargeComponent>())
            {
                var component = entity.GetComponent<ChargeComponent>();
                component.Weight = settings.Weight;
                component.IsEnabled = settings.Enabled;
                component.ChargeDistance = settings.ChargeDistance;
                component.ChargeSpeedMultiplier = settings.ChargeSpeedMultiplier;
                component.ChargeDamageMultiplier = settings.ChargeDamageMultiplier;
                component.ChargePreparationTime = settings.ChargePreparationTime;
                component.ChargeCooldown = settings.ChargeCooldown;
            }
            else
            {
                entity.AddComponent(new ChargeComponent(
                    settings.Weight, settings.ChargeDistance, settings.ChargeSpeedMultiplier,
                    settings.ChargeDamageMultiplier, settings.ChargePreparationTime, settings.ChargeCooldown));
            }
        }
        
        private static void ApplyFormationBehavior(Entity entity, SteeringBehaviorConfig.FormationSettings settings)
        {
            if (!settings.Enabled) return;
            
            switch (settings.FormationType)
            {
                case SteeringBehaviorConfig.FormationType.Phalanx:
                    if (entity.HasComponent<PhalanxComponent>())
                    {
                        var component = entity.GetComponent<PhalanxComponent>();
                        component.Weight = settings.Weight;
                        component.IsEnabled = settings.Enabled;
                        component.FormationSpacing = settings.FormationSpacing;
                        component.MovementSpeedMultiplier = settings.MovementSpeedMultiplier;
                        component.MaxRowsInFormation = settings.MaxRowsInFormation;
                    }
                    else
                    {
                        entity.AddComponent(new PhalanxComponent(
                            settings.Weight, settings.FormationSpacing, 
                            settings.MovementSpeedMultiplier, settings.MaxRowsInFormation));
                    }
                    break;
                    
                case SteeringBehaviorConfig.FormationType.Testudo:
                    if (entity.HasComponent<TestudoComponent>())
                    {
                        var component = entity.GetComponent<TestudoComponent>();
                        component.Weight = settings.Weight;
                        component.IsEnabled = settings.Enabled;
                        component.FormationSpacing = settings.FormationSpacing;
                        component.MovementSpeedMultiplier = settings.MovementSpeedMultiplier;
                        component.KnockbackResistanceBonus = settings.KnockbackResistanceBonus;
                        component.RangedDefenseBonus = settings.RangedDefenseBonus;
                    }
                    else
                    {
                        entity.AddComponent(new TestudoComponent(
                            settings.Weight, settings.FormationSpacing, 
                            settings.MovementSpeedMultiplier, settings.KnockbackResistanceBonus,
                            settings.RangedDefenseBonus));
                    }
                    break;
            }
        }
        
        private static void ApplyProtectBehavior(Entity entity, SteeringBehaviorConfig.ProtectSettings settings)
        {
            if (!settings.Enabled) return;
            
            if (entity.HasComponent<ProtectComponent>())
            {
                var component = entity.GetComponent<ProtectComponent>();
                component.Weight = settings.Weight;
                component.IsEnabled = settings.Enabled;
                component.ProtectRadius = settings.ProtectRadius;
                component.PositioningSpeed = settings.PositioningSpeed;
                component.ProtectedTags = settings.ProtectedTags;
            }
            else
            {
                entity.AddComponent(new ProtectComponent(
                    settings.Weight, settings.ProtectRadius, settings.PositioningSpeed));
            }
        }
        
        private static void ApplyCoverBehavior(Entity entity, SteeringBehaviorConfig.CoverSettings settings)
        {
            if (!settings.Enabled) return;
            
            if (entity.HasComponent<CoverComponent>())
            {
                var component = entity.GetComponent<CoverComponent>();
                component.Weight = settings.Weight;
                component.IsEnabled = settings.Enabled;
                component.CoverDistance = settings.CoverDistance;
                component.PositioningSpeed = settings.PositioningSpeed;
            }
            else
            {
                entity.AddComponent(new CoverComponent(
                    settings.Weight, settings.CoverDistance, settings.PositioningSpeed));
            }
        }
        
        private static void ApplySurroundBehavior(Entity entity, SteeringBehaviorConfig.SurroundSettings settings)
        {
            if (!settings.Enabled) return;
            
            if (entity.HasComponent<SurroundComponent>())
            {
                var component = entity.GetComponent<SurroundComponent>();
                component.Weight = settings.Weight;
                component.IsEnabled = settings.Enabled;
                component.SurroundRadius = settings.SurroundRadius;
                component.SurroundSpeed = settings.SurroundSpeed;
            }
            else
            {
                entity.AddComponent(new SurroundComponent(
                    settings.Weight, settings.SurroundRadius, settings.SurroundSpeed));
            }
        }
    }
}