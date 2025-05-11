using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using VikingRaven.Configuration;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace Editor
{
    public class NewTroopSetupWindow : OdinEditorWindow
    {
        [MenuItem("VikingRaven/Setup/New Troop Setup")]
        private static void OpenWindow()
        {
            GetWindow<NewTroopSetupWindow>().Show();
        }

        [PropertySpace(10)]
        [Title("New Troop Setup Window", "Create and configure new troop units with Odin Inspector")]
        
        [TabGroup("Basic", "General Settings")]
        [BoxGroup("Basic/General Settings/Basic Info")]
        [LabelText("Troop Name")]
        [Required]
        public string TroopName = "NewTroop";
        
        [BoxGroup("Basic/General Settings/Basic Info")]
        [LabelText("Unit Type")]
        public UnitType UnitType = UnitType.Infantry;
        
        [BoxGroup("Basic/General Settings/Basic Info")]
        [LabelText("Create Prefab")]
        public bool CreatePrefab = true;
        
        [BoxGroup("Basic/General Settings/Basic Info")]
        [ShowIf("CreatePrefab")]
        [FolderPath(RequireExistingPath = true, AbsolutePath = false)]
        [LabelText("Prefab Folder")]
        public string PrefabFolder = "Assets/Prefabs/Units";
        
        [TabGroup("Basic", "Visual Settings")]
        [BoxGroup("Basic/Visual Settings/Appearance")]
        [LabelText("Base Mesh Type")]
        public PrimitiveType MeshType = PrimitiveType.Capsule;
        
        [BoxGroup("Basic/Visual Settings/Appearance")]
        [LabelText("Unit Color")]
        public Color UnitColor = Color.blue;
        
        [BoxGroup("Basic/Visual Settings/Appearance")]
        [LabelText("Scale")]
        public Vector3 Scale = new Vector3(1, 1, 1);
        
        [TabGroup("Components", "Required Components")]
        [BoxGroup("Components/Required Components/Core")]
        [LabelText("Base Components")]
        [TableList(ShowIndexLabels = false)]
        public List<ComponentInfo> CoreComponents = new List<ComponentInfo>
        {
            new ComponentInfo { ComponentName = "TransformComponent", AddComponent = true, IsRequired = true },
            new ComponentInfo { ComponentName = "UnitTypeComponent", AddComponent = true, IsRequired = true },
            new ComponentInfo { ComponentName = "StateComponent", AddComponent = true, IsRequired = true },
            new ComponentInfo { ComponentName = "HealthComponent", AddComponent = true, IsRequired = true }
        };
        
        [TabGroup("Components", "Optional Components")]
        [BoxGroup("Components/Optional Components/Movement")]
        [LabelText("Movement Components")]
        [TableList(ShowIndexLabels = false)]
        public List<ComponentInfo> MovementComponents = new List<ComponentInfo>
        {
            new ComponentInfo { ComponentName = "NavigationComponent", AddComponent = true },
            new ComponentInfo { ComponentName = "FormationComponent", AddComponent = true },
            new ComponentInfo { ComponentName = "SteeringComponent", AddComponent = false }
        };
        
        [BoxGroup("Components/Optional Components/Combat")]
        [LabelText("Combat Components")]
        [TableList(ShowIndexLabels = false)]
        public List<ComponentInfo> CombatComponents = new List<ComponentInfo>
        {
            new ComponentInfo { ComponentName = "CombatComponent", AddComponent = true },
            new ComponentInfo { ComponentName = "AggroDetectionComponent", AddComponent = true },
            new ComponentInfo { ComponentName = "WeightedBehaviorComponent", AddComponent = true }
        };
        
        [TabGroup("Stats", "Combat Stats")]
        [BoxGroup("Stats/Combat Stats/Health")]
        [LabelText("Max Health")]
        [Range(10, 500)]
        public float MaxHealth = 100;
        
        [BoxGroup("Stats/Combat Stats/Health")]
        [LabelText("Health Regeneration")]
        [Range(0, 10)]
        public float HealthRegen = 0;
        
        [BoxGroup("Stats/Combat Stats/Attack")]
        [LabelText("Attack Damage")]
        [Range(5, 100)]
        public float AttackDamage = 20;
        
        [BoxGroup("Stats/Combat Stats/Attack")]
        [LabelText("Attack Range")]
        [Range(0.5f, 20)]
        public float AttackRange = 2.0f;
        
        [BoxGroup("Stats/Combat Stats/Attack")]
        [LabelText("Attack Cooldown")]
        [Range(0.1f, 5)]
        public float AttackCooldown = 1.5f;
        
        [TabGroup("Stats", "Movement Stats")]
        [BoxGroup("Stats/Movement Stats/Speed")]
        [LabelText("Move Speed")]
        [Range(1, 10)]
        public float MoveSpeed = 3.5f;
        
        [BoxGroup("Stats/Movement Stats/Speed")]
        [LabelText("Rotation Speed")]
        [Range(1, 10)]
        public float RotationSpeed = 5.0f;
        
        [BoxGroup("Stats/Movement Stats/Detection")]
        [LabelText("Aggro Range")]
        [Range(5, 30)]
        public float AggroRange = 10.0f;
        
        [TabGroup("Stats", "AI Settings")]
        [BoxGroup("Stats/AI Settings/Behaviors")]
        [LabelText("Behavior Weights")]
        [TableList(ShowIndexLabels = false)]
        public List<BehaviorWeight> BehaviorWeights = new List<BehaviorWeight>
        {
            new BehaviorWeight { BehaviorName = "Move", Weight = 2.0f },
            new BehaviorWeight { BehaviorName = "Attack", Weight = 3.0f },
            new BehaviorWeight { BehaviorName = "Strafe", Weight = 1.5f }
        };
        
        [BoxGroup("Stats/AI Settings/Formations")]
        [LabelText("Formation Preferences")]
        [TableList(ShowIndexLabels = false)]
        public List<FormationPreference> FormationPreferences = new List<FormationPreference>
        {
            new FormationPreference { FormationType = FormationType.Line, Preference = 1.0f },
            new FormationPreference { FormationType = FormationType.Phalanx, Preference = 0.5f },
            new FormationPreference { FormationType = FormationType.Circle, Preference = 0.7f }
        };
        
        [TabGroup("Advanced", "Custom Parameters")]
        [BoxGroup("Advanced/Custom Parameters/Extra")]
        [LabelText("Custom Parameters")]
        [TableList(ShowIndexLabels = false)]
        public List<CustomParameter> CustomParameters = new List<CustomParameter>();
        
        [TabGroup("Advanced", "Configuration")]
        [BoxGroup("Advanced/Configuration/Settings")]
        [LabelText("Generate Config File")]
        public bool GenerateConfigFile = true;
        
        [BoxGroup("Advanced/Configuration/Settings")]
        [ShowIf("GenerateConfigFile")]
        [LabelText("Add to Existing Config")]
        public bool AddToExistingConfig = false;
        
        [BoxGroup("Advanced/Configuration/Settings")]
        [ShowIf("@this.GenerateConfigFile && this.AddToExistingConfig")]
        [AssetsOnly, AssetSelector(Paths = "Assets")]
        public TroopConfigurationSO ExistingTroopConfig;
        
        [PropertySpace(20)]
        [Button("Create Troop", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        public void CreateTroop()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Creating Troop", "Preparing...", 0.1f);
                
                // Check if a troop with this name already exists
                if (CreatePrefab)
                {
                    string prefabPath = Path.Combine(PrefabFolder, $"{TroopName}.prefab");
                    if (File.Exists(prefabPath))
                    {
                        bool overwrite = EditorUtility.DisplayDialog(
                            "Prefab Already Exists",
                            $"A prefab named '{TroopName}' already exists. Do you want to overwrite it?",
                            "Overwrite",
                            "Cancel"
                        );
                        
                        if (!overwrite)
                            return;
                    }
                    
                    // Make sure the directory exists
                    Directory.CreateDirectory(PrefabFolder);
                }
                
                EditorUtility.DisplayProgressBar("Creating Troop", "Creating game object...", 0.3f);
                
                // Create the base game object
                GameObject troopObject = new GameObject(TroopName);
                
                // Add mesh renderer and material
                EditorUtility.DisplayProgressBar("Creating Troop", "Adding visuals...", 0.4f);
                AddVisuals(troopObject);
                
                // Add entity component
                troopObject.AddComponent<BaseEntity>();
                
                // Add components
                EditorUtility.DisplayProgressBar("Creating Troop", "Adding components...", 0.6f);
                AddComponents(troopObject);
                
                // Set component values
                EditorUtility.DisplayProgressBar("Creating Troop", "Configuring components...", 0.7f);
                ConfigureComponents(troopObject);
                
                // Create prefab
                if (CreatePrefab)
                {
                    EditorUtility.DisplayProgressBar("Creating Troop", "Creating prefab...", 0.8f);
                    string prefabPath = Path.Combine(PrefabFolder, $"{TroopName}.prefab");
                    GameObject prefab = PrefabUtility.SaveAsPrefabAsset(troopObject, prefabPath);
                    
                    // Log success
                    Debug.Log($"Created troop prefab at: {prefabPath}");
                    
                    // Cleanup temporary object
                    DestroyImmediate(troopObject);
                    
                    // Focus on the new prefab
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
                
                // Create or update configuration file
                if (GenerateConfigFile)
                {
                    EditorUtility.DisplayProgressBar("Creating Troop", "Generating configuration...", 0.9f);
                    GenerateConfiguration();
                }
                
                EditorUtility.DisplayDialog("Success", $"Troop '{TroopName}' created successfully!", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating troop: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"Failed to create troop: {ex.Message}", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void AddVisuals(GameObject troopObject)
        {
            // Create the base mesh
            GameObject meshObject = GameObject.CreatePrimitive(MeshType);
            meshObject.name = "Mesh";
            meshObject.transform.SetParent(troopObject.transform);
            meshObject.transform.localPosition = Vector3.zero;
            meshObject.transform.localScale = Scale;
            
            // Set material color
            Renderer renderer = meshObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = UnitColor;
            }
            
            // Create unit type-specific visuals
            GameObject unitTypeVisual = new GameObject("UnitTypeVisual");
            unitTypeVisual.transform.SetParent(troopObject.transform);
            
            switch (UnitType)
            {
                case UnitType.Infantry:
                    CreateInfantryVisuals(unitTypeVisual);
                    break;
                case UnitType.Archer:
                    CreateArcherVisuals(unitTypeVisual);
                    break;
                case UnitType.Pike:
                    CreatePikeVisuals(unitTypeVisual);
                    break;
            }
        }

        private void CreateInfantryVisuals(GameObject parent)
        {
            // Infantry typically has shield and sword
            // Shield
            GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shield.name = "Shield";
            shield.transform.SetParent(parent.transform);
            shield.transform.localPosition = new Vector3(-0.5f, 0, 0.2f);
            shield.transform.localRotation = Quaternion.Euler(0, 0, 0);
            shield.transform.localScale = new Vector3(0.1f, 0.6f, 0.4f);
            
            // Sword
            GameObject sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sword.name = "Sword";
            sword.transform.SetParent(parent.transform);
            sword.transform.localPosition = new Vector3(0.5f, 0, 0);
            sword.transform.localRotation = Quaternion.Euler(0, 0, -45);
            sword.transform.localScale = new Vector3(0.1f, 0.5f, 0.05f);
            
            // Set materials
            SetUnitTypeMaterials(shield, sword);
        }

        private void CreateArcherVisuals(GameObject parent)
        {
            // Archer has a bow
            GameObject bow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bow.name = "Bow";
            bow.transform.SetParent(parent.transform);
            bow.transform.localPosition = new Vector3(0.5f, 0, 0);
            bow.transform.localRotation = Quaternion.Euler(90, 0, 0);
            bow.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);
            
            // Bow string
            GameObject bowString = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowString.name = "BowString";
            bowString.transform.SetParent(parent.transform);
            bowString.transform.localPosition = new Vector3(0.5f, 0, 0);
            bowString.transform.localRotation = Quaternion.Euler(90, 0, 0);
            bowString.transform.localScale = new Vector3(0.01f, 0.48f, 0.01f);
            
            // Quiver
            GameObject quiver = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            quiver.name = "Quiver";
            quiver.transform.SetParent(parent.transform);
            quiver.transform.localPosition = new Vector3(-0.3f, 0, -0.3f);
            quiver.transform.localRotation = Quaternion.Euler(30, 0, 0);
            quiver.transform.localScale = new Vector3(0.15f, 0.4f, 0.15f);
            
            // Set materials
            Renderer bowRenderer = bow.GetComponent<Renderer>();
            Renderer stringRenderer = bowString.GetComponent<Renderer>();
            Renderer quiverRenderer = quiver.GetComponent<Renderer>();
            
            if (bowRenderer != null && stringRenderer != null && quiverRenderer != null)
            {
                Material woodMaterial = new Material(Shader.Find("Standard"));
                woodMaterial.color = new Color(0.5f, 0.35f, 0.15f); // Brown
                
                Material stringMaterial = new Material(Shader.Find("Standard"));
                stringMaterial.color = new Color(0.9f, 0.9f, 0.9f); // Light gray
                
                Material leatherMaterial = new Material(Shader.Find("Standard"));
                leatherMaterial.color = new Color(0.6f, 0.4f, 0.2f); // Leather brown
                
                bowRenderer.material = woodMaterial;
                stringRenderer.material = stringMaterial;
                quiverRenderer.material = leatherMaterial;
            }
        }

        private void CreatePikeVisuals(GameObject parent)
        {
            // Pike unit has a long spear
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(parent.transform);
            pole.transform.localPosition = new Vector3(0, 0, 0.8f);
            pole.transform.localRotation = Quaternion.Euler(90, 0, 0);
            pole.transform.localScale = new Vector3(0.05f, 1.5f, 0.05f);
            
            GameObject spearhead = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            spearhead.name = "Spearhead";
            spearhead.transform.SetParent(parent.transform);
            spearhead.transform.localPosition = new Vector3(0, 0, 2.3f);
            spearhead.transform.localRotation = Quaternion.Euler(90, 0, 0);
            spearhead.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
            
            // Set materials
            Renderer poleRenderer = pole.GetComponent<Renderer>();
            Renderer spearheadRenderer = spearhead.GetComponent<Renderer>();
            
            if (poleRenderer != null && spearheadRenderer != null)
            {
                Material poleMaterial = new Material(Shader.Find("Standard"));
                poleMaterial.color = new Color(0.5f, 0.35f, 0.15f); // Brown
                
                Material metalMaterial = new Material(Shader.Find("Standard"));
                metalMaterial.color = new Color(0.7f, 0.7f, 0.7f); // Silver
                
                poleRenderer.material = poleMaterial;
                spearheadRenderer.material = metalMaterial;
            }
        }

        private void SetUnitTypeMaterials(GameObject part1, GameObject part2)
        {
            Renderer part1Renderer = part1.GetComponent<Renderer>();
            Renderer part2Renderer = part2.GetComponent<Renderer>();
            
            if (part1Renderer != null && part2Renderer != null)
            {
                Material metalMaterial = new Material(Shader.Find("Standard"));
                metalMaterial.color = new Color(0.7f, 0.7f, 0.7f); // Silver
                
                Material shieldMaterial = new Material(Shader.Find("Standard"));
                shieldMaterial.color = new Color(0.8f, 0.1f, 0.1f); // Red for shield
                
                part1Renderer.material = shieldMaterial;
                part2Renderer.material = metalMaterial;
            }
        }

        private void AddComponents(GameObject troopObject)
        {
            // Add all required components
            foreach (var compInfo in CoreComponents)
            {
                if (compInfo.AddComponent)
                {
                    AddComponentByName(troopObject, compInfo.ComponentName);
                }
            }
            
            // Add movement components
            foreach (var compInfo in MovementComponents)
            {
                if (compInfo.AddComponent)
                {
                    AddComponentByName(troopObject, compInfo.ComponentName);
                }
            }
            
            // Add combat components
            foreach (var compInfo in CombatComponents)
            {
                if (compInfo.AddComponent)
                {
                    AddComponentByName(troopObject, compInfo.ComponentName);
                }
            }
        }

        private void AddComponentByName(GameObject obj, string componentName)
        {
            string fullTypeName = $"VikingRaven.Units.Components.{componentName}";
            Type componentType = Type.GetType(fullTypeName);
            
            if (componentType == null)
            {
                fullTypeName = $"VikingRaven.Core.{componentName}";
                componentType = Type.GetType(fullTypeName);
            }
            
            if (componentType == null)
            {
                Debug.LogWarning($"Component type '{componentName}' not found. Skipping.");
                return;
            }
            
            obj.AddComponent(componentType);
        }

        private void ConfigureComponents(GameObject troopObject)
        {
            // Set UnitType
            UnitTypeComponent unitTypeComponent = troopObject.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                // Set the unit type using reflection since SetUnitType is not directly accessible
                var method = unitTypeComponent.GetType().GetMethod("SetUnitType");
                if (method != null)
                {
                    method.Invoke(unitTypeComponent, new object[] { UnitType });
                }
            }
            
            // Set Health 
            HealthComponent healthComponent = troopObject.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                var maxHealthField = healthComponent.GetType().GetField("_maxHealth", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    
                if (maxHealthField != null)
                    maxHealthField.SetValue(healthComponent, MaxHealth);
                    
                var healthRegenField = healthComponent.GetType().GetField("_regenerationRate", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    
                if (healthRegenField != null)
                    healthRegenField.SetValue(healthComponent, HealthRegen);
            }
            
            // Set Combat parameters
            CombatComponent combatComponent = troopObject.GetComponent<CombatComponent>();
            if (combatComponent != null)
            {
                var damageField = combatComponent.GetType().GetField("_attackDamage", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    
                if (damageField != null)
                    damageField.SetValue(combatComponent, AttackDamage);
                    
                var rangeField = combatComponent.GetType().GetField("_attackRange", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    
                if (rangeField != null)
                    rangeField.SetValue(combatComponent, AttackRange);
                    
                var cooldownField = combatComponent.GetType().GetField("_attackCooldown", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    
                if (cooldownField != null)
                    cooldownField.SetValue(combatComponent, AttackCooldown);
            }
            
            // Set aggro range
            AggroDetectionComponent aggroComponent = troopObject.GetComponent<AggroDetectionComponent>();
            if (aggroComponent != null)
            {
                var aggroRangeField = aggroComponent.GetType().GetField("_aggroRange", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    
                if (aggroRangeField != null)
                    aggroRangeField.SetValue(aggroComponent, AggroRange);
            }
            
            // Set movement parameters
            TransformComponent transformComponent = troopObject.GetComponent<TransformComponent>();
            if (transformComponent != null)
            {
                var rotationSpeedField = transformComponent.GetType().GetField("_rotationSpeed", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    
                if (rotationSpeedField != null)
                    rotationSpeedField.SetValue(transformComponent, RotationSpeed);
            }
            
            // Set NavigationComponent move speed
            NavigationComponent navComponent = troopObject.GetComponent<NavigationComponent>();
            if (navComponent != null)
            {
                // Adjust NavMeshAgent speed if available
                var navMeshAgentField = navComponent.GetType().GetField("_navMeshAgent", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    
                if (navMeshAgentField != null)
                {
                    var navMeshAgent = navMeshAgentField.GetValue(navComponent) as UnityEngine.AI.NavMeshAgent;
                    if (navMeshAgent != null)
                    {
                        navMeshAgent.speed = MoveSpeed;
                    }
                }
            }
            
            // Initialize any components that need it
            InitializeComponents(troopObject);
        }

        private void InitializeComponents(GameObject troopObject)
        {
            // Get all components that implement IComponent
            var components = troopObject.GetComponents<IComponent>();
            
            foreach (var component in components)
            {
                component.Initialize();
            }
        }

        private void GenerateConfiguration()
        {
            TroopConfigurationSO configSO = null;
            
            if (AddToExistingConfig && ExistingTroopConfig != null)
            {
                configSO = ExistingTroopConfig;
            }
            else
            {
                // Create a new config asset
                configSO = CreateInstance<TroopConfigurationSO>();
                
                // Reset to defaults
                configSO.ResetToDefaults();
                
                // Save the asset
                string configFolder = "Assets/ScriptableObjects/Configurations";
                Directory.CreateDirectory(configFolder);
                
                string configPath = Path.Combine(configFolder, $"{TroopName}Configuration.asset");
                AssetDatabase.CreateAsset(configSO, configPath);
            }
            
            // Update the configuration for this unit type
            var config = configSO.GetConfigForType(UnitType);
            
            // Set basic stats
            config.MaxHealth = MaxHealth;
            config.MoveSpeed = MoveSpeed;
            config.RotationSpeed = RotationSpeed;
            config.AttackDamage = AttackDamage;
            config.AttackRange = AttackRange;
            config.AttackCooldown = AttackCooldown;
            config.AggroRange = AggroRange;
            
            // Set behavior weights
            config.BehaviorWeights.Clear();
            foreach (var behavior in BehaviorWeights)
            {
                config.BehaviorWeights[behavior.BehaviorName] = behavior.Weight;
            }
            
            // Set formation preferences
            config.FormationPreferences.Clear();
            foreach (var formation in FormationPreferences)
            {
                config.FormationPreferences[formation.FormationType] = formation.Preference;
            }
            
            // Set custom parameters
            config.CustomParameters.Clear();
            foreach (var param in CustomParameters)
            {
                config.CustomParameters[param.ParameterName] = param.Value;
            }
            
            EditorUtility.SetDirty(configSO);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Generated configuration for {TroopName} ({UnitType})");
        }

        [PropertySpace(10)]
        [Button("Reset to Type Defaults"), GUIColor(1, 0.6f, 0.4f)]
        public void ResetToTypeDefaults()
        {
            switch (UnitType)
            {
                case UnitType.Infantry:
                    SetInfantryDefaults();
                    break;
                case UnitType.Archer:
                    SetArcherDefaults();
                    break;
                case UnitType.Pike:
                    SetPikeDefaults();
                    break;
            }
        }

        private void SetInfantryDefaults()
        {
            // Reset name and prefab settings
            TroopName = "Infantry";
            MeshType = PrimitiveType.Capsule;
            UnitColor = Color.blue;
            Scale = new Vector3(1, 1, 1);
            
            // Combat stats
            MaxHealth = 100;
            HealthRegen = 0;
            AttackDamage = 20;
            AttackRange = 1.5f;
            AttackCooldown = 1.5f;
            
            // Movement stats
            MoveSpeed = 3.5f;
            RotationSpeed = 5.0f;
            AggroRange = 10.0f;
            
            // Behavior weights
            BehaviorWeights = new List<BehaviorWeight>
            {
                new BehaviorWeight { BehaviorName = "Move", Weight = 2.0f },
                new BehaviorWeight { BehaviorName = "Attack", Weight = 3.0f },
                new BehaviorWeight { BehaviorName = "Strafe", Weight = 1.5f },
                new BehaviorWeight { BehaviorName = "Protect", Weight = 2.5f },
                new BehaviorWeight { BehaviorName = "Charge", Weight = 2.0f }
            };
            
            // Formation preferences
            FormationPreferences = new List<FormationPreference>
            {
                new FormationPreference { FormationType = FormationType.Line, Preference = 1.0f },
                new FormationPreference { FormationType = FormationType.Testudo, Preference = 0.8f },
                new FormationPreference { FormationType = FormationType.Phalanx, Preference = 0.5f },
                new FormationPreference { FormationType = FormationType.Circle, Preference = 0.7f }
            };
        }

        private void SetArcherDefaults()
        {
            // Reset name and prefab settings
            TroopName = "Archer";
            MeshType = PrimitiveType.Capsule;
            UnitColor = Color.cyan;
            Scale = new Vector3(0.9f, 1, 0.9f);
            
            // Combat stats
            MaxHealth = 70;
            HealthRegen = 0;
            AttackDamage = 15;
            AttackRange = 12.0f;
            AttackCooldown = 2.0f;
            
            // Movement stats
            MoveSpeed = 3.7f;
            RotationSpeed = 6.0f;
            AggroRange = 15.0f;
            
            // Behavior weights
            BehaviorWeights = new List<BehaviorWeight>
            {
                new BehaviorWeight { BehaviorName = "Move", Weight = 2.0f },
                new BehaviorWeight { BehaviorName = "Attack", Weight = 3.5f },
                new BehaviorWeight { BehaviorName = "IdleAttack", Weight = 3.0f },
                new BehaviorWeight { BehaviorName = "Strafe", Weight = 2.5f },
                new BehaviorWeight { BehaviorName = "Cover", Weight = 2.5f },
                new BehaviorWeight { BehaviorName = "AmbushMove", Weight = 1.5f }
            };
            
            // Formation preferences
            FormationPreferences = new List<FormationPreference>
            {
                new FormationPreference { FormationType = FormationType.Line, Preference = 0.7f },
                new FormationPreference { FormationType = FormationType.Column, Preference = 0.9f },
                new FormationPreference { FormationType = FormationType.Circle, Preference = 0.5f }
            };
        }

        private void SetPikeDefaults()
        {
            // Reset name and prefab settings
            TroopName = "Pike";
            MeshType = PrimitiveType.Capsule;
            UnitColor = Color.green;
            Scale = new Vector3(1.1f, 1, 1.1f);
            
            // Combat stats
            MaxHealth = 85;
            HealthRegen = 0;
            AttackDamage = 25;
            AttackRange = 2.5f;
            AttackCooldown = 2.0f;
            
            // Movement stats
            MoveSpeed = 3.0f;
            RotationSpeed = 4.0f;
            AggroRange = 8.0f;
            
            // Behavior weights
            BehaviorWeights = new List<BehaviorWeight>
            {
                new BehaviorWeight { BehaviorName = "Move", Weight = 2.0f },
                new BehaviorWeight { BehaviorName = "Attack", Weight = 2.8f },
                new BehaviorWeight { BehaviorName = "Strafe", Weight = 1.0f },
                new BehaviorWeight { BehaviorName = "Phalanx", Weight = 3.0f }
            };
            
            // Formation preferences
            FormationPreferences = new List<FormationPreference>
            {
                new FormationPreference { FormationType = FormationType.Line, Preference = 0.6f },
                new FormationPreference { FormationType = FormationType.Phalanx, Preference = 1.0f },
                new FormationPreference { FormationType = FormationType.Column, Preference = 0.6f }
            };
        }

        // Helper class for component info
        [Serializable]
        public class ComponentInfo
        {
            [LabelText("Component")]
            public string ComponentName;
            
            [LabelText("Add")]
            [ToggleLeft]
            public bool AddComponent = true;
            
            [LabelText("Required")]
            [ToggleLeft]
            public bool IsRequired = false;
        }

        // Helper class for behavior weights
        [Serializable]
        public class BehaviorWeight
        {
            [LabelText("Behavior")]
            public string BehaviorName;
            
            [LabelText("Weight")]
            [Range(0, 5)]
            public float Weight = 1.0f;
        }

        // Helper class for formation preferences
        [Serializable]
        public class FormationPreference
        {
            [LabelText("Formation")]
            public FormationType FormationType;
            
            [LabelText("Preference")]
            [Range(0, 1)]
            public float Preference = 1.0f;
        }

        // Helper class for custom parameters
        [Serializable]
        public class CustomParameter
        {
            [LabelText("Parameter")]
            public string ParameterName;
            
            [LabelText("Value")]
            public string Value;
        }
    }
}