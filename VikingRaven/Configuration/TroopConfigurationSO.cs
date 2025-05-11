using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using VikingRaven.Units.Components;

namespace VikingRaven.Configuration
{
    [CreateAssetMenu(fileName = "TroopConfiguration", menuName = "VikingRaven/Configurations/Troop Configuration")]
    public class TroopConfigurationSO : SerializedScriptableObject
    {
        [Title("Troop Configuration Settings")]
        [InfoBox("This Scriptable Object stores configuration for all troop types in the game.")]
        
        [BoxGroup("Infantry Configuration")]
        [InlineProperty, HideLabel]
        public TroopTypeConfig InfantryConfig = new TroopTypeConfig(UnitType.Infantry);
        
        [BoxGroup("Archer Configuration")]
        [InlineProperty, HideLabel]
        public TroopTypeConfig ArcherConfig = new TroopTypeConfig(UnitType.Archer);
        
        [BoxGroup("Pike Configuration")]
        [InlineProperty, HideLabel]
        public TroopTypeConfig PikeConfig = new TroopTypeConfig(UnitType.Pike);

        // Get config by unit type
        public TroopTypeConfig GetConfigForType(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Infantry:
                    return InfantryConfig;
                case UnitType.Archer:
                    return ArcherConfig;
                case UnitType.Pike:
                    return PikeConfig;
                default:
                    Debug.LogError($"Unknown unit type: {unitType}");
                    return InfantryConfig; // Default
            }
        }

        [Button("Reset to Defaults"), GUIColor(1, 0.6f, 0.4f)]
        public void ResetToDefaults()
        {
            ResetInfantryToDefaults();
            ResetArcherToDefaults();
            ResetPikeToDefaults();
        }

        [FoldoutGroup("Reset Individual Types")]
        [Button("Reset Infantry Config"), GUIColor(0.7f, 0.7f, 1)]
        public void ResetInfantryToDefaults()
        {
            InfantryConfig = new TroopTypeConfig(UnitType.Infantry);
            
            // Set Infantry defaults
            InfantryConfig.MaxHealth = 100;
            InfantryConfig.MoveSpeed = 3.5f;
            InfantryConfig.RotationSpeed = 5f;
            InfantryConfig.AttackDamage = 20;
            InfantryConfig.AttackRange = 1.5f;
            InfantryConfig.AttackCooldown = 1.5f;
            InfantryConfig.AggroRange = 10f;
            
            // Set behavior weights
            InfantryConfig.BehaviorWeights.Clear();
            InfantryConfig.BehaviorWeights.Add("Move", 2.0f);
            InfantryConfig.BehaviorWeights.Add("Attack", 3.0f);
            InfantryConfig.BehaviorWeights.Add("Strafe", 1.5f);
            InfantryConfig.BehaviorWeights.Add("Protect", 2.5f);
            InfantryConfig.BehaviorWeights.Add("Charge", 2.0f);
            
            // Set formation preferences
            InfantryConfig.FormationPreferences.Clear();
            InfantryConfig.FormationPreferences.Add(FormationType.Line, 1.0f);
            InfantryConfig.FormationPreferences.Add(FormationType.Testudo, 0.8f);
            InfantryConfig.FormationPreferences.Add(FormationType.Phalanx, 0.5f);
            InfantryConfig.FormationPreferences.Add(FormationType.Circle, 0.7f);
        }
        
        [FoldoutGroup("Reset Individual Types")]
        [Button("Reset Archer Config"), GUIColor(0.7f, 1, 0.7f)]
        public void ResetArcherToDefaults()
        {
            ArcherConfig = new TroopTypeConfig(UnitType.Archer);
            
            // Set Archer defaults
            ArcherConfig.MaxHealth = 70;
            ArcherConfig.MoveSpeed = 3.7f;
            ArcherConfig.RotationSpeed = 6f;
            ArcherConfig.AttackDamage = 15;
            ArcherConfig.AttackRange = 12f;
            ArcherConfig.AttackCooldown = 2.0f;
            ArcherConfig.AggroRange = 15f;
            
            // Set behavior weights
            ArcherConfig.BehaviorWeights.Clear();
            ArcherConfig.BehaviorWeights.Add("Move", 2.0f);
            ArcherConfig.BehaviorWeights.Add("Attack", 3.5f);
            ArcherConfig.BehaviorWeights.Add("IdleAttack", 3.0f);
            ArcherConfig.BehaviorWeights.Add("Strafe", 2.5f);
            ArcherConfig.BehaviorWeights.Add("Cover", 2.5f);
            ArcherConfig.BehaviorWeights.Add("AmbushMove", 1.5f);
            
            // Set formation preferences
            ArcherConfig.FormationPreferences.Clear();
            ArcherConfig.FormationPreferences.Add(FormationType.Line, 0.7f);
            ArcherConfig.FormationPreferences.Add(FormationType.Column, 0.9f);
            ArcherConfig.FormationPreferences.Add(FormationType.Circle, 0.5f);
        }
        
