using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using VikingRaven.Configuration;
using VikingRaven.Core;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Game;
using VikingRaven.Game.Examples;
using VikingRaven.Units.Components;
using VikingRaven.Units.Systems;

namespace Editor
{
    public class SceneSetupWindow : OdinEditorWindow
    {
        [MenuItem("VikingRaven/Setup/Scene Setup")]
        private static void OpenWindow()
        {
            GetWindow<SceneSetupWindow>().Show();
        }

        [PropertySpace(10)]
        [Title("Scene Setup Window", "Automatically set up a complete tactical game scene")]
        
        [BoxGroup("Scene Configuration")]
        public bool SetupNewScene = true;
        
        [BoxGroup("Scene Configuration")]
        [ShowIf("SetupNewScene")]
        public string NewSceneName = "TacticalBattle";
        
        [BoxGroup("Scene Configuration")]
        [LabelText("Create Terrain")]
        public bool CreateTerrain = true;
        
        [BoxGroup("Scene Configuration")]
        [ShowIf("CreateTerrain")]
        [MinMaxSlider(10, 1000, true)]
        public Vector2 TerrainSize = new Vector2(100, 100);
        
        [PropertySpace(10)]
        [TabGroup("Core Setup")]
        [BoxGroup("Core Setup/Core Objects")]
        [LabelText("Create Core Systems")]
        public bool CreateCoreSystems = true;
        
        [BoxGroup("Core Setup/Core Objects")]
        [LabelText("Create Game Manager")]
        public bool CreateGameManager = true;
        
        [BoxGroup("Core Setup/Core Objects")]
        [LabelText("Create Entity Registry")]
        public bool CreateEntityRegistry = true;
        
        [BoxGroup("Core Setup/Core Objects")]
        [LabelText("Create System Registry")]
        public bool CreateSystemRegistry = true;
        
        [BoxGroup("Core Setup/Core Objects")]
        [LabelText("Create Game Bootstrapper")]
        public bool CreateGameBootstrapper = true;
        
        [PropertySpace(10)]
        [BoxGroup("Core Setup/Factories")]
        [LabelText("Create Unit Factory")]
        public bool CreateUnitFactory = true;
        
        [BoxGroup("Core Setup/Factories")]
        [LabelText("Create Squad Factory")]
        public bool CreateSquadFactory = true;
        
        [PropertySpace(10)]
        [TabGroup("Core Setup")]
        [BoxGroup("Core Setup/Systems")]
        [TableList(ShowIndexLabels = true, IsReadOnly = false)]
        public List<SystemSetupInfo> SystemsToCreate = new List<SystemSetupInfo>
        {
            new SystemSetupInfo { SystemType = "MovementSystem", CreateSystem = true, Priority = 10 },
            new SystemSetupInfo { SystemType = "AIDecisionSystem", CreateSystem = true, Priority = 20 },
            new SystemSetupInfo { SystemType = "FormationSystem", CreateSystem = true, Priority = 30 },
            new SystemSetupInfo { SystemType = "AggroDetectionSystem", CreateSystem = true, Priority = 40 },
            new SystemSetupInfo { SystemType = "SquadCoordinationSystem", CreateSystem = true, Priority = 50 },
            new SystemSetupInfo { SystemType = "SteeringSystem", CreateSystem = true, Priority = 60 },
            new SystemSetupInfo { SystemType = "TacticalAnalysisSystem", CreateSystem = true, Priority = 70 },
            new SystemSetupInfo { SystemType = "WeightedBehaviorSystem", CreateSystem = true, Priority = 80 }
        };
        
        [PropertySpace(10)]
        [TabGroup("Units Setup")]
        [BoxGroup("Units Setup/Player Squads")]
        [LabelText("Create Player Squads")]
        public bool CreatePlayerSquads = true;
        
