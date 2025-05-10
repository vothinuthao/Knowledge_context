using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using VikingRaven.Core.ECS;
using VikingRaven.Game;
using VikingRaven.Units.Systems;
using VikingRaven.Core;
using VikingRaven.Core.Factory;
using VikingRaven.Units.Components;

namespace VikingRaven.Editor
{
    /// <summary>
    /// Cửa sổ thiết lập Scene - tự động tạo và thiết lập cảnh với các thành phần cần thiết
    /// </summary>
    public class SceneSetupWindow : OdinEditorWindow
    {
        [MenuItem("VikingRaven/Công cụ/Thiết lập Scene")]
        private static void OpenWindow()
        {
            GetWindow<SceneSetupWindow>().Show();
        }

        [TitleGroup("Thông tin cơ bản")]
        [LabelText("Tên Scene"), LabelWidth(150)]
        public string sceneName = "VikingRaven_Scene";

        [TitleGroup("Thông tin cơ bản")]
        [LabelText("Lưu đường dẫn"), LabelWidth(150)]
        public bool saveScene = true;

        [TitleGroup("Thông tin cơ bản")]
        [LabelText("Đường dẫn lưu"), ShowIf("saveScene"), LabelWidth(150)]
        [FolderPath]
        public string savePath = "Assets/Scenes";

        [TitleGroup("Cấu hình Core Systems")]
        [LabelText("Tạo GameBootstrapper"), LabelWidth(200)]
        public bool createGameBootstrapper = true;

        [TitleGroup("Cấu hình Core Systems")]
        [LabelText("Tạo EntityRegistry"), LabelWidth(200)]
        public bool createEntityRegistry = true;

        [TitleGroup("Cấu hình Core Systems")]
        [LabelText("Tạo SystemRegistry"), LabelWidth(200)]
        public bool createSystemRegistry = true;

        [TitleGroup("Cấu hình Core Systems")]
        [LabelText("Tạo GameManager"), LabelWidth(200)]
        public bool createGameManager = true;

        [TitleGroup("Cấu hình Core Systems")]
        [LabelText("Tạo LevelManager"), LabelWidth(200)]
        public bool createLevelManager = true;

        [TitleGroup("Cấu hình Game Systems")]
        [TableList(ShowIndexLabels = true), LabelText("Danh sách Game Systems")]
        public List<GameSystemInfo> gameSystems = new List<GameSystemInfo>();

        [Serializable]
        public class GameSystemInfo
        {
            [TableColumnWidth(150)]
            public string Name;
            
            [TableColumnWidth(250)]
            [GUIColor("GetSystemColor")]
            [LabelText("Loại System")]
            [ValueDropdown("GetSystemTypes")]
            public string SystemType;
            
            [TableColumnWidth(80)]
            public bool Create;
            
            [HideInInspector]
            public System.Type ActualType;

            public Color GetSystemColor()
            {
                if (SystemType.Contains("Movement")) return Color.green;
                if (SystemType.Contains("Combat")) return new Color(1, 0.5f, 0.5f);
                if (SystemType.Contains("AI")) return new Color(0.5f, 0.5f, 1);
                if (SystemType.Contains("Formation")) return new Color(1, 0.7f, 0.3f);
                if (SystemType.Contains("Tactical")) return new Color(0.7f, 0.3f, 1);
                return Color.white;
            }

            public IEnumerable<string> GetSystemTypes()
            {
                var baseSystemType = typeof(BaseSystem);
                var systemTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => baseSystemType.IsAssignableFrom(t) && !t.IsAbstract && t != baseSystemType)
                    .Select(t => t.Name)
                    .OrderBy(n => n);
                
                return systemTypes;
            }
        }

        [TitleGroup("Thiết lập Terrain")]
        [LabelText("Tạo Terrain"), LabelWidth(200)]
        public bool createTerrain = true;

        [TitleGroup("Thiết lập Terrain")]
        [LabelText("Kích thước Terrain"), ShowIf("createTerrain"), LabelWidth(200)]
        [MinValue(100), MaxValue(2000)]
        public Vector3 terrainSize = new Vector3(500, 100, 500);

        [TitleGroup("Thiết lập Spawn Points")]
        [LabelText("Số lượng điểm hồi sinh"), LabelWidth(200)]
        [MinValue(0), MaxValue(10)]
        public int spawnPointCount = 2;

        [TitleGroup("Thiết lập Debug Tools")]
        [LabelText("Tạo công cụ debug Formation"), LabelWidth(250)]
        public bool createFormationDebugger = true;

        [TitleGroup("Thiết lập Debug Tools")]
        [LabelText("Tạo công cụ debug FormationSlot"), LabelWidth(250)]
        public bool createFormationSlotVisualizer = true;

        [TitleGroup("Thiết lập Debug Tools")]
        [LabelText("Tạo công cụ phân tích Formation"), LabelWidth(250)]
        public bool createFormationAnalyzer = true;

        [Button("Thiết lập Scene", ButtonSizes.Large)]
        [GUIColor(0, 0.8f, 0)]
        private void SetupScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Không thể thực hiện", "Không thể thiết lập scene khi game đang chạy. Vui lòng dừng game và thử lại.", "OK");
                return;
            }

            // Tạo scene mới
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

            // Core Systems
            if (createGameBootstrapper) CreateGameBootstrapper();
            if (createEntityRegistry) CreateEntityRegistry();
            if (createSystemRegistry) CreateSystemRegistry();
            if (createGameManager) CreateGameManager();
            if (createLevelManager) CreateLevelManager();

            // Game Systems
            CreateSelectedGameSystems();

            // Terrain
            if (createTerrain) CreateTerrain();

            // Spawn Points
            CreateSpawnPoints();

            // Debug Tools
            CreateDebugTools();

            // Lưu scene
            if (saveScene)
            {
                string scenePath = $"{savePath}/{sceneName}.unity";
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
                Debug.Log($"Scene đã được lưu tại: {scenePath}");
            }

            EditorUtility.DisplayDialog("Thiết lập hoàn tất", "Scene đã được thiết lập thành công với các thành phần được chọn.", "OK");
        }

        private void CreateGameBootstrapper()
        {
            GameObject bootstrapper = new GameObject("GameBootstrapper");
            bootstrapper.AddComponent<GameBootstrapper>();
            Debug.Log("Đã tạo GameBootstrapper");
        }

        private void CreateEntityRegistry()
        {
            GameObject entityRegistry = new GameObject("EntityRegistry");
            entityRegistry.AddComponent<EntityRegistry>();
            Debug.Log("Đã tạo EntityRegistry");
        }

        private void CreateSystemRegistry()
        {
            GameObject systemRegistry = new GameObject("SystemRegistry");
            systemRegistry.AddComponent<SystemRegistry>();
            Debug.Log("Đã tạo SystemRegistry");
        }

        private void CreateGameManager()
        {
            GameObject gameManager = new GameObject("GameManager");
            GameManager manager = gameManager.AddComponent<GameManager>();
            
            // Tạo UnitFactory
            GameObject unitFactoryObj = new GameObject("UnitFactory");
            unitFactoryObj.transform.SetParent(gameManager.transform);
            UnitFactory unitFactory = unitFactoryObj.AddComponent<UnitFactory>();
            
            // Gán UnitFactory
            var fieldInfo = typeof(GameManager).GetField("_unitFactory", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(manager, unitFactory);
            }
            
            Debug.Log("Đã tạo GameManager");
        }

        private void CreateLevelManager()
        {
            GameObject levelManager = new GameObject("LevelManager");
            levelManager.AddComponent<LevelManager>();
            Debug.Log("Đã tạo LevelManager");
        }

        private void CreateSelectedGameSystems()
        {
            GameObject systemsContainer = new GameObject("Game_Systems");
            
            foreach (var systemInfo in gameSystems)
            {
                if (systemInfo.Create && !string.IsNullOrEmpty(systemInfo.SystemType))
                {
                    // Tìm type hệ thống
                    var baseSystemType = typeof(BaseSystem);
                    var systemType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.Name == systemInfo.SystemType && baseSystemType.IsAssignableFrom(t));

                    if (systemType != null)
                    {
                        GameObject systemObj = new GameObject(systemInfo.Name ?? systemInfo.SystemType);
                        systemObj.transform.SetParent(systemsContainer.transform);
                        
                        Component system = systemObj.AddComponent(systemType);
                        Debug.Log($"Đã tạo {systemInfo.SystemType}");
                    }
                }
            }
        }

        private void CreateTerrain()
        {
            GameObject terrainObj = new GameObject("Terrain");
            Terrain terrain = terrainObj.AddComponent<Terrain>();
            terrainObj.AddComponent<TerrainCollider>();
            
            TerrainData terrainData = new TerrainData();
            terrainData.size = terrainSize;
            
            terrain.terrainData = terrainData;
            
            // Đặt terrain ở vị trí phù hợp
            terrainObj.transform.position = new Vector3(-terrainSize.x / 2, 0, -terrainSize.z / 2);
            
            // Lưu terrainData
            if (saveScene)
            {
                string terrainDataPath = $"{savePath}/TerrainData_{sceneName}.asset";
                AssetDatabase.CreateAsset(terrainData, terrainDataPath);
                AssetDatabase.SaveAssets();
            }
            
            Debug.Log("Đã tạo Terrain");
        }

        private void CreateSpawnPoints()
        {
            if (spawnPointCount <= 0) return;
            
            GameObject spawnPointsContainer = new GameObject("SpawnPoints");
            GameObject playerSpawnsContainer = new GameObject("PlayerSpawnPoints");
            GameObject enemySpawnsContainer = new GameObject("EnemySpawnPoints");
            
            playerSpawnsContainer.transform.SetParent(spawnPointsContainer.transform);
            enemySpawnsContainer.transform.SetParent(spawnPointsContainer.transform);
            
            // Tạo spawn points cho player
            for (int i = 0; i < spawnPointCount; i++)
            {
                GameObject spawnPoint = new GameObject($"PlayerSpawn_{i}");
                spawnPoint.transform.SetParent(playerSpawnsContainer.transform);
                spawnPoint.transform.position = new Vector3(i * 10, 0, 0);
            }
            
            // Tạo spawn points cho enemy
            for (int i = 0; i < spawnPointCount; i++)
            {
                GameObject spawnPoint = new GameObject($"EnemySpawn_{i}");
                spawnPoint.transform.SetParent(enemySpawnsContainer.transform);
                spawnPoint.transform.position = new Vector3(i * 10, 0, 50);
            }
            
            Debug.Log($"Đã tạo {spawnPointCount} điểm hồi sinh cho player và enemy");
        }

        private void CreateDebugTools()
        {
            GameObject debugContainer = new GameObject("DebugTools");
            
            if (createFormationDebugger)
            {
                GameObject debuggerObj = new GameObject("FormationDebugger");
                debuggerObj.transform.SetParent(debugContainer.transform);
                debuggerObj.AddComponent<VikingRaven.Debug_Game.FormationDebugger>();
                Debug.Log("Đã tạo FormationDebugger");
            }
            
            if (createFormationSlotVisualizer)
            {
                GameObject visualizerObj = new GameObject("FormationSlotVisualizer");
                visualizerObj.transform.SetParent(debugContainer.transform);
                visualizerObj.AddComponent<VikingRaven.Debug_Game.FormationSlotVisualizer>();
                Debug.Log("Đã tạo FormationSlotVisualizer");
            }
            
            if (createFormationAnalyzer)
            {
                GameObject analyzerObj = new GameObject("FormationAnalyzer");
                analyzerObj.transform.SetParent(debugContainer.transform);
                
                // Tạo Canvas UI cho FormationAnalyzer
                GameObject canvasObj = new GameObject("Canvas");
                canvasObj.transform.SetParent(analyzerObj.transform);
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                // Tạo Text output
                GameObject textObj = new GameObject("AnalyzerOutput");
                textObj.transform.SetParent(canvasObj.transform);
                UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 14;
                text.color = Color.white;
                
                // Thiết lập RectTransform
                RectTransform rectTransform = textObj.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(0.3f, 1);
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.offsetMin = new Vector2(10, 10);
                rectTransform.offsetMax = new Vector2(-10, -10);
                
                // Add FormationAnalyzer và thiết lập
                VikingRaven.Debug_Game.FormationAnalyzer analyzer = analyzerObj.AddComponent<VikingRaven.Debug_Game.FormationAnalyzer>();
                analyzer._uiOutput = text;
                
                Debug.Log("Đã tạo FormationAnalyzer");
            }
        }

        [OnInspectorInit]
        private void Initialize()
        {
            if (gameSystems.Count == 0)
            {
                // Thiết lập các system mặc định
                gameSystems = new List<GameSystemInfo>
                {
                    new GameSystemInfo { Name = "MovementSystem", SystemType = "MovementSystem", Create = true },
                    new GameSystemInfo { Name = "FormationSystem", SystemType = "FormationSystem", Create = true },
                    new GameSystemInfo { Name = "SquadCoordinationSystem", SystemType = "SquadCoordinationSystem", Create = true },
                    new GameSystemInfo { Name = "AggroDetectionSystem", SystemType = "AggroDetectionSystem", Create = true },
                    new GameSystemInfo { Name = "AIDecisionSystem", SystemType = "AIDecisionSystem", Create = true },
                    new GameSystemInfo { Name = "SteeringSystem", SystemType = "SteeringSystem", Create = true },
                    new GameSystemInfo { Name = "WeightedBehaviorSystem", SystemType = "WeightedBehaviorSystem", Create = true },
                    new GameSystemInfo { Name = "TacticalAnalysisSystem", SystemType = "TacticalAnalysisSystem", Create = true },
                    new GameSystemInfo { Name = "SpecializedBehaviorSystem", SystemType = "SpecializedBehaviorSystem", Create = true },
                };
            }
        }
    }
}