using UnityEngine;
using System.Collections.Generic;

namespace Steering.Config
{
    /// <summary>
    /// ScriptableObject for configuring steering behaviors
    /// </summary>
    [CreateAssetMenu(fileName = "SteeringBehaviorConfig", menuName = "Wiking Raven/Steering Behavior Config")]
    public class SteeringBehaviorConfig : ScriptableObject
    {
        // Basic behaviors
        public BehaviorSettings SeekSettings = new BehaviorSettings(true, 1.0f);
        public BehaviorSettings FleeSettings = new BehaviorSettings(false, 1.5f, 5.0f);
        public BehaviorSettings ArrivalSettings = new BehaviorSettings(true, 1.0f, 3.0f);
        public BehaviorSettings SeparationSettings = new BehaviorSettings(true, 2.0f, 2.0f);
        public BehaviorSettings CohesionSettings = new BehaviorSettings(false, 1.0f, 5.0f);
        public BehaviorSettings AlignmentSettings = new BehaviorSettings(false, 1.0f, 5.0f);
        public BehaviorSettings ObstacleAvoidanceSettings = new BehaviorSettings(true, 2.0f, 3.0f, 5.0f);
        public BehaviorSettings PathFollowingSettings = new BehaviorSettings(false, 1.0f, 1.0f, 0.5f);
        
        // Advanced behaviors
        public JumpAttackSettings JumpAttackSettingsBehavior = new JumpAttackSettings();
        public AmbushMoveSettings AmbushMoveSettingsBehavior = new AmbushMoveSettings();
        public ChargeSettings ChargeSettingsBehavior = new ChargeSettings();
        public FormationSettings PhalanxSettingsBehavior = new FormationSettings(FormationType.Phalanx);
        public FormationSettings TestudoSettingsBehavior = new FormationSettings(FormationType.Testudo);
        public ProtectSettings ProtectSettingsBehavior = new ProtectSettings();
        public CoverSettings CoverSettingsBehavior = new CoverSettings();
        public SurroundSettings SurroundSettingsBehavior = new SurroundSettings();
        
        /// <summary>
        /// Base class for behavior settings
        /// </summary>
        [System.Serializable]
        public class BehaviorSettings
        {
            public bool Enabled = true;
            public float Weight = 1.0f;
            public float Parameter1 = 0; // Generic parameter that can be used for different purposes
            public float Parameter2 = 0; // Generic parameter that can be used for different purposes
            
            public BehaviorSettings(bool enabled, float weight)
            {
                Enabled = enabled;
                Weight = weight;
            }
            
            public BehaviorSettings(bool enabled, float weight, float param1)
            {
                Enabled = enabled;
                Weight = weight;
                Parameter1 = param1;
            }
            
            public BehaviorSettings(bool enabled, float weight, float param1, float param2)
            {
                Enabled = enabled;
                Weight = weight;
                Parameter1 = param1;
                Parameter2 = param2;
            }
        }
        
        /// <summary>
        /// Settings for jump attack behavior
        /// </summary>
        [System.Serializable]
        public class JumpAttackSettings : BehaviorSettings
        {
            public float JumpRange = 6.0f;
            public float JumpSpeed = 10.0f;
            public float JumpHeight = 2.0f;
            public float DamageMultiplier = 1.5f;
            public float Cooldown = 5.0f;
            
            public JumpAttackSettings() : base(false, 2.0f) { }
        }
        
        /// <summary>
        /// Settings for ambush move behavior
        /// </summary>
        [System.Serializable]
        public class AmbushMoveSettings : BehaviorSettings
        {
            public float MoveSpeedMultiplier = 0.5f;
            public float DetectionRadiusMultiplier = 0.5f;
            
            public AmbushMoveSettings() : base(false, 1.0f) { }
        }
        
        /// <summary>
        /// Settings for charge behavior
        /// </summary>
        [System.Serializable]
        public class ChargeSettings : BehaviorSettings
        {
            public float ChargeDistance = 10.0f;
            public float ChargeSpeedMultiplier = 2.0f;
            public float ChargeDamageMultiplier = 2.0f;
            public float ChargePreparationTime = 1.0f;
            public float ChargeCooldown = 8.0f;
            
            public ChargeSettings() : base(false, 3.0f) { }
        }
        
        /// <summary>
        /// Formation type enum
        /// </summary>
        public enum FormationType
        {
            Phalanx,
            Testudo
        }
        
        /// <summary>
        /// Settings for formation behaviors
        /// </summary>
        [System.Serializable]
        public class FormationSettings : BehaviorSettings
        {
            public FormationType FormationType;
            public float FormationSpacing = 1.5f;
            public float MovementSpeedMultiplier = 0.7f;
            public int MaxRowsInFormation = 3;
            
            // For Testudo only
            public float KnockbackResistanceBonus = 0.8f;
            public float RangedDefenseBonus = 0.7f;
            
            public FormationSettings(FormationType type) : base(false, 2.0f)
            {
                FormationType = type;
                
                // Different defaults based on formation type
                if (type == FormationType.Testudo)
                {
                    FormationSpacing = 1.0f;
                    MovementSpeedMultiplier = 0.5f;
                    Weight = 3.0f;
                }
            }
        }
        
        /// <summary>
        /// Settings for protect behavior
        /// </summary>
        [System.Serializable]
        public class ProtectSettings : BehaviorSettings
        {
            public float ProtectRadius = 3.0f;
            public float PositioningSpeed = 5.0f;
            public string[] ProtectedTags = new string[] { "Player" };
            
            public ProtectSettings() : base(false, 2.0f) { }
        }
        
        /// <summary>
        /// Settings for cover behavior
        /// </summary>
        [System.Serializable]
        public class CoverSettings : BehaviorSettings
        {
            public float CoverDistance = 2.0f;
            public float PositioningSpeed = 4.0f;
            
            public CoverSettings() : base(false, 1.0f) { }
        }
        
        /// <summary>
        /// Settings for surround behavior
        /// </summary>
        [System.Serializable]
        public class SurroundSettings : BehaviorSettings
        {
            public float SurroundRadius = 5.0f;
            public float SurroundSpeed = 3.0f;
            
            public SurroundSettings() : base(false, 1.0f) { }
        }
    }
}