using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using VikingRaven.Configuration;
using VikingRaven.Core.ECS;

namespace Editor
{
    public class SystemConfigurationWindow : OdinEditorWindow
    {
        [MenuItem("VikingRaven/Configuration/System Configuration")]
        private static void OpenWindow()
        {
            GetWindow<SystemConfigurationWindow>().Show();
        }

        [PropertySpace(10)]
        [Title("System Configuration Window", "Configure game systems' parameters and save/load configurations")]
        [HideLabel]
        [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
        public SystemConfigurationSO ConfigData;

        [PropertySpace(10)]
        [Title("Configuration Mode")]
        [EnumToggleButtons]
        public ConfigurationMode Mode = ConfigurationMode.Manual;

        [ShowIf("Mode", ConfigurationMode.Auto)]
        [PropertySpace(10)]
        [BoxGroup("Auto Configuration Settings")]
        [LabelText("Balance Preset")]
        [EnumToggleButtons]
        public BalancePreset Preset = BalancePreset.Balanced;

        [ShowIf("Mode", ConfigurationMode.Auto)]
        [BoxGroup("Auto Configuration Settings")]
        [LabelText("Game Speed")]
        [Range(0.5f, 2f)]
        public float GameSpeed = 1f;

        [ShowIf("Mode", ConfigurationMode.Auto)]
        [BoxGroup("Auto Configuration Settings")]
        [LabelText("AI Aggressiveness")]
        [Range(0, 1f)]
        public float AIAggressiveness = 0.5f;

        [ShowIf("Mode", ConfigurationMode.Auto)]
        [BoxGroup("Auto Configuration Settings")]
        [Button("Apply Auto Configuration"), GUIColor(0.4f, 0.8f, 1f)]
        public void ApplyAutoConfiguration()
        {
            if (ConfigData == null)
            {
                EditorUtility.DisplayDialog("Error", "Please create or load a configuration first.", "OK");
                return;
            }

            // Adjust configurations based on presets and sliders
            foreach (var config in ConfigData.SystemConfigurations)
            {
                // Adjust update intervals based on game speed
                config.UpdateInterval = Mathf.Clamp(config.UpdateInterval / GameSpeed, 0.01f, 5f);
                
                // Apply preset-specific configurations
                switch (Preset)
                {
                    case BalancePreset.PerformanceOptimized:
                        // Increase update intervals to reduce CPU usage
                        config.UpdateInterval *= 1.5f;
                        break;
                        
                    case BalancePreset.CombatFocused:
                        // Prioritize combat systems
                        if (config.SystemType.Contains("Combat") || config.SystemType.Contains("Aggro"))
                        {
                            config.Priority = Mathf.Max(0, config.Priority - 20);
                            config.UpdateInterval *= 0.7f;
                        }
                        break;
                        
                    case BalancePreset.TacticalFocused:
                        // Prioritize tactical systems
                        if (config.SystemType.Contains("Tactical") || config.SystemType.Contains("Formation"))
                        {
                            config.Priority = Mathf.Max(0, config.Priority - 20);
                            config.UpdateInterval *= 0.7f;
                        }
                        break;
                }
                
                // Adjust AI aggressiveness settings
                if (config.SystemType == "AIDecisionSystem")
                {
                    config.CustomParameters["AggressivenessMultiplier"] = AIAggressiveness.ToString("F2");
                }
                else if (config.SystemType == "AggroDetectionSystem")
                {
                    float baseAggroRange = 10f;
                    float aggroRangeMultiplier = 0.7f + (AIAggressiveness * 0.6f); // 0.7 to 1.3 based on aggressiveness
                    config.CustomParameters["AggroRangeMultiplier"] = aggroRangeMultiplier.ToString("F2");
                    config.CustomParameters["BaseAggroRange"] = (baseAggroRange * aggroRangeMultiplier).ToString("F2");
                }
            }
            
            EditorUtility.SetDirty(ConfigData);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", "Auto configuration applied!", "OK");
        }

        [PropertySpace(10)]
        [HorizontalGroup("ConfigButtons")]
        [Button("New Configuration"), GUIColor(0.4f, 0.8f, 0.4f)]
        public void CreateNewConfiguration()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New System Configuration", 
                "SystemConfiguration", 
                "asset", 
                "Please enter a file name for the new system configuration asset."
            );
            
