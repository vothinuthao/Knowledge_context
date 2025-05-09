// ZenjectSceneInstaller.cs - Phiên bản đã cập nhật
using UnityEngine;
using Zenject;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Game;
using VikingRaven.Units.Components;
using VikingRaven.Units.Systems;

namespace VikingRaven.Core.DI
{
    /// <summary>
    /// Main installer for registering all dependencies in the scene
    /// This replaces the old DependencyInstaller class
    /// </summary>
    public class ZenjectSceneInstaller : MonoInstaller
    {
        [Header("Core Registries")]
        [SerializeField] private EntityRegistry _entityRegistry;
        [SerializeField] private SystemRegistry _systemRegistry;
        
        [Header("Factories")]
        [SerializeField] private UnitFactory _unitFactory;
        [SerializeField] private SquadFactory _squadFactory;
        
        [Header("Systems")]
        [SerializeField] private SquadCoordinationSystem _squadCoordinationSystem;
        [SerializeField] private StateManagementSystem _stateManagementSystem; 
        [SerializeField] private MovementSystem _movementSystem;
        [SerializeField] private CombatSystem _combatSystem;
        [SerializeField] private AIDecisionSystem _aiDecisionSystem;
        [SerializeField] private FormationSystem _formationSystem;
        [SerializeField] private AggroDetectionSystem _aggroDetectionSystem;
        [SerializeField] private AnimationSystem _animationSystem;
        [SerializeField] private TacticalAnalysisSystem _tacticalAnalysisSystem;
        [SerializeField] private WeightedBehaviorSystem _weightedBehaviorSystem;
        [SerializeField] private SteeringSystem _steeringSystem;
        [SerializeField] private SpecializedBehaviorSystem _specializedBehaviorSystem;

        [Header("Game Management")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private LevelManager _levelManager;

        /// <summary>
        /// This method is called by Zenject to install all bindings
        /// It replaces the RegisterAllDependencies method from the old DependencyInstaller
        /// </summary>
        public override void InstallBindings()
        {
            Debug.Log("ZenjectSceneInstaller: Installing bindings...");
            
            InstallRegistries();
            InstallFactories();
            InstallSystems();
            InstallManagers();
            
            // Debug helper - nếu đã tạo class ZenjectDebugHelper
            // ZenjectDebugHelper.LogBindings(Container);
            
            // Hoặc log thủ công
            Debug.Log("ZenjectSceneInstaller: All bindings installed successfully");
        }

        /// <summary>
        /// Register core registries
        /// </summary>
        private void InstallRegistries()
        {
            Debug.Log("Installing Registries...");
            
            // Bind registries
            if (_entityRegistry != null)
            {
                Container.Bind<EntityRegistry>().FromInstance(_entityRegistry).AsSingle();
                Container.Bind<IEntityRegistry>().FromInstance(_entityRegistry).AsSingle();
                Debug.Log("Registered EntityRegistry");
            }
            else
            {
                Debug.LogError("EntityRegistry is missing!");
            }

            if (_systemRegistry != null)
            {
                Container.Bind<SystemRegistry>().FromInstance(_systemRegistry).AsSingle();
                Container.Bind<ISystemRegistry>().FromInstance(_systemRegistry).AsSingle();
                Debug.Log("Registered SystemRegistry");
            }
            else
            {
                Debug.LogError("SystemRegistry is missing!");
            }
        }

        /// <summary>
        /// Register factories
        /// </summary>
        private void InstallFactories()
        {
            Debug.Log("Installing Factories...");
            
            // Bind factories
            if (_unitFactory != null)
            {
                // Kiểm tra các prefab trong UnitFactory
                bool prefabsValid = _unitFactory.ValidatePrefabs();
                if (!prefabsValid)
                {
                    Debug.LogWarning("UnitFactory has missing prefabs. Check the inspector!");
                }
                
                Container.Bind<UnitFactory>().FromInstance(_unitFactory).AsSingle();
                Container.Bind<IEntityFactory>().FromInstance(_unitFactory).AsSingle();
                Debug.Log("Registered UnitFactory");
            }
            else
            {
                Debug.LogError("UnitFactory is missing!");
            }

            if (_squadFactory != null)
            {
                Container.Bind<SquadFactory>().FromInstance(_squadFactory).AsSingle();
                Debug.Log("Registered SquadFactory");
            }
            else
            {
                Debug.LogWarning("SquadFactory is missing, some functionality will be limited");
            }
            
            // Bind DiContainer để các factory có thể sử dụng
            Container.Bind<DiContainer>().FromInstance(Container).AsSingle();
            Debug.Log("Registered DiContainer for manual injection in factories");
        }

        /// <summary>
        /// Register all systems
        /// </summary>
        private void InstallSystems()
        {
            Debug.Log("Installing Systems...");
            
            // Bind systems - grouped by category for better error handling
            
            // Core systems
            BindIfNotNull(_stateManagementSystem, "State Management System");
            BindIfNotNull(_movementSystem, "Movement System");
            
            // Combat systems
            BindIfNotNull(_combatSystem, "Combat System");
            BindIfNotNull(_aggroDetectionSystem, "Aggro Detection System");
            
            // AI and decision making systems
            BindIfNotNull(_aiDecisionSystem, "AI Decision System");
            BindIfNotNull(_tacticalAnalysisSystem, "Tactical Analysis System");
            BindIfNotNull(_weightedBehaviorSystem, "Weighted Behavior System");
            
            // Formation and coordination systems
            BindIfNotNull(_squadCoordinationSystem, "Squad Coordination System");
            BindIfNotNull(_formationSystem, "Formation System");
            
            // Animation and visual systems
            BindIfNotNull(_animationSystem, "Animation System");
            
            // Steering and movement systems
            BindIfNotNull(_steeringSystem, "Steering System");
            BindIfNotNull(_specializedBehaviorSystem, "Specialized Behavior System");
        }

        /// <summary>
        /// Register game managers
        /// </summary>
        private void InstallManagers()
        {
            Debug.Log("Installing Managers...");
            
            if (_gameManager != null)
            {
                Container.Bind<GameManager>().FromInstance(_gameManager).AsSingle();
                Debug.Log("Registered GameManager");
            }
            else
            {
                Debug.LogWarning("GameManager is missing");
            }
            
            if (_levelManager != null)
            {
                Container.Bind<LevelManager>().FromInstance(_levelManager).AsSingle();
                Debug.Log("Registered LevelManager");
            }
            else
            {
                Debug.LogWarning("LevelManager is missing");
            }
        }

        /// <summary>
        /// Helper method to bind a component if it's not null
        /// </summary>
        private void BindIfNotNull<T>(T system, string systemName = null) where T : Component
        {
            if (system != null)
            {
                Container.Bind<T>().FromInstance(system).AsSingle();
                Debug.Log($"Registered {systemName ?? typeof(T).Name}");
            }
            else
            {
                Debug.LogWarning($"{systemName ?? typeof(T).Name} is missing, not registered");
            }
        }
    }
}