        [BoxGroup("Units Setup/Player Squads")]
        [ShowIf("CreatePlayerSquads")]
        [TableList(ShowIndexLabels = false)]
        public List<SquadSetupInfo> PlayerSquads = new List<SquadSetupInfo>
        {
            new SquadSetupInfo { SquadName = "Infantry Squad", UnitType = UnitType.Infantry, UnitCount = 8, 
                Position = new Vector3(0, 0, -20), Rotation = Quaternion.identity }
        };
        
        [PropertySpace(10)]
        [BoxGroup("Units Setup/Enemy Squads")]
        [LabelText("Create Enemy Squads")]
        public bool CreateEnemySquads = true;
        
        [BoxGroup("Units Setup/Enemy Squads")]
        [ShowIf("CreateEnemySquads")]
        [TableList(ShowIndexLabels = false)]
        public List<SquadSetupInfo> EnemySquads = new List<SquadSetupInfo>
        {
            new SquadSetupInfo { SquadName = "Enemy Archer Squad", UnitType = UnitType.Archer, UnitCount = 6, 
                Position = new Vector3(0, 0, 20), Rotation = Quaternion.Euler(0, 180, 0) }
        };
        
        [PropertySpace(10)]
        [TabGroup("Units Setup")]
        [BoxGroup("Units Setup/Camera & Controls")]
        [LabelText("Create Camera")]
        public bool CreateCamera = true;
        
        [BoxGroup("Units Setup/Camera & Controls")]
        [LabelText("Create Squad Controller")]
        public bool CreateSquadController = true;
        
        [PropertySpace(10)]
        [TabGroup("Configuration")]
        [BoxGroup("Configuration/Config Files")]
        [LabelText("Load System Configuration")]
        public bool LoadSystemConfig = true;
        
        [BoxGroup("Configuration/Config Files")]
        [ShowIf("LoadSystemConfig")]
        [AssetsOnly, AssetSelector(Paths = "Assets")]
        public SystemConfigurationSO SystemConfigurationAsset;
        
        [BoxGroup("Configuration/Config Files")]
        [LabelText("Load Troop Configuration")]
        public bool LoadTroopConfig = true;
        
        [BoxGroup("Configuration/Config Files")]
        [ShowIf("LoadTroopConfig")]
        [AssetsOnly, AssetSelector(Paths = "Assets")]
        public TroopConfigurationSO TroopConfigurationAsset;

