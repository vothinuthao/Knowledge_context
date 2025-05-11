using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace VikingRaven.Configuration
{
    [CreateAssetMenu(fileName = "SystemConfiguration", menuName = "VikingRaven/Configurations/System Configuration")]
    public class SystemConfigurationSO : SerializedScriptableObject
    {
        [Title("System Configuration Settings")]
        [InfoBox("This Scriptable Object stores configuration for all systems in the game.")]
        
        [TableList, ShowInInspector]
        [OdinSerialize]
        [LabelText("Systems Configuration")] 
        public List<SystemConfig> SystemConfigurations = new List<SystemConfig>();

        [Serializable]
        public class SystemConfig
        {
            [HorizontalGroup("Basic", Width = 0.3f)]
            [VerticalGroup("Basic/Left")]
            [LabelText("System Type"), LabelWidth(80)]
            [ReadOnly]
            public string SystemType;

            [VerticalGroup("Basic/Left")]
            [LabelText("Is Active"), LabelWidth(80), ToggleLeft]
            public bool IsActive = true;

            [VerticalGroup("Basic/Right")]
            [LabelText("Priority"), LabelWidth(80), Range(0, 100)]
            [Tooltip("Execution priority (lower value = executed earlier)")]
            public int Priority;

            [FoldoutGroup("Advanced Settings")]
            [Range(0.01f, 5f)]
            [LabelText("Update Interval (s)"), LabelWidth(120)]
            [Tooltip("How frequently the system updates (in seconds)")]
            public float UpdateInterval = 0.1f;

            [FoldoutGroup("Advanced Settings")]
            [LabelText("Custom Parameters"), LabelWidth(120)]
            public Dictionary<string, string> CustomParameters = new Dictionary<string, string>();
            
            // Default constructor for serialization
            public SystemConfig() { }
            
            // Constructor for creating from existing system type
            public SystemConfig(string systemType, int priority = 0, bool isActive = true)
            {
                SystemType = systemType;
                Priority = priority;
                IsActive = isActive;
                UpdateInterval = 0.1f;
            }
        }

        // Method to find a system configuration by type
        public SystemConfig GetSystemConfig(string systemType)
        {
            return SystemConfigurations.Find(config => config.SystemType == systemType);
        }

        // Method to add a new system configuration
        public void AddSystemConfig(string systemType, int priority = 0, bool isActive = true)
        {
            if (GetSystemConfig(systemType) == null)
            {
                SystemConfigurations.Add(new SystemConfig(systemType, priority, isActive));
            }
        }

        // Method to reset all configurations to default values
        [Button("Reset to Defaults"), GUIColor(1, 0.6f, 0.4f)]
        public void ResetToDefaults()
        {
            SystemConfigurations.Clear();
            
            // Add default configurations for all known system types
            AddSystemConfig("MovementSystem", 10, true);
            AddSystemConfig("AIDecisionSystem", 20, true);
            AddSystemConfig("FormationSystem", 30, true);
            AddSystemConfig("AggroDetectionSystem", 40, true);
            AddSystemConfig("SquadCoordinationSystem", 50, true);
            AddSystemConfig("SteeringSystem", 60, true);
            AddSystemConfig("TacticalAnalysisSystem", 70, true);
            AddSystemConfig("WeightedBehaviorSystem", 80, true);
            
            // Set default custom parameters as needed
            GetSystemConfig("AIDecisionSystem").CustomParameters["DecisionUpdateInterval"] = "0.5";
            GetSystemConfig("FormationSystem").CustomParameters["FormationTightness"] = "0.8";
            GetSystemConfig("TacticalAnalysisSystem").CustomParameters["CombatEvaluationInterval"] = "2.0";
        }
    }
}