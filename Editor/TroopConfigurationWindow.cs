using System;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using VikingRaven.Configuration;
using VikingRaven.Units.Components;

namespace Editor
{
    public class TroopConfigurationWindow : OdinEditorWindow
    {
        [MenuItem("VikingRaven/Configuration/Troop Configuration")]
        private static void OpenWindow()
        {
            GetWindow<TroopConfigurationWindow>().Show();
        }

        [PropertySpace(10)]
        [Title("Troop Configuration Window", "Configure troop unit parameters and save/load configurations")]
        [HideLabel]
        [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
        public TroopConfigurationSO ConfigData;
        
        [PropertySpace(10)]
        [Title("Configuration Mode")]
        [EnumToggleButtons]
        public ConfigurationMode Mode = ConfigurationMode.Manual;
        
        [ShowIf("Mode", ConfigurationMode.Auto)]
        [PropertySpace(10)]
        [BoxGroup("Auto Configuration Settings")]
        [LabelText("Game Balance")]
        [EnumToggleButtons]
        public GameBalanceType BalanceType = GameBalanceType.Balanced;
        
        [ShowIf("Mode", ConfigurationMode.Auto)]
        [BoxGroup("Auto Configuration Settings")]
        [LabelText("Gameplay Speed")]
        [Range(0.5f, 2f)]
        public float GameplaySpeed = 1f;

        [ShowIf("Mode", ConfigurationMode.Auto)]
        [BoxGroup("Auto Configuration Settings")]
        [LabelText("Overall Combat Difficulty")]
        [Range(0.5f, 1.5f)]
        public float CombatDifficulty = 1f;

        [PropertySpace(10)]
        [TabGroup("UnitTypes")]
        [Button("Infantry", ButtonSizes.Large), GUIColor(0.7f, 0.7f, 1.0f)]
        private void SelectInfantry() => _selectedUnitType = UnitType.Infantry;

        [TabGroup("UnitTypes")]
        [Button("Archer", ButtonSizes.Large), GUIColor(0.7f, 1.0f, 0.7f)]
        private void SelectArcher() => _selectedUnitType = UnitType.Archer;

        [TabGroup("UnitTypes")]
        [Button("Pike", ButtonSizes.Large), GUIColor(1.0f, 0.7f, 0.7f)]
        private void SelectPike() => _selectedUnitType = UnitType.Pike;

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

            // Apply different balance settings based on the selected balance type
            switch (BalanceType)
            {
                case GameBalanceType.Balanced:
                    ApplyBalancedConfiguration();
                    break;
                case GameBalanceType.OffensiveFocused:
                    ApplyOffensiveConfiguration();
                    break;
                case GameBalanceType.DefensiveFocused:
                    ApplyDefensiveConfiguration();
                    break;
                case GameBalanceType.MobilityCentered:
                    ApplyMobilityConfiguration();
                    break;
            }
            
            // Adjust for gameplay speed
            AdjustForGameplaySpeed();
            
            // Apply combat difficulty multipliers
            AdjustForCombatDifficulty();
            
            EditorUtility.SetDirty(ConfigData);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", "Auto configuration applied!", "OK");
        }

        private void ApplyBalancedConfiguration()
        {
            // Reset to defaults first
            ConfigData.ResetToDefaults();
            
            // Balanced configuration is already set by defaults
        }

        private void ApplyOffensiveConfiguration()
        {
            // Reset to defaults first
            ConfigData.ResetToDefaults();
            
            // Increase attack stats, decrease defense
            AdjustAllUnitTypes(
                healthMult: 0.9f,
                damageMult: 1.3f,
                attackRangeMult: 1.1f,
                attackCooldownMult: 0.9f,  // Faster attacks
                aggroRangeMult: 1.2f       // More aggressive
            );
            
            // Adjust behavior weights for more aggressive play
            ConfigData.InfantryConfig.BehaviorWeights["Attack"] *= 1.3f;
            ConfigData.InfantryConfig.BehaviorWeights["Charge"] *= 1.4f;
            if (ConfigData.InfantryConfig.BehaviorWeights.ContainsKey("Protect"))
                ConfigData.InfantryConfig.BehaviorWeights["Protect"] *= 0.7f;
            
            ConfigData.ArcherConfig.BehaviorWeights["Attack"] *= 1.3f;
            ConfigData.ArcherConfig.BehaviorWeights["IdleAttack"] *= 1.2f;
            if (ConfigData.ArcherConfig.BehaviorWeights.ContainsKey("Cover"))
                ConfigData.ArcherConfig.BehaviorWeights["Cover"] *= 0.8f;
            
            ConfigData.PikeConfig.BehaviorWeights["Attack"] *= 1.2f;
            if (ConfigData.PikeConfig.BehaviorWeights.ContainsKey("Phalanx"))
                ConfigData.PikeConfig.BehaviorWeights["Phalanx"] *= 0.8f;
        }