        [FoldoutGroup("Reset Individual Types")]
        [Button("Reset Pike Config"), GUIColor(1, 0.7f, 0.7f)]
        public void ResetPikeToDefaults()
        {
            PikeConfig = new TroopTypeConfig(UnitType.Pike);
            
            // Set Pike defaults
            PikeConfig.MaxHealth = 85;
            PikeConfig.MoveSpeed = 3.0f;
            PikeConfig.RotationSpeed = 4f;
            PikeConfig.AttackDamage = 25;
            PikeConfig.AttackRange = 2.5f;
            PikeConfig.AttackCooldown = 2.0f;
            PikeConfig.AggroRange = 8f;
            
            // Set behavior weights
            PikeConfig.BehaviorWeights.Clear();
            PikeConfig.BehaviorWeights.Add("Move", 2.0f);
            PikeConfig.BehaviorWeights.Add("Attack", 2.8f);
            PikeConfig.BehaviorWeights.Add("Strafe", 1.0f);
            PikeConfig.BehaviorWeights.Add("Phalanx", 3.0f);
            
            // Set formation preferences
            PikeConfig.FormationPreferences.Clear();
            PikeConfig.FormationPreferences.Add(FormationType.Line, 0.6f);
            PikeConfig.FormationPreferences.Add(FormationType.Phalanx, 1.0f);
            PikeConfig.FormationPreferences.Add(FormationType.Column, 0.6f);
        }

        [Serializable]
        public class TroopTypeConfig
        {
            [ReadOnly, HideInInspector]
            public UnitType UnitType;
            
            [Title("Combat Stats")]
            [HorizontalGroup("Combat")]
            [VerticalGroup("Combat/Left")]
            [LabelWidth(100)]
            [MinValue(1)]
            public float MaxHealth = 100;
            
            [VerticalGroup("Combat/Left")]
            [LabelWidth(100)]
            [MinValue(0.1f)]
            public float MoveSpeed = 3.5f;
            
            [VerticalGroup("Combat/Left")]
            [LabelWidth(100)]
            [MinValue(0.1f)]
            public float RotationSpeed = 5.0f;
            
            [VerticalGroup("Combat/Right")]
            [LabelWidth(100)]
            [MinValue(1)]
            public float AttackDamage = 20;
            
            [VerticalGroup("Combat/Right")]
            [LabelWidth(100)]
            [MinValue(0.1f)]
            public float AttackRange = 2.0f;
            
            [VerticalGroup("Combat/Right")]
            [LabelWidth(100)]
            [MinValue(0.1f)]
            public float AttackCooldown = 1.5f;
            
            [VerticalGroup("Combat/Left")]
            [LabelWidth(100)]
            [MinValue(1)]
            public float AggroRange = 10.0f;
            
            [Title("Behavior Weights")]
            [InfoBox("Adjust weights to control how likely the AI is to choose each behavior")]
            [TableList(ShowIndexLabels = false)]
            [OdinSerialize]
            public Dictionary<string, float> BehaviorWeights = new Dictionary<string, float>();
            
            [Title("Formation Preferences")]
            [InfoBox("Higher values mean the unit prefers this formation type")]
            [TableList(ShowIndexLabels = false)]
            [OdinSerialize]
            public Dictionary<FormationType, float> FormationPreferences = new Dictionary<FormationType, float>();
            
            [Title("Additional Parameters")]
            [TableList(ShowIndexLabels = false)]
            [OdinSerialize]
            public Dictionary<string, string> CustomParameters = new Dictionary<string, string>();
            
            // Constructor
            public TroopTypeConfig(UnitType unitType)
            {
                UnitType = unitType;
            }
        }
    }
}