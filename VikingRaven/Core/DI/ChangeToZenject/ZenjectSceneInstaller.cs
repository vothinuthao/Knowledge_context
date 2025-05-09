using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Game;
using VikingRaven.Units.Components;
using VikingRaven.Units.Systems;
using Zenject;

namespace VikingRaven.Core.DI
{
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
            
            Debug.Log("ZenjectSceneInstaller: Bindings installed successfully");
        }

        /// <summary>
        /// Register core registries
        /// </summary>
        private void InstallRegistries()
        {
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
            // Bind factories
            if (_unitFactory != null)
            {
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
        }

        /// <summary>
        /// Register all systems
        /// </summary>
        private void InstallSystems()
        {
            // Bind systems
            BindIfNotNull(_squadCoordinationSystem);
            BindIfNotNull(_stateManagementSystem);
            BindIfNotNull(_movementSystem);
            BindIfNotNull(_combatSystem);
            BindIfNotNull(_aiDecisionSystem);
            BindIfNotNull(_formationSystem);
            BindIfNotNull(_aggroDetectionSystem);
            BindIfNotNull(_animationSystem);
            BindIfNotNull(_tacticalAnalysisSystem);
            BindIfNotNull(_weightedBehaviorSystem);
            BindIfNotNull(_steeringSystem);
            BindIfNotNull(_specializedBehaviorSystem);
        }

        /// <summary>
        /// Register game managers
        /// </summary>
        private void InstallManagers()
        {
            if (_gameManager != null)
            {
                Container.Bind<GameManager>().FromInstance(_gameManager).AsSingle();
                Debug.Log("Registered GameManager");
            }
            else
            {
                Debug.LogWarning("GameManager is missing");
            }
        }

        /// <summary>
        /// Helper method to bind a component if it's not null
        /// </summary>
        private void BindIfNotNull<T>(T system) where T : Component
        {
            if (system != null)
            {
                Container.Bind<T>().FromInstance(system).AsSingle();
                Debug.Log($"Registered {typeof(T).Name}");
            }
            else
            {
                Debug.LogWarning($"{typeof(T).Name} is null, not registered");
            }
        }
    }
}