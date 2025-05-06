using System;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Units.Components;
using VikingRaven.Units.Systems;

namespace VikingRaven.Core.DI
{
    public class DependencyInstaller : MonoBehaviour
    {
        [SerializeField] private EntityRegistry _entityRegistry;
        [SerializeField] private SystemRegistry _systemRegistry;
        
        // Factories
        [SerializeField] private UnitFactory _unitFactory;
        [SerializeField] private SquadFactory _squadFactory;
        
        // Systems
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
        public bool IsInitialized { get; private set; }
        
        
        
       private void Awake()
        {
            Debug.Log("DependencyInstaller.Awake() starting...");
            
            ValidateComponents();
            RegisterAllDependencies();
            if (!UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage())
            {
                Debug.Log("Injecting dependencies to scene...");
                DependencyContainer.Instance.InjectDependenciesInScene();
            }
            
            IsInitialized = true;
            Debug.Log("DependencyInstaller.Awake() completed");
        }
        
        private void ValidateComponents()
        {
            if (_entityRegistry == null) 
                _entityRegistry = FindObjectOfType<EntityRegistry>();
            if (_systemRegistry == null) 
                _systemRegistry = FindObjectOfType<SystemRegistry>();
            
            if (_unitFactory == null) 
                _unitFactory = FindObjectOfType<UnitFactory>();
            if (_squadFactory == null) 
                _squadFactory = FindObjectOfType<SquadFactory>();
            
            if (_entityRegistry == null)
                Debug.LogError("EntityRegistry is missing!");
            if (_systemRegistry == null)
                Debug.LogError("SystemRegistry is missing!");
            if (_unitFactory == null)
                Debug.LogError("UnitFactory is missing!");
            
            if (_squadFactory == null)
                Debug.LogWarning("SquadFactory is missing, some functionality will be limited");
        }
        
        private void RegisterAllDependencies()
        {
            Debug.Log($"Registering EntityRegistry: {(_entityRegistry != null ? "OK" : "NULL")}");
            DependencyContainer.Instance.Register<IEntityRegistry>(_entityRegistry);
            DependencyContainer.Instance.Register<EntityRegistry>(_entityRegistry);
            
            Debug.Log($"Registering SystemRegistry: {(_systemRegistry != null ? "OK" : "NULL")}");
            DependencyContainer.Instance.Register<ISystemRegistry>(_systemRegistry);
            DependencyContainer.Instance.Register<SystemRegistry>(_systemRegistry);
            
            // Đăng ký factories
            Debug.Log($"Registering UnitFactory: {(_unitFactory != null ? "OK" : "NULL")}");
            DependencyContainer.Instance.Register<UnitFactory>(_unitFactory);
            DependencyContainer.Instance.Register<IEntityFactory>(_unitFactory);
            
            if (_squadFactory != null)
            {
                Debug.Log("Registering SquadFactory: OK");
                DependencyContainer.Instance.Register<SquadFactory>(_squadFactory);
            }
            
            RegisterSystems();
        }
        
        private void RegisterSystems()
        {
            RegisterIfNotNull(_squadCoordinationSystem);
            RegisterIfNotNull(_stateManagementSystem);
            RegisterIfNotNull(_movementSystem);
            RegisterIfNotNull(_combatSystem);
            RegisterIfNotNull(_aiDecisionSystem);
            RegisterIfNotNull(_formationSystem);
            RegisterIfNotNull(_aggroDetectionSystem);
            RegisterIfNotNull(_animationSystem);
            RegisterIfNotNull(_tacticalAnalysisSystem);
            RegisterIfNotNull(_weightedBehaviorSystem);
            RegisterIfNotNull(_steeringSystem);
            RegisterIfNotNull(_specializedBehaviorSystem);
        }
        
        private void RegisterIfNotNull<T>(T system) where T : Component
        {
            if (system != null)
            {
                Debug.Log($"Registering {typeof(T).Name}: OK");
                DependencyContainer.Instance.Register<T>(system);
            }
            else
            {
                Debug.LogWarning($"{typeof(T).Name} is null, not registered");
            }
        }
    }
}