            if (string.IsNullOrEmpty(path))
                return;
                
            ConfigData = CreateInstance<SystemConfigurationSO>();
            ConfigData.ResetToDefaults();
            
            AssetDatabase.CreateAsset(ConfigData, path);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(ConfigData);
        }

        [HorizontalGroup("ConfigButtons")]
        [Button("Load Configuration"), GUIColor(0.8f, 0.8f, 0.4f)]
        public void LoadConfiguration()
        {
            string path = EditorUtility.OpenFilePanel("Load System Configuration", "Assets", "asset");
            if (string.IsNullOrEmpty(path))
                return;
                
            // Convert absolute path to relative asset path
            path = "Assets" + path.Substring(Application.dataPath.Length);
            ConfigData = AssetDatabase.LoadAssetAtPath<SystemConfigurationSO>(path);
            
            if (ConfigData == null)
            {
                EditorUtility.DisplayDialog("Error", "Failed to load configuration. Make sure it's a valid SystemConfigurationSO asset.", "OK");
            }
        }

        [PropertySpace(10)]
        [HorizontalGroup("CSV")]
        [Button("Export to CSV"), GUIColor(0.4f, 0.8f, 0.8f)]
        public void ExportToCSV()
        {
            if (ConfigData == null || ConfigData.SystemConfigurations.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No configuration data to export.", "OK");
                return;
            }
            
            string path = EditorUtility.SaveFilePanel("Export Configuration to CSV", "", "SystemConfig", "csv");
            if (string.IsNullOrEmpty(path))
                return;
                
            using (StreamWriter writer = new StreamWriter(path))
            {
                // Write header
                writer.WriteLine("SystemType,IsActive,Priority,UpdateInterval,CustomParameters");
                
                // Write data for each system
                foreach (var config in ConfigData.SystemConfigurations)
                {
                    string customParams = string.Join("|", config.CustomParameters
                        .Select(kv => $"{kv.Key}:{kv.Value}"));
                        
                    writer.WriteLine($"{config.SystemType},{config.IsActive},{config.Priority},{config.UpdateInterval},{customParams}");
                }
            }
            
            EditorUtility.DisplayDialog("Success", "Configuration exported to CSV successfully!", "OK");
        }

