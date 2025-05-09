#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VikingRaven.Core.DI;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Game;
using VikingRaven.Units.Systems;

namespace Editor
{
    public class GameSetupWindow : EditorWindow
    {
        private GameObject _gameManagerObject;
        private GameObject _entityRegistryObject;
        private GameObject _systemRegistryObject;
        
        [MenuItem("Tools/Viking Raven Game/Setup Game")]
        public static void ShowWindow()
        {
            GetWindow<GameSetupWindow>("Game Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Tactical Game Setup", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create Core Objects"))
            {
                CreateCoreObjects();
            }
            
            if (GUILayout.Button("Create Systems"))
            {
                CreateSystems();
            }
            
            if (GUILayout.Button("Create Factories"))
            {
                CreateFactories();
            }
            
            if (GUILayout.Button("Setup Level Manager"))
            {
                SetupLevelManager();
            }
            
            if (GUILayout.Button("Complete Setup"))
            {
                CreateCoreObjects();
                CreateSystems();
                CreateFactories();
                SetupLevelManager();
            }
        }

        private void CreateCoreObjects()
        {
            // Create Entity Registry
            _entityRegistryObject = new GameObject("EntityRegistry");
            var entityRegistry = _entityRegistryObject.AddComponent<EntityRegistry>();
            
            // Create System Registry
            _systemRegistryObject = new GameObject("SystemRegistry");
            var systemRegistry = _systemRegistryObject.AddComponent<SystemRegistry>();
            
            
            // Create Game Bootstrapper
            var bootstrapperObject = new GameObject("GameBootstrapper");
            var bootstrapper = bootstrapperObject.AddComponent<GameBootstrapper>();
            
            
            
            // Create Game Manager
            _gameManagerObject = new GameObject("GameManager");
            var gameManager = _gameManagerObject.AddComponent<GameManager>();
            
            var serializedManager = new SerializedObject(gameManager);
            var managerEntityRegistryProp = serializedManager.FindProperty("_entityRegistry");
            var managerSystemRegistryProp = serializedManager.FindProperty("_systemRegistry");
            
            managerEntityRegistryProp.objectReferenceValue = entityRegistry;
            managerSystemRegistryProp.objectReferenceValue = systemRegistry;
            
            serializedManager.ApplyModifiedProperties();
            
            Debug.Log("Core objects created successfully");
        }

        private void CreateSystems()
        {
            if (_systemRegistryObject == null)
            {
                Debug.LogError("System Registry not found. Create core objects first.");
                return;
            }
            
            // Create Systems GameObject to hold all systems
            var systemsObject = new GameObject("Systems");
            
            // Create Unit Systems
            var stateSystem = systemsObject.AddComponent<StateManagementSystem>();
            var movementSystem = systemsObject.AddComponent<MovementSystem>();
            var combatSystem = systemsObject.AddComponent<CombatSystem>();
            var aiSystem = systemsObject.AddComponent<AIDecisionSystem>();
            var formationSystem = systemsObject.AddComponent<FormationSystem>();
            var aggroSystem = systemsObject.AddComponent<AggroDetectionSystem>();
            var animationSystem = systemsObject.AddComponent<AnimationSystem>();
            
            // Create Squad Systems
            var squadSystem = systemsObject.AddComponent<SquadCoordinationSystem>();
            
            // Create Combat Systems
            var tacticalSystem = systemsObject.AddComponent<TacticalAnalysisSystem>();
            var behaviorSystem = systemsObject.AddComponent<WeightedBehaviorSystem>();
            
            // Update Game Manager references
            if (_gameManagerObject != null)
            {
                var gameManager = _gameManagerObject.GetComponent<GameManager>();
                if (gameManager != null)
                {
                    var serializedManager = new SerializedObject(gameManager);
                    
                    serializedManager.FindProperty("_stateManagementSystem").objectReferenceValue = stateSystem;
                    serializedManager.FindProperty("_movementSystem").objectReferenceValue = movementSystem;
                    serializedManager.FindProperty("_combatSystem").objectReferenceValue = combatSystem;
                    serializedManager.FindProperty("_aiDecisionSystem").objectReferenceValue = aiSystem;
                    serializedManager.FindProperty("_formationSystem").objectReferenceValue = formationSystem;
                    serializedManager.FindProperty("_aggroDetectionSystem").objectReferenceValue = aggroSystem;
                    serializedManager.FindProperty("_animationSystem").objectReferenceValue = animationSystem;
                    serializedManager.FindProperty("_squadCoordinationSystem").objectReferenceValue = squadSystem;
                    serializedManager.FindProperty("_tacticalAnalysisSystem").objectReferenceValue = tacticalSystem;
                    serializedManager.FindProperty("_weightedBehaviorSystem").objectReferenceValue = behaviorSystem;
                    
                    serializedManager.ApplyModifiedProperties();
                }
            }
            
            Debug.Log("Systems created successfully");
        }

        private void CreateFactories()
        {
            // Create Factories GameObject to hold all factories
            var factoriesObject = new GameObject("Factories");
            
            // Create Unit Factory
            var unitFactoryObject = new GameObject("UnitFactory");
            unitFactoryObject.transform.parent = factoriesObject.transform;
            var unitFactory = unitFactoryObject.AddComponent<UnitFactory>();
            
            // Create Squad Factory
            var squadFactoryObject = new GameObject("SquadFactory");
            squadFactoryObject.transform.parent = factoriesObject.transform;
            var squadFactory = squadFactoryObject.AddComponent<SquadFactory>();
            
            // Update Game Manager references
            if (_gameManagerObject != null)
            {
                var gameManager = _gameManagerObject.GetComponent<GameManager>();
                if (gameManager != null)
                {
                    var serializedManager = new SerializedObject(gameManager);
                    
                    serializedManager.FindProperty("_unitFactory").objectReferenceValue = unitFactory;
                    serializedManager.FindProperty("_squadFactory").objectReferenceValue = squadFactory;
                    
                    serializedManager.ApplyModifiedProperties();
                }
            }
            
            Debug.Log("Factories created successfully");
        }

        private void SetupLevelManager()
        {
            // Create Level Manager
            var levelManagerObject = new GameObject("LevelManager");
            var levelManager = levelManagerObject.AddComponent<LevelManager>();
            
            // Create spawn points
            var spawnPointsObject = new GameObject("SpawnPoints");
            spawnPointsObject.transform.parent = levelManagerObject.transform;
            
            var playerSpawnPointsObject = new GameObject("PlayerSpawnPoints");
            playerSpawnPointsObject.transform.parent = spawnPointsObject.transform;
            
            var player1 = new GameObject("PlayerSpawn1");
            player1.transform.parent = playerSpawnPointsObject.transform;
            player1.transform.position = new Vector3(-10, 0, 0);
            
            var player2 = new GameObject("PlayerSpawn2");
            player2.transform.parent = playerSpawnPointsObject.transform;
            player2.transform.position = new Vector3(-10, 0, 10);
            
            var enemySpawnPointsObject = new GameObject("EnemySpawnPoints");
            enemySpawnPointsObject.transform.parent = spawnPointsObject.transform;
            
            var enemy1 = new GameObject("EnemySpawn1");
            enemy1.transform.parent = enemySpawnPointsObject.transform;
            enemy1.transform.position = new Vector3(10, 0, 0);
            
            var enemy2 = new GameObject("EnemySpawn2");
            enemy2.transform.parent = enemySpawnPointsObject.transform;
            enemy2.transform.position = new Vector3(10, 0, 10);
            
            // Set references in Level Manager
            var serializedLevelManager = new SerializedObject(levelManager);
            
            // Set GameManager reference
            if (_gameManagerObject != null)
            {
                var gameManager = _gameManagerObject.GetComponent<GameManager>();
                serializedLevelManager.FindProperty("_gameManager").objectReferenceValue = gameManager;
            }
            
            // Set spawn points
            var playerSpawns = new Transform[] { player1.transform, player2.transform };
            var enemySpawns = new Transform[] { enemy1.transform, enemy2.transform };
            
            serializedLevelManager.FindProperty("_playerSpawnPoints").arraySize = playerSpawns.Length;
            for (int i = 0; i < playerSpawns.Length; i++)
            {
                serializedLevelManager.FindProperty("_playerSpawnPoints").GetArrayElementAtIndex(i).objectReferenceValue = playerSpawns[i];
            }
            
            serializedLevelManager.FindProperty("_enemySpawnPoints").arraySize = enemySpawns.Length;
            for (int i = 0; i < enemySpawns.Length; i++)
            {
                serializedLevelManager.FindProperty("_enemySpawnPoints").GetArrayElementAtIndex(i).objectReferenceValue = enemySpawns[i];
            }
            
            serializedLevelManager.ApplyModifiedProperties();
            
            Debug.Log("Level Manager set up successfully");
        }
    }
}

#endif