        [PropertySpace(20)]
        [Button("Setup Scene", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        public void SetupScene()
        {
            if (SetupNewScene)
            {
                // Check if there are unsaved changes
                if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty)
                {
                    bool shouldProceed = EditorUtility.DisplayDialog(
                        "Unsaved Changes",
                        "Current scene has unsaved changes. Do you want to proceed and lose these changes?",
                        "Yes, Create New Scene",
                        "Cancel"
                    );
                    
                    if (!shouldProceed)
                        return;
                }
                
                // Create a new scene
                UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                    UnityEditor.SceneManagement.NewSceneMode.Single
                );
            }
            
            try
            {
                EditorUtility.DisplayProgressBar("Setting Up Scene", "Creating core objects...", 0.1f);
                
                // Create terrain if needed
                GameObject terrain = null;
                if (CreateTerrain)
                {
                    terrain = CreateTerrainObject();
                }
                
                EditorUtility.DisplayProgressBar("Setting Up Scene", "Creating managers...", 0.2f);
                
                // Create core managers
                GameObject coreContainer = new GameObject("Core");
                
                if (CreateGameManager)
                {
                    CreateManagerObject<GameManager>(coreContainer, "GameManager");
                }
                
                if (CreateEntityRegistry)
                {
                    CreateManagerObject<EntityRegistry>(coreContainer, "EntityRegistry");
                }
                
                if (CreateSystemRegistry)
                {
                    CreateManagerObject<SystemRegistry>(coreContainer, "SystemRegistry");
                }
                
                if (CreateGameBootstrapper)
                {
                    CreateManagerObject<GameBootstrapper>(coreContainer, "GameBootstrapper");
                }
                
                // Create factories
                if (CreateUnitFactory || CreateSquadFactory)
                {
                    GameObject factoriesContainer = new GameObject("Factories");
                    factoriesContainer.transform.SetParent(coreContainer.transform);
                    
                    if (CreateUnitFactory)
                    {
                        var factoryObj = new GameObject("UnitFactory");
                        factoryObj.transform.SetParent(factoriesContainer.transform);
                        factoryObj.AddComponent<UnitFactory>();
                    }
                    
                    if (CreateSquadFactory)
                    {
                        var factoryObj = new GameObject("SquadFactory");
                        factoryObj.transform.SetParent(factoriesContainer.transform);
                        factoryObj.AddComponent<SquadFactory>();
                    }
                }
                
                EditorUtility.DisplayProgressBar("Setting Up Scene", "Creating systems...", 0.3f);
                
                // Create Systems
                GameObject systemsContainer = null;
                if (CreateCoreSystems && SystemsToCreate.Count > 0)
                {
                    systemsContainer = new GameObject("Systems");
                    systemsContainer.transform.SetParent(coreContainer.transform);
                    
                    foreach (var systemInfo in SystemsToCreate)
                    {
                        if (systemInfo.CreateSystem)
                        {
                            CreateSystemFromName(systemsContainer, systemInfo.SystemType, systemInfo.Priority);
                        }
                    }
                }
                
                EditorUtility.DisplayProgressBar("Setting Up Scene", "Creating player units...", 0.6f);
                
                // Create squads
                GameObject squadsContainer = new GameObject("Squads");
                
                GameObject playerSquadsContainer = null;
                if (CreatePlayerSquads && PlayerSquads.Count > 0)
                {
                    playerSquadsContainer = new GameObject("PlayerSquads");
                    playerSquadsContainer.transform.SetParent(squadsContainer.transform);
                    
                    int index = 0;
                    foreach (var squadInfo in PlayerSquads)
                    {
                        CreateSquad(playerSquadsContainer, squadInfo, index++, true);
                    }
                }
                
                EditorUtility.DisplayProgressBar("Setting Up Scene", "Creating enemy units...", 0.7f);
                
                GameObject enemySquadsContainer = null;
                if (CreateEnemySquads && EnemySquads.Count > 0)
                {
                    enemySquadsContainer = new GameObject("EnemySquads");
                    enemySquadsContainer.transform.SetParent(squadsContainer.transform);
                    
                    int index = 0;
                    foreach (var squadInfo in EnemySquads)
                    {
                        CreateSquad(enemySquadsContainer, squadInfo, index++, false);
                    }
                }
                
                EditorUtility.DisplayProgressBar("Setting Up Scene", "Creating camera and controls...", 0.8f);
                
                // Create camera and controls
                if (CreateCamera)
                {
                    GameObject cameraObj = new GameObject("Main Camera");
                    Camera camera = cameraObj.AddComponent<Camera>();
                    cameraObj.tag = "MainCamera";
                    
                    // Set up camera position and rotation
                    cameraObj.transform.position = new Vector3(0, 30, -20);
                    cameraObj.transform.rotation = Quaternion.Euler(60, 0, 0);
                    
                    // Add a simple orbit camera script if available
                    Type orbitCameraType = Type.GetType("VikingRaven.Game.OrbitCamera");
                    if (orbitCameraType != null)
                    {
                        cameraObj.AddComponent(orbitCameraType);
                    }
                }
                
                if (CreateSquadController)
                {
                    GameObject controllerObj = new GameObject("SquadController");
                    controllerObj.AddComponent<SimpleSquadController>();
                    
                    // Link to camera and system if exists
                    SimpleSquadController controller = controllerObj.GetComponent<SimpleSquadController>();
                    if (controller != null)
                    {
                        // controller._mainCamera = Camera.main;
                        
                        if (systemsContainer != null)
                        {
                            var coordinationSystem = systemsContainer.GetComponentInChildren<SquadCoordinationSystem>();
                            if (coordinationSystem != null)
                            {
                                var serializedObj = new SerializedObject(controller);
                                var systemField = serializedObj.FindProperty("_squadCoordinationSystem");
                                systemField.objectReferenceValue = coordinationSystem;
                                serializedObj.ApplyModifiedProperties();
                            }
                        }
                    }
                }
                
                EditorUtility.DisplayProgressBar("Setting Up Scene", "Applying configurations...", 0.9f);
                
                // Apply configurations if specified
                if (LoadSystemConfig && SystemConfigurationAsset != null && systemsContainer != null)
                {
                    ApplySystemConfiguration(systemsContainer, SystemConfigurationAsset);
                }
                
                if (LoadTroopConfig && TroopConfigurationAsset != null)
                {
                    ApplyTroopConfiguration(TroopConfigurationAsset);
                }
                
                EditorUtility.DisplayProgressBar("Setting Up Scene", "Finalizing...", 1.0f);
                
                // Save the scene if it's new
                if (SetupNewScene)
                {
                    string scenePath = $"Assets/Scenes/{NewSceneName}.unity";
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(), 
                        scenePath, 
                        true
                    );
                }
                
                EditorUtility.DisplayDialog("Success", "Scene setup completed successfully!", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during scene setup: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"Failed to set up scene: {ex.Message}", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private GameObject CreateTerrainObject()
        {
            // Create terrain data
            TerrainData terrainData = new TerrainData();
            terrainData.size = new Vector3(TerrainSize.x, 20, TerrainSize.y);
            
            // Create terrain game object
            GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "Terrain";
            
            // Flatten the terrain
            float[,] heights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
            for (int i = 0; i < terrainData.heightmapResolution; i++)
            {
                for (int j = 0; j < terrainData.heightmapResolution; j++)
                {
                    heights[i, j] = 0; // Flat terrain
                }
            }
            terrainData.SetHeights(0, 0, heights);
            
            // Set the terrain material if needed
            Terrain terrain = terrainObject.GetComponent<Terrain>();
            terrain.materialTemplate = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Terrain-Standard.mat");
            
            return terrainObject;
        }

        private T CreateManagerObject<T>(GameObject parent, string name) where T : Component
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform);
            return obj.AddComponent<T>();
        }