        [HorizontalGroup("CSV")]
        [Button("Import from CSV"), GUIColor(0.8f, 0.4f, 0.8f)]
        public void ImportFromCSV()
        {
            if (ConfigData == null)
            {
                EditorUtility.DisplayDialog("Error", "Please create or load a configuration first.", "OK");
                return;
            }
            
            string path = EditorUtility.OpenFilePanel("Import Configuration from CSV", "", "csv");
            if (string.IsNullOrEmpty(path))
                return;
                
            try
            {
                ConfigData.SystemConfigurations.Clear();
                
                using (StreamReader reader = new StreamReader(path))
                {
                    // Skip header
                    reader.ReadLine();
                    
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(',');
                        
                        if (values.Length < 4)
                            continue;
                            
                        SystemConfigurationSO.SystemConfig config = new SystemConfigurationSO.SystemConfig
                        {
                            SystemType = values[0],
                            IsActive = bool.Parse(values[1]),
                            Priority = int.Parse(values[2]),
                            UpdateInterval = float.Parse(values[3])
                        };
                        
                        // Parse custom parameters if present
                        if (values.Length > 4 && !string.IsNullOrEmpty(values[4]))
                        {
                            string[] paramPairs = values[4].Split('|');
                            foreach (string pair in paramPairs)
                            {
                                string[] keyValue = pair.Split(':');
                                if (keyValue.Length == 2)
                                {
                                    config.CustomParameters[keyValue[0]] = keyValue[1];
                                }
                            }
                        }
                        
                        ConfigData.SystemConfigurations.Add(config);
                    }
                }
                
                EditorUtility.SetDirty(ConfigData);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Success", "Configuration imported from CSV successfully!", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to import CSV: {ex.Message}", "OK");
            }
        }

        [PropertySpace(10)]
        [Button("Apply to Scene Systems"), GUIColor(0.8f, 0.4f, 0.4f)]
        public void ApplyToSceneSystems()
        {
            if (ConfigData == null)
            {
                EditorUtility.DisplayDialog("Error", "Please create or load a configuration first.", "OK");
                return;
            }
            
            BaseSystem[] sceneSystems = FindObjectsOfType<BaseSystem>();
            int appliedCount = 0;
            
            foreach (var system in sceneSystems)
            {
                string systemType = system.GetType().Name;
                var config = ConfigData.GetSystemConfig(systemType);
                
                if (config != null)
                {
                    system.IsActive = config.IsActive;
                    system.Priority = config.Priority;
                    
                    // Find and set any matching public fields from custom parameters
                    foreach (var param in config.CustomParameters)
                    {
                        FieldInfo field = system.GetType().GetField(param.Key, 
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                        if (field != null)
                        {
                            try
                            {
                                if (field.FieldType == typeof(float))
                                    field.SetValue(system, float.Parse(param.Value));
                                else if (field.FieldType == typeof(int))
                                    field.SetValue(system, int.Parse(param.Value));
                                else if (field.FieldType == typeof(bool))
                                    field.SetValue(system, bool.Parse(param.Value));
                                else if (field.FieldType == typeof(string))
                                    field.SetValue(system, param.Value);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"Failed to set field {param.Key} on {systemType}: {ex.Message}");
                            }
                        }
                    }
                    
                    EditorUtility.SetDirty(system);
                    appliedCount++;
                }
            }
            
            if (appliedCount > 0)
            {
                EditorUtility.DisplayDialog("Success", $"Applied configuration to {appliedCount} scene systems!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Warning", "No matching systems found in the current scene.", "OK");
            }
        }

        [PropertySpace(10)]
        [Button("Extract from Scene Systems"), GUIColor(0.4f, 0.4f, 0.8f)]
        public void ExtractFromSceneSystems()
        {
            if (ConfigData == null)
            {
                EditorUtility.DisplayDialog("Error", "Please create or load a configuration first.", "OK");
                return;
            }
            
            BaseSystem[] sceneSystems = FindObjectsOfType<BaseSystem>();
            
            if (sceneSystems.Length == 0)
            {
                EditorUtility.DisplayDialog("Warning", "No systems found in the current scene.", "OK");
                return;
            }
            
            ConfigData.SystemConfigurations.Clear();
            
            foreach (var system in sceneSystems)
            {
                string systemType = system.GetType().Name;
                var config = new SystemConfigurationSO.SystemConfig(systemType, system.Priority, system.IsActive);
                
                // Extract custom parameters by reflection
                FieldInfo[] fields = system.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    // Skip fields that are not serializable or are part of the base class
                    if (field.IsLiteral || field.IsInitOnly || field.DeclaringType == typeof(BaseSystem))
                        continue;
                        
                    if (field.FieldType == typeof(float) || field.FieldType == typeof(int) || 
                        field.FieldType == typeof(bool) || field.FieldType == typeof(string))
                    {
                        var value = field.GetValue(system);
                        if (value != null)
                        {
                            config.CustomParameters[field.Name] = value.ToString();
                        }
                    }
                }
                
                ConfigData.SystemConfigurations.Add(config);
            }
            
            EditorUtility.SetDirty(ConfigData);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", $"Extracted configuration from {sceneSystems.Length} scene systems!", "OK");
        }

        // Enum definitions
        public enum ConfigurationMode
        {
            Manual,
            Auto
        }

        public enum BalancePreset
        {
            Balanced,
            PerformanceOptimized,
            CombatFocused,
            TacticalFocused
        }
    }
}