        private void ApplyDefensiveConfiguration()
        {
            // Reset to defaults first
            ConfigData.ResetToDefaults();
            
            // Increase defense stats, decrease offense
            AdjustAllUnitTypes(
                healthMult: 1.3f,
                damageMult: 0.9f,
                attackRangeMult: 0.9f,
                attackCooldownMult: 1.1f,  // Slower attacks
                aggroRangeMult: 0.9f       // Less aggressive
            );
            
            // Adjust behavior weights for more defensive play
            if (ConfigData.InfantryConfig.BehaviorWeights.ContainsKey("Protect"))
                ConfigData.InfantryConfig.BehaviorWeights["Protect"] *= 1.5f;
            if (ConfigData.InfantryConfig.BehaviorWeights.ContainsKey("Charge"))
                ConfigData.InfantryConfig.BehaviorWeights["Charge"] *= 0.7f;
            
            if (ConfigData.ArcherConfig.BehaviorWeights.ContainsKey("Cover"))
                ConfigData.ArcherConfig.BehaviorWeights["Cover"] *= 1.4f;
            if (ConfigData.ArcherConfig.BehaviorWeights.ContainsKey("Strafe"))
                ConfigData.ArcherConfig.BehaviorWeights["Strafe"] *= 1.3f;
            
            if (ConfigData.PikeConfig.BehaviorWeights.ContainsKey("Phalanx"))
                ConfigData.PikeConfig.BehaviorWeights["Phalanx"] *= 1.5f;
            
            // Adjust formation preferences
            ConfigData.InfantryConfig.FormationPreferences[FormationType.Testudo] = 1.2f;
            ConfigData.PikeConfig.FormationPreferences[FormationType.Phalanx] = 1.3f;
        }

        private void ApplyMobilityConfiguration()
        {
            // Reset to defaults first
            ConfigData.ResetToDefaults();
            
            // Increase mobility stats
            AdjustAllUnitTypes(
                healthMult: 0.9f,
                speedMult: 1.3f,
                rotationSpeedMult: 1.2f,
                attackCooldownMult: 0.9f,  // Faster attacks
                aggroRangeMult: 1.1f       // More aggressive
            );
            
            // Adjust behavior weights for more mobile play
            ConfigData.InfantryConfig.BehaviorWeights["Move"] *= 1.3f;
            ConfigData.InfantryConfig.BehaviorWeights["Strafe"] *= 1.3f;
            
            ConfigData.ArcherConfig.BehaviorWeights["Move"] *= 1.2f;
            ConfigData.ArcherConfig.BehaviorWeights["Strafe"] *= 1.4f;
            
            ConfigData.PikeConfig.BehaviorWeights["Move"] *= 1.2f;
            
            // Adjust formation preferences
            ConfigData.InfantryConfig.FormationPreferences[FormationType.Column] = 1.1f;
            ConfigData.ArcherConfig.FormationPreferences[FormationType.Column] = 1.2f;
        }

        private void AdjustForGameplaySpeed()
        {
            if (GameplaySpeed == 1.0f)
                return; // No need to adjust at normal speed
                
            AdjustAllUnitTypes(
                speedMult: GameplaySpeed,
                rotationSpeedMult: GameplaySpeed,
                attackCooldownMult: 1.0f / GameplaySpeed  // Inverse for cooldown
            );
        }

        private void AdjustForCombatDifficulty()
        {
            if (CombatDifficulty == 1.0f)
                return; // No need to adjust at normal difficulty
                
            // For player units (assuming Infantry is player controlled)
            ConfigData.InfantryConfig.MaxHealth *= CombatDifficulty;
            ConfigData.InfantryConfig.AttackDamage *= CombatDifficulty;
                
            // For AI/enemy units (assuming Archer and Pike are AI controlled)
            float aiDifficultyFactor = 2.0f - CombatDifficulty; // Inverse of player difficulty
            ConfigData.ArcherConfig.MaxHealth *= aiDifficultyFactor;
            ConfigData.ArcherConfig.AttackDamage *= aiDifficultyFactor;
            ConfigData.PikeConfig.MaxHealth *= aiDifficultyFactor;
            ConfigData.PikeConfig.AttackDamage *= aiDifficultyFactor;
        }

        private void AdjustAllUnitTypes(
            float healthMult = 1.0f,
            float speedMult = 1.0f,
            float rotationSpeedMult = 1.0f,
            float damageMult = 1.0f,
            float attackRangeMult = 1.0f,
            float attackCooldownMult = 1.0f,
            float aggroRangeMult = 1.0f)
        {
            // Apply multipliers to Infantry
            ConfigData.InfantryConfig.MaxHealth *= healthMult;
            ConfigData.InfantryConfig.MoveSpeed *= speedMult;
            ConfigData.InfantryConfig.RotationSpeed *= rotationSpeedMult;
            ConfigData.InfantryConfig.AttackDamage *= damageMult;
            ConfigData.InfantryConfig.AttackRange *= attackRangeMult;
            ConfigData.InfantryConfig.AttackCooldown *= attackCooldownMult;
            ConfigData.InfantryConfig.AggroRange *= aggroRangeMult;
            
            // Apply multipliers to Archer
            ConfigData.ArcherConfig.MaxHealth *= healthMult;
            ConfigData.ArcherConfig.MoveSpeed *= speedMult;
            ConfigData.ArcherConfig.RotationSpeed *= rotationSpeedMult;
            ConfigData.ArcherConfig.AttackDamage *= damageMult;
            ConfigData.ArcherConfig.AttackRange *= attackRangeMult;
            ConfigData.ArcherConfig.AttackCooldown *= attackCooldownMult;
            ConfigData.ArcherConfig.AggroRange *= aggroRangeMult;
            
            // Apply multipliers to Pike
            ConfigData.PikeConfig.MaxHealth *= healthMult;
            ConfigData.PikeConfig.MoveSpeed *= speedMult;
            ConfigData.PikeConfig.RotationSpeed *= rotationSpeedMult;
            ConfigData.PikeConfig.AttackDamage *= damageMult;
            ConfigData.PikeConfig.AttackRange *= attackRangeMult;
            ConfigData.PikeConfig.AttackCooldown *= attackCooldownMult;
            ConfigData.PikeConfig.AggroRange *= aggroRangeMult;
        }

        [PropertySpace(10)]
        [HorizontalGroup("ConfigButtons")]
        [Button("New Configuration"), GUIColor(0.4f, 0.8f, 0.4f)]
        public void CreateNewConfiguration()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Troop Configuration", 
                "TroopConfiguration", 
                "asset", 
                "Please enter a file name for the new troop configuration asset."
            );
            
            if (string.IsNullOrEmpty(path))
                return;
                
            ConfigData = CreateInstance<TroopConfigurationSO>();
            ConfigData.ResetToDefaults();
            
            AssetDatabase.CreateAsset(ConfigData, path);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(ConfigData);
        }

        [HorizontalGroup("ConfigButtons")]
        [Button("Load Configuration"), GUIColor(0.8f, 0.8f, 0.4f)]
        public void LoadConfiguration()
        {
            string path = EditorUtility.OpenFilePanel("Load Troop Configuration", "Assets", "asset");
            if (string.IsNullOrEmpty(path))
                return;
                
            // Convert absolute path to relative asset path
            path = "Assets" + path.Substring(Application.dataPath.Length);
            ConfigData = AssetDatabase.LoadAssetAtPath<TroopConfigurationSO>(path);
            
            if (ConfigData == null)
            {
                EditorUtility.DisplayDialog("Error", "Failed to load configuration. Make sure it's a valid TroopConfigurationSO asset.", "OK");
            }
        }

        [PropertySpace(10)]
        [HorizontalGroup("CSV")]
        [Button("Export to CSV"), GUIColor(0.4f, 0.8f, 0.8f)]
        public void ExportToCSV()
        {
            if (ConfigData == null)
            {
                EditorUtility.DisplayDialog("Error", "No configuration data to export.", "OK");
                return;
            }
            
            string path = EditorUtility.SaveFilePanel("Export Configuration to CSV", "", "TroopConfig", "csv");
            if (string.IsNullOrEmpty(path))
                return;
                
            using (StreamWriter writer = new StreamWriter(path))
            {
                // Write header
                writer.WriteLine("UnitType,MaxHealth,MoveSpeed,RotationSpeed,AttackDamage,AttackRange,AttackCooldown,AggroRange,BehaviorWeights,FormationPreferences,CustomParameters");
                
                // Write data for each unit type
                WriteUnitTypeToCSV(writer, ConfigData.InfantryConfig);
                WriteUnitTypeToCSV(writer, ConfigData.ArcherConfig);
                WriteUnitTypeToCSV(writer, ConfigData.PikeConfig);
            }
            
            EditorUtility.DisplayDialog("Success", "Configuration exported to CSV successfully!", "OK");
        }

        private void WriteUnitTypeToCSV(StreamWriter writer, TroopConfigurationSO.TroopTypeConfig config)
        {
            string behaviorWeights = string.Join("|", config.BehaviorWeights
                .Select(kv => $"{kv.Key}:{kv.Value}"));
                
            string formationPrefs = string.Join("|", config.FormationPreferences
                .Select(kv => $"{kv.Key}:{kv.Value}"));
                
            string customParams = string.Join("|", config.CustomParameters
                .Select(kv => $"{kv.Key}:{kv.Value}"));
                
            writer.WriteLine($"{config.UnitType},{config.MaxHealth},{config.MoveSpeed},{config.RotationSpeed}," +
                            $"{config.AttackDamage},{config.AttackRange},{config.AttackCooldown},{config.AggroRange}," +
                            $"{behaviorWeights},{formationPrefs},{customParams}");
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
                using (StreamReader reader = new StreamReader(path))
                {
                    // Skip header
                    reader.ReadLine();
                    
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(',');
                        
                        if (values.Length < 8)
                            continue;
                            
                        UnitType unitType;
                        if (!Enum.TryParse(values[0], out unitType))
                            continue;
                            
                        var config = ConfigData.GetConfigForType(unitType);
                        
                        config.MaxHealth = float.Parse(values[1]);
                        config.MoveSpeed = float.Parse(values[2]);
                        config.RotationSpeed = float.Parse(values[3]);
                        config.AttackDamage = float.Parse(values[4]);
                        config.AttackRange = float.Parse(values[5]);
                        config.AttackCooldown = float.Parse(values[6]);
                        config.AggroRange = float.Parse(values[7]);
                        
                        // Parse behavior weights
                        if (values.Length > 8 && !string.IsNullOrEmpty(values[8]))
                        {
                            config.BehaviorWeights.Clear();
                            string[] weightPairs = values[8].Split('|');
                            foreach (string pair in weightPairs)
                            {
                                string[] keyValue = pair.Split(':');
                                if (keyValue.Length == 2)
                                {
                                    config.BehaviorWeights[keyValue[0]] = float.Parse(keyValue[1]);
                                }
                            }
                        }
                        
                        // Parse formation preferences
                        if (values.Length > 9 && !string.IsNullOrEmpty(values[9]))
                        {
                            config.FormationPreferences.Clear();
                            string[] prefPairs = values[9].Split('|');
                            foreach (string pair in prefPairs)
                            {
                                string[] keyValue = pair.Split(':');
                                if (keyValue.Length == 2)
                                {
                                    FormationType formationType;
                                    if (Enum.TryParse(keyValue[0], out formationType))
                                    {
                                        config.FormationPreferences[formationType] = float.Parse(keyValue[1]);
                                    }
                                }
                            }
                        }
                        
                        // Parse custom parameters
                        if (values.Length > 10 && !string.IsNullOrEmpty(values[10]))
                        {
                            config.CustomParameters.Clear();
                            string[] paramPairs = values[10].Split('|');
                            foreach (string pair in paramPairs)
                            {
                                string[] keyValue = pair.Split(':');
                                if (keyValue.Length == 2)
                                {
                                    config.CustomParameters[keyValue[0]] = keyValue[1];
                                }
                            }
                        }
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
        [Button("Apply to Scene Units"), GUIColor(0.8f, 0.4f, 0.4f)]
        public void ApplyToSceneUnits()
        {
            if (ConfigData == null)
            {
                EditorUtility.DisplayDialog("Error", "Please create or load a configuration first.", "OK");
                return;
            }
            
            UnitTypeComponent[] unitComponents = FindObjectsOfType<UnitTypeComponent>();
            int appliedCount = 0;
            
            foreach (var unitComponent in unitComponents)
            {
                var entity = unitComponent.Entity;
                if (entity == null)
                    continue;
                
                var config = ConfigData.GetConfigForType(unitComponent.UnitType);
                
                // Apply configs to relevant components
                ApplyConfigToEntity(entity, config);
                EditorUtility.SetDirty(unitComponent.gameObject);
                appliedCount++;
            }
            
            if (appliedCount > 0)
            {
                EditorUtility.DisplayDialog("Success", $"Applied configuration to {appliedCount} scene units!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Warning", "No matching units found in the current scene.", "OK");
            }
        }

        private void ApplyConfigToEntity(VikingRaven.Core.ECS.IEntity entity, TroopConfigurationSO.TroopTypeConfig config)
        {
            // Apply to health component
            var healthComponent = entity.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                var field = healthComponent.GetType().GetField("_maxHealth", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                    field.SetValue(healthComponent, config.MaxHealth);
            }
            
            // Apply to combat component
            var combatComponent = entity.GetComponent<CombatComponent>();
            if (combatComponent != null)
            {
                var damageField = combatComponent.GetType().GetField("_attackDamage", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (damageField != null)
                    damageField.SetValue(combatComponent, config.AttackDamage);
                    
                var rangeField = combatComponent.GetType().GetField("_attackRange", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (rangeField != null)
                    rangeField.SetValue(combatComponent, config.AttackRange);
                    
                var cooldownField = combatComponent.GetType().GetField("_attackCooldown", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (cooldownField != null)
                    cooldownField.SetValue(combatComponent, config.AttackCooldown);
            }
            
            // Apply to transform component
            var transformComponent = entity.GetComponent<TransformComponent>();
            if (transformComponent != null)
            {
                var speedField = transformComponent.GetType().GetField("_moveSpeed", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (speedField != null)
                    speedField.SetValue(transformComponent, config.MoveSpeed);
                    
                var rotationField = transformComponent.GetType().GetField("_rotationSpeed", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (rotationField != null)
                    rotationField.SetValue(transformComponent, config.RotationSpeed);
            }
            
            // Apply to aggro detection component
            var aggroComponent = entity.GetComponent<AggroDetectionComponent>();
            if (aggroComponent != null)
            {
                var rangeField = aggroComponent.GetType().GetField("_aggroRange", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (rangeField != null)
                    rangeField.SetValue(aggroComponent, config.AggroRange);
            }
            
            // Apply behavior weights
            var behaviorComponent = entity.GetComponent<WeightedBehaviorComponent>();
            if (behaviorComponent != null)
            {
                // This requires a more complex implementation that depends on your behavior system
                // The following is a simplified approach
                foreach (var behavior in config.BehaviorWeights)
                {
                    // Apply weight to behavior if it exists
                    // This is just a placeholder - actual implementation depends on your system
                    behaviorComponent.SetBehaviorWeight(behavior.Key, behavior.Value);
                }
            }
        }

        // Track the selected unit type for UI
        private UnitType _selectedUnitType = UnitType.Infantry;

        // Enum definitions for configuration mode
        public enum ConfigurationMode
        {
            Manual,
            Auto
        }

        // Enum definition for game balance types
        public enum GameBalanceType
        {
            Balanced,
            OffensiveFocused,
            DefensiveFocused,
            MobilityCentered
        }
    }

    // Extension method for WeightedBehaviorComponent (assumed implementation)
    public static class WeightedBehaviorComponentExtensions
    {
        public static void SetBehaviorWeight(this WeightedBehaviorComponent component, string behaviorName, float weight)
        {
            // This is a placeholder implementation
            // In a real implementation, you would use reflection or a direct API call
            // to set the weight of the behavior in the component
            
            // Example:
            // if (component.BehaviorManager != null)
            // {
            //     var behavior = component.BehaviorManager.GetBehavior(behaviorName);
            //     if (behavior != null)
            //     {
            //         behavior.SetWeight(weight);
            //     }
            // }
            
            // For now, just log that we would set this
            Debug.Log($"Would set weight of {behaviorName} to {weight} on {component.Entity.Id}");
        }
    }
}