        private void CreateSystemFromName(GameObject parent, string systemTypeName, int priority)
        {
            string fullTypeName = $"VikingRaven.Units.Systems.{systemTypeName}";
            Type systemType = Type.GetType(fullTypeName);
            
            if (systemType == null)
            {
                fullTypeName = $"VikingRaven.Core.Systems.{systemTypeName}";
                systemType = Type.GetType(fullTypeName);
            }
            
            if (systemType == null)
            {
                Debug.LogWarning($"System type '{systemTypeName}' not found.");
                return;
            }
            
            GameObject systemObj = new GameObject(systemTypeName);
            systemObj.transform.SetParent(parent.transform);
            
            BaseSystem system = systemObj.AddComponent(systemType) as BaseSystem;
            if (system != null)
            {
                system.Priority = priority;
            }
        }

        private void CreateSquad(GameObject parent, SquadSetupInfo squadInfo, int index, bool isPlayer)
        {
            GameObject squadContainer = new GameObject(squadInfo.SquadName);
            squadContainer.transform.SetParent(parent.transform);
            squadContainer.transform.position = squadInfo.Position;
            squadContainer.transform.rotation = squadInfo.Rotation;
            
            // Create placeholder for units
            // In a real scenario, you would use SquadFactory to create actual units
            GameObject unitsPlaceholder = new GameObject($"Units");
            unitsPlaceholder.transform.SetParent(squadContainer.transform);
            
            // Create visual representation for the squad in editor
            GameObject visualRepresentation = new GameObject("SquadVisual");
            visualRepresentation.transform.SetParent(squadContainer.transform);
            
            // Add a visual mesh to represent the squad type
            if (squadInfo.UnitType == UnitType.Infantry)
            {
                CreateVisualMesh(visualRepresentation, PrimitiveType.Cube, isPlayer ? Color.blue : Color.red);
            }
            else if (squadInfo.UnitType == UnitType.Archer)
            {
                CreateVisualMesh(visualRepresentation, PrimitiveType.Sphere, isPlayer ? Color.cyan : Color.magenta);
            }
            else if (squadInfo.UnitType == UnitType.Pike)
            {
                CreateVisualMesh(visualRepresentation, PrimitiveType.Cylinder, isPlayer ? Color.green : Color.yellow);
            }
            
            // Add info component to the squad for easy identification
            SquadInfoHelper infoHelper = squadContainer.AddComponent<SquadInfoHelper>();
            infoHelper.SquadName = squadInfo.SquadName;
            infoHelper.UnitType = squadInfo.UnitType;
            infoHelper.UnitCount = squadInfo.UnitCount;
            infoHelper.IsPlayerSquad = isPlayer;
            infoHelper.SquadId = isPlayer ? index + 1 : 101 + index;
        }

        private void CreateVisualMesh(GameObject parent, PrimitiveType meshType, Color color)
        {
            GameObject meshObj = GameObject.CreatePrimitive(meshType);
            meshObj.transform.SetParent(parent.transform);
            meshObj.transform.localPosition = Vector3.zero;
            meshObj.transform.localScale = new Vector3(5, 1, 5);
            
            // Set material color
            Renderer renderer = meshObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = color;
            }
        }

        private void ApplySystemConfiguration(GameObject systemsContainer, SystemConfigurationSO config)
        {
            if (systemsContainer == null || config == null)
                return;
                
            BaseSystem[] systems = systemsContainer.GetComponentsInChildren<BaseSystem>();
            
            foreach (var system in systems)
            {
                string systemType = system.GetType().Name;
                var systemConfig = config.GetSystemConfig(systemType);
                
                if (systemConfig != null)
                {
                    system.IsActive = systemConfig.IsActive;
                    system.Priority = systemConfig.Priority;
                    
                    // Apply custom parameters
                    // This would require reflection to access private fields
                    // Simplified implementation for the tutorial
                }
            }
        }

        private void ApplyTroopConfiguration(TroopConfigurationSO config)
        {
            if (config == null)
                return;
                
            // In a real scenario, you would find all unit prefabs and apply the configuration
            // For this tutorial, we just log the application
            Debug.Log("Applied troop configuration to scene.");
            
            // Update squad info helpers
            SquadInfoHelper[] squadInfos = FindObjectsOfType<SquadInfoHelper>();
            foreach (var info in squadInfos)
            {
                var troopConfig = config.GetConfigForType(info.UnitType);
                info.UpdateConfigInfo(troopConfig);
            }
        }

        // Reset button to default settings
        [PropertySpace(10)]
        [Button("Reset to Defaults"), GUIColor(1, 0.6f, 0.4f)]
        public void ResetToDefaults()
        {
            NewSceneName = "TacticalBattle";
            CreateTerrain = true;
            TerrainSize = new Vector2(100, 100);
            
            CreateCoreSystems = true;
            CreateGameManager = true;
            CreateEntityRegistry = true;
            CreateSystemRegistry = true;
            CreateGameBootstrapper = true;
            
            CreateUnitFactory = true;
            CreateSquadFactory = true;
            
            SystemsToCreate = new List<SystemSetupInfo>
            {
                new SystemSetupInfo { SystemType = "MovementSystem", CreateSystem = true, Priority = 10 },
                new SystemSetupInfo { SystemType = "AIDecisionSystem", CreateSystem = true, Priority = 20 },
                new SystemSetupInfo { SystemType = "FormationSystem", CreateSystem = true, Priority = 30 },
                new SystemSetupInfo { SystemType = "AggroDetectionSystem", CreateSystem = true, Priority = 40 },
                new SystemSetupInfo { SystemType = "SquadCoordinationSystem", CreateSystem = true, Priority = 50 },
                new SystemSetupInfo { SystemType = "SteeringSystem", CreateSystem = true, Priority = 60 },
                new SystemSetupInfo { SystemType = "TacticalAnalysisSystem", CreateSystem = true, Priority = 70 },
                new SystemSetupInfo { SystemType = "WeightedBehaviorSystem", CreateSystem = true, Priority = 80 }
            };
            
            CreatePlayerSquads = true;
            PlayerSquads = new List<SquadSetupInfo>
            {
                new SquadSetupInfo { SquadName = "Infantry Squad", UnitType = UnitType.Infantry, UnitCount = 8, 
                    Position = new Vector3(0, 0, -20), Rotation = Quaternion.identity }
            };
            
            CreateEnemySquads = true;
            EnemySquads = new List<SquadSetupInfo>
            {
                new SquadSetupInfo { SquadName = "Enemy Archer Squad", UnitType = UnitType.Archer, UnitCount = 6, 
                    Position = new Vector3(0, 0, 20), Rotation = Quaternion.Euler(0, 180, 0) }
            };
            
            CreateCamera = true;
            CreateSquadController = true;
            
            LoadSystemConfig = true;
            SystemConfigurationAsset = null;
            LoadTroopConfig = true;
            TroopConfigurationAsset = null;
        }

        // Helper class for system setup info
        [Serializable]
        public class SystemSetupInfo
        {
            [LabelText("System Type")]
            public string SystemType;
            
            [LabelText("Create")]
            [ToggleLeft]
            public bool CreateSystem = true;
            
            [LabelText("Priority")]
            [Range(0, 100)]
            public int Priority = 0;
        }

        // Helper class for squad setup info
        [Serializable]
        public class SquadSetupInfo
        {
            [LabelText("Squad Name")]
            public string SquadName;
            
            [LabelText("Unit Type")]
            public UnitType UnitType;
            
            [LabelText("Unit Count")]
            [MinValue(1)]
            public int UnitCount = 8;
            
            [LabelText("Position")]
            public Vector3 Position;
            
            [LabelText("Rotation")]
            public Quaternion Rotation = Quaternion.identity;
        }
    }

    // Helper MonoBehaviour to store squad info in the scene
    public class SquadInfoHelper : MonoBehaviour
    {
        [ReadOnly]
        public string SquadName;
        
        [ReadOnly]
        public UnitType UnitType;
        
        [ReadOnly]
        public int UnitCount;
        
        [ReadOnly]
        public bool IsPlayerSquad;
        
        [ReadOnly]
        public int SquadId;
        
        [ReadOnly]
        public Dictionary<string, float> ConfigValues = new Dictionary<string, float>();
        
        public void UpdateConfigInfo(TroopConfigurationSO.TroopTypeConfig config)
        {
            if (config == null)
                return;
                
            ConfigValues.Clear();
            ConfigValues.Add("Health", config.MaxHealth);
            ConfigValues.Add("Speed", config.MoveSpeed);
            ConfigValues.Add("Damage", config.AttackDamage);
            ConfigValues.Add("Range", config.AttackRange);
        }
        
        private void OnDrawGizmos()
        {
            // Draw squad gizmo for easy visualization in the editor
            Gizmos.color = IsPlayerSquad ? Color.blue : Color.red;
            Gizmos.DrawWireSphere(transform.position, 5f);
            
            // Draw text label with squad info
            Handles.Label(transform.position + Vector3.up * 2, 
                $"{SquadName}\nType: {UnitType}\nUnits: {UnitCount}");
        }
    }
}