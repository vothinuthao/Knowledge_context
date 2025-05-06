using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using VikingRaven.Combat.Components;
using VikingRaven.Core.DI;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Events;
using VikingRaven.Core.Factory;
using VikingRaven.Feedback.Systems;
using VikingRaven.Game.Examples;
using VikingRaven.SystemDebugger_Tool;
using VikingRaven.Units.Components;
using VikingRaven.Units.Systems;

public class SceneSetup : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool _enableDebugTools = true;
        
        [Header("Scene References")]
        [SerializeField] private Transform _environmentParent;
        [SerializeField] private Transform _uiParent;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject _infantryPrefab;
        [SerializeField] private GameObject _archerPrefab;
        [SerializeField] private GameObject _pikePrefab;
        [SerializeField] private GameObject _terrainPrefab;
        [SerializeField] private GameObject _uiPrefab;
        [SerializeField] private GameObject _debugConsolePrefab;
        [SerializeField] private GameObject _healthBarPrefab;
        [SerializeField] private GameObject _stateIndicatorPrefab;
        [SerializeField] private GameObject _behaviorIndicatorPrefab;
        [SerializeField] private GameObject _tacticalRoleIndicatorPrefab;
        
        [Header("Demo Settings")]
        [SerializeField] private bool _spawnInitialUnits = true;
        [SerializeField] private int _infantryPerSquad = 4;
        [SerializeField] private int _archerPerSquad = 2;
        [SerializeField] private int _pikePerSquad = 2;
        [SerializeField] private Vector3[] _playerSpawnPositions;
        [SerializeField] private Vector3[] _enemySpawnPositions;
        
        // References to created objects
        private GameObject _terrain;
        private GameObject _ui;
        private GameObject _debugConsole;
        
        // References to Core components
        private EntityRegistry _entityRegistry;
        private SystemRegistry _systemRegistry;
        private DependencyContainer _dependencyContainer;
        private EventManager _eventManager;
        
        // References to Systems
        private StateManagementSystem _stateManagementSystem;
        private MovementSystem _movementSystem;
        private CombatSystem _combatSystem;
        private AIDecisionSystem _aiDecisionSystem;
        private FormationSystem _formationSystem;
        private AggroDetectionSystem _aggroDetectionSystem;
        private AnimationSystem _animationSystem;
        private SquadCoordinationSystem _squadCoordinationSystem;
        private TacticalAnalysisSystem _tacticalAnalysisSystem;
        private WeightedBehaviorSystem _weightedBehaviorSystem;
        private SteeringSystem _steeringSystem;
        private SpecializedBehaviorSystem _specializedBehaviorSystem;
        private TacticalDecisionSystem _tacticalDecisionSystem;
        private TacticalExecutionSystem _tacticalExecutionSystem;
        private FeedbackSystem _feedbackSystem;
        
        // References to Factories
        private UnitFactory _unitFactory;
        private SquadFactory _squadFactory;
        
        // Start is called before the first frame update
        private void Start()
        {
            SetupScene();
        }
        
        private void SetupScene()
        {
            // Create core components
            CreateCoreComponents();
            
            // Create systems
            CreateSystems();
            
            // Create factories
            CreateFactories();
            
            // Set up dependencies
            SetupDependencies();
            
            // Create environment
            CreateEnvironment();
            
            // Create UI
            CreateUI();
            
            // Create debug tools if enabled
            if (_enableDebugTools)
            {
                CreateDebugTools();
            }
            
            // Initialize all systems
            _systemRegistry.InitializeAllSystems();
            
            // Spawn initial units if enabled
            if (_spawnInitialUnits)
            {
                SpawnInitialUnits();
            }
            
            Debug.Log("Scene setup complete!");
        }
        
        private void CreateCoreComponents()
        {
            // Create entity registry
            var entityRegistryObject = new GameObject("EntityRegistry");
            _entityRegistry = entityRegistryObject.AddComponent<EntityRegistry>();
            
            // Create system registry
            var systemRegistryObject = new GameObject("SystemRegistry");
            _systemRegistry = systemRegistryObject.AddComponent<SystemRegistry>();
            
            // Create dependency container
            var dependencyContainerObject = new GameObject("DependencyContainer");
            _dependencyContainer = dependencyContainerObject.AddComponent<DependencyContainer>();
            DontDestroyOnLoad(dependencyContainerObject);
            
            // Create event manager
            var eventManagerObject = new GameObject("EventManager");
            _eventManager = eventManagerObject.AddComponent<EventManager>();
            DontDestroyOnLoad(eventManagerObject);
            
            Debug.Log("Core components created");
        }
        
        private void CreateSystems()
        {
            // Create a parent object for all systems
            var systemsObject = new GameObject("Systems");
            
            // Create Unit Systems
            _stateManagementSystem = CreateSystem<StateManagementSystem>("StateManagementSystem", systemsObject.transform);
            _movementSystem = CreateSystem<MovementSystem>("MovementSystem", systemsObject.transform);
            _combatSystem = CreateSystem<CombatSystem>("CombatSystem", systemsObject.transform);
            _aiDecisionSystem = CreateSystem<AIDecisionSystem>("AIDecisionSystem", systemsObject.transform);
            _formationSystem = CreateSystem<FormationSystem>("FormationSystem", systemsObject.transform);
            _aggroDetectionSystem = CreateSystem<AggroDetectionSystem>("AggroDetectionSystem", systemsObject.transform);
            _animationSystem = CreateSystem<AnimationSystem>("AnimationSystem", systemsObject.transform);
            _steeringSystem = CreateSystem<SteeringSystem>("SteeringSystem", systemsObject.transform);
            _specializedBehaviorSystem = CreateSystem<SpecializedBehaviorSystem>("SpecializedBehaviorSystem", systemsObject.transform);
            
            // Create Squad Systems
            _squadCoordinationSystem = CreateSystem<SquadCoordinationSystem>("SquadCoordinationSystem", systemsObject.transform);
            
            // Create Combat Systems
            _tacticalAnalysisSystem = CreateSystem<TacticalAnalysisSystem>("TacticalAnalysisSystem", systemsObject.transform);
            _weightedBehaviorSystem = CreateSystem<WeightedBehaviorSystem>("WeightedBehaviorSystem", systemsObject.transform);
            _tacticalDecisionSystem = CreateSystem<TacticalDecisionSystem>("TacticalDecisionSystem", systemsObject.transform);
            _tacticalExecutionSystem = CreateSystem<TacticalExecutionSystem>("TacticalExecutionSystem", systemsObject.transform);
            
            // Create Feedback Systems
            _feedbackSystem = CreateSystem<FeedbackSystem>("FeedbackSystem", systemsObject.transform);
            
            // Set prefabs for feedback system
            var field = _feedbackSystem.GetType().GetField("_healthBarPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(_feedbackSystem, _healthBarPrefab);
                
            field = _feedbackSystem.GetType().GetField("_stateIndicatorPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(_feedbackSystem, _stateIndicatorPrefab);
                
            field = _feedbackSystem.GetType().GetField("_behaviorIndicatorPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(_feedbackSystem, _behaviorIndicatorPrefab);
                
            field = _feedbackSystem.GetType().GetField("_tacticalRoleIndicatorPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(_feedbackSystem, _tacticalRoleIndicatorPrefab);
            
            // Register systems with system registry
            _systemRegistry.RegisterSystem(_stateManagementSystem);
            _systemRegistry.RegisterSystem(_movementSystem);
            _systemRegistry.RegisterSystem(_combatSystem);
            _systemRegistry.RegisterSystem(_aiDecisionSystem);
            _systemRegistry.RegisterSystem(_formationSystem);
            _systemRegistry.RegisterSystem(_aggroDetectionSystem);
            _systemRegistry.RegisterSystem(_animationSystem);
            _systemRegistry.RegisterSystem(_squadCoordinationSystem);
            _systemRegistry.RegisterSystem(_tacticalAnalysisSystem);
            _systemRegistry.RegisterSystem(_weightedBehaviorSystem);
            _systemRegistry.RegisterSystem(_steeringSystem);
            _systemRegistry.RegisterSystem(_specializedBehaviorSystem);
            _systemRegistry.RegisterSystem(_tacticalDecisionSystem);
            _systemRegistry.RegisterSystem(_tacticalExecutionSystem);
            _systemRegistry.RegisterSystem(_feedbackSystem);
            
            Debug.Log("Systems created and registered");
        }
        
        private T CreateSystem<T>(string name, Transform parent) where T : Component
        {
            var systemObject = new GameObject(name);
            systemObject.transform.SetParent(parent);
            return systemObject.AddComponent<T>();
        }
        
        private void CreateFactories()
        {
            // Create a parent object for all factories
            var factoriesObject = new GameObject("Factories");
            
            // Create unit factory
            var unitFactoryObject = new GameObject("UnitFactory");
            unitFactoryObject.transform.SetParent(factoriesObject.transform);
            _unitFactory = unitFactoryObject.AddComponent<UnitFactory>();
            
            // Set prefabs for unit factory
            var field = _unitFactory.GetType().GetField("_infantryPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(_unitFactory, _infantryPrefab);
                
            field = _unitFactory.GetType().GetField("_archerPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(_unitFactory, _archerPrefab);
                
            field = _unitFactory.GetType().GetField("_pikePrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(_unitFactory, _pikePrefab);
                
            // Create squad factory
            var squadFactoryObject = new GameObject("SquadFactory");
            squadFactoryObject.transform.SetParent(factoriesObject.transform);
            _squadFactory = squadFactoryObject.AddComponent<SquadFactory>();
            
            Debug.Log("Factories created");
        }
        
        private void SetupDependencies()
        {
            // Register core components
            _dependencyContainer.Register<IEntityRegistry>(_entityRegistry);
            _dependencyContainer.Register<ISystemRegistry>(_systemRegistry);
            _dependencyContainer.Register<EventManager>(_eventManager);
            
            // Register factories
            _dependencyContainer.Register<UnitFactory>(_unitFactory);
            _dependencyContainer.Register<SquadFactory>(_squadFactory);
            
            // Register systems
            _dependencyContainer.Register<SquadCoordinationSystem>(_squadCoordinationSystem);
            _dependencyContainer.Register<TacticalAnalysisSystem>(_tacticalAnalysisSystem);
            
            // Inject dependencies
            _dependencyContainer.InjectDependencies(_unitFactory);
            _dependencyContainer.InjectDependencies(_squadFactory);
            
            foreach (var system in GetAllSystems())
            {
                _dependencyContainer.InjectDependencies(system);
            }
            
            // Inject dependencies to all objects in the scene
            _dependencyContainer.InjectDependenciesInScene();
            
            Debug.Log("Dependencies set up");
        }
        
        private ISystem[] GetAllSystems()
        {
            return new ISystem[]
            {
                _stateManagementSystem,
                _movementSystem,
                _combatSystem,
                _aiDecisionSystem,
                _formationSystem,
                _aggroDetectionSystem,
                _animationSystem,
                _squadCoordinationSystem,
                _tacticalAnalysisSystem,
                _weightedBehaviorSystem,
                _steeringSystem,
                _specializedBehaviorSystem,
                _tacticalDecisionSystem,
                _tacticalExecutionSystem,
                _feedbackSystem
            };
        }
        
        private void CreateEnvironment()
        {
            // Create terrain
            if (_terrainPrefab != null)
            {
                _terrain = Instantiate(_terrainPrefab, Vector3.zero, Quaternion.identity);
                
                if (_environmentParent != null)
                {
                    _terrain.transform.SetParent(_environmentParent);
                }
            }
            else
            {
                // Create a simple plane
                var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.transform.localScale = new Vector3(10, 1, 10);
                
                if (_environmentParent != null)
                {
                    plane.transform.SetParent(_environmentParent);
                }
                
                // Add NavMesh to the plane
                plane.AddComponent<NavMeshSurface>().BuildNavMesh();
            }
            
            Debug.Log("Environment created");
        }
        
        private void CreateUI()
        {
            // Create UI
            if (_uiPrefab != null)
            {
                _ui = Instantiate(_uiPrefab);
                
                if (_uiParent != null)
                {
                    _ui.transform.SetParent(_uiParent);
                }
            }
            else
            {
                // Create a simple camera controller
                var mainCamera = Camera.main;
                
                if (mainCamera != null)
                {
                    mainCamera.transform.position = new Vector3(0, 10, -10);
                    mainCamera.transform.eulerAngles = new Vector3(45, 0, 0);
                    
                    var cameraController = mainCamera.gameObject.AddComponent<SimpleCameraController>();
                }
                
                // Create a simple squad controller
                var squadControllerObject = new GameObject("SquadController");
                var squadController = squadControllerObject.AddComponent<SimpleSquadController>();
                
                // Set references
                var field = squadController.GetType().GetField("_squadCoordinationSystem", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    field.SetValue(squadController, _squadCoordinationSystem);
                    
                field = squadController.GetType().GetField("_mainCamera", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    field.SetValue(squadController, Camera.main);
            }
            
            Debug.Log("UI created");
        }
        
        private void CreateDebugTools()
        {
            // Create debug console
            if (_debugConsolePrefab != null)
            {
                _debugConsole = Instantiate(_debugConsolePrefab);
            }
            else
            {
                var debugConsoleObject = new GameObject("DebugConsole");
                var debugConsole = debugConsoleObject.AddComponent<DebugConsole>();
                
                // Set entity registry
                var field = debugConsole.GetType().GetField("_entityRegistry", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    field.SetValue(debugConsole, _entityRegistry);
                
                _debugConsole = debugConsoleObject;
            }
            
            // Create entity visualizer
            var entityVisualizerObject = new GameObject("EntityVisualizer");
            var entityVisualizer = entityVisualizerObject.AddComponent<EntityVisualizer>();
            
            // Set entity registry
            var visualizerField = entityVisualizer.GetType().GetField("_entityRegistry", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (visualizerField != null)
                visualizerField.SetValue(entityVisualizer, _entityRegistry);
            
            Debug.Log("Debug tools created");
        }
        
        private void SpawnInitialUnits()
        {
            // Spawn player squads
            for (int i = 0; i < _playerSpawnPositions.Length; i++)
            {
                if (i == 0)
                {
                    // Create a mixed squad
                    Dictionary<UnitType, int> unitCounts = new Dictionary<UnitType, int>
                    {
                        { UnitType.Infantry, _infantryPerSquad },
                        { UnitType.Archer, _archerPerSquad },
                        { UnitType.Pike, _pikePerSquad }
                    };
                    
                    var playerSquad = _squadFactory.CreateMixedSquad(unitCounts, _playerSpawnPositions[i], Quaternion.identity);
                    
                    // Get squad ID from first entity
                    int squadId = -1;
                    if (playerSquad.Count > 0)
                    {
                        var formationComponent = playerSquad[0].GetComponent<FormationComponent>();
                        if (formationComponent != null)
                        {
                            squadId = formationComponent.SquadId;
                        }
                    }
                    
                    Debug.Log($"Spawned player squad {squadId} with {playerSquad.Count} units at {_playerSpawnPositions[i]}");
                }
                else
                {
                    // Create specialized squads
                    UnitType squadType = (i % 3 == 0) ? UnitType.Infantry : ((i % 3 == 1) ? UnitType.Archer : UnitType.Pike);
                    int count = (squadType == UnitType.Infantry) ? _infantryPerSquad : 
                                ((squadType == UnitType.Archer) ? _archerPerSquad : _pikePerSquad);
                    
                    var playerSquad = _squadFactory.CreateSquad(squadType, count, _playerSpawnPositions[i], Quaternion.identity);
                    
                    // Get squad ID from first entity
                    int squadId = -1;
                    if (playerSquad.Count > 0)
                    {
                        var formationComponent = playerSquad[0].GetComponent<FormationComponent>();
                        if (formationComponent != null)
                        {
                            squadId = formationComponent.SquadId;
                        }
                    }
                    
                    Debug.Log($"Spawned player squad {squadId} with {playerSquad.Count} {squadType} units at {_playerSpawnPositions[i]}");
                }
            }
            
            // Spawn enemy squads
            for (int i = 0; i < _enemySpawnPositions.Length; i++)
            {
                // Create specialized squads
                UnitType squadType = (i % 3 == 0) ? UnitType.Infantry : ((i % 3 == 1) ? UnitType.Archer : UnitType.Pike);
                int count = (squadType == UnitType.Infantry) ? _infantryPerSquad : 
                            ((squadType == UnitType.Archer) ? _archerPerSquad : _pikePerSquad);
                
                var enemySquad = _squadFactory.CreateSquad(squadType, count, _enemySpawnPositions[i], Quaternion.identity);
                
                // Get squad ID from first entity
                int squadId = -1;
                if (enemySquad.Count > 0)
                {
                    var formationComponent = enemySquad[0].GetComponent<FormationComponent>();
                    if (formationComponent != null)
                    {
                        squadId = formationComponent.SquadId;
                    }
                }
                
                Debug.Log($"Spawned enemy squad {squadId} with {enemySquad.Count} {squadType} units at {_enemySpawnPositions[i]}");
            }
            
            Debug.Log("Initial units spawned");
        }
    }