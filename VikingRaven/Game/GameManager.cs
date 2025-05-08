using System;
using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.DI;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Units.Components;
using VikingRaven.Units.Systems;

namespace VikingRaven.Game
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private EntityRegistry _entityRegistry;
        [SerializeField] private SystemRegistry _systemRegistry;
        [SerializeField] private UnitFactory _unitFactory;
        [SerializeField] private SquadFactory _squadFactory;
        
        // Systems
        [SerializeField] private StateManagementSystem _stateManagementSystem;
        [SerializeField] private MovementSystem _movementSystem;
        [SerializeField] private CombatSystem _combatSystem;
        [SerializeField] private AIDecisionSystem _aiDecisionSystem;
        [SerializeField] private FormationSystem _formationSystem;
        [SerializeField] private AggroDetectionSystem _aggroDetectionSystem;
        [SerializeField] private AnimationSystem _animationSystem;
        [SerializeField] private SquadCoordinationSystem _squadCoordinationSystem;
        [SerializeField] private TacticalAnalysisSystem _tacticalAnalysisSystem;
        [SerializeField] private WeightedBehaviorSystem _weightedBehaviorSystem;
        [SerializeField]
        private DependencyInstaller _dependencyInstaller;
        [SerializeField]
        private GameBootstrapper _gameBootstrapper;

        private void Awake()
        {
            // Create dependency installer if not already exists
            if (_dependencyInstaller == null)
            {
                GameObject installerObject = new GameObject("DependencyInstaller");
                _dependencyInstaller = installerObject.AddComponent<DependencyInstaller>();
            }
            
            // Create game bootstrapper if not already exists
            if (_gameBootstrapper == null)
            {
                GameObject bootstrapperObject = new GameObject("GameBootstrapper");
                _gameBootstrapper = bootstrapperObject.AddComponent<GameBootstrapper>();
            }
            
            RegisterSystems();
        }

        private void RegisterSystems()
        {
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
        }
        
        public void CreateSquad(UnitType unitType, int count, Vector3 position)
        {
            _squadFactory.CreateSquad(unitType, count, position, Quaternion.identity);
        }

        // Example method to create a mixed squad
        public void CreateMixedSquad(Vector3 position)
        {
            Dictionary<UnitType, int> unitCounts = new Dictionary<UnitType, int>
            {
                { UnitType.Infantry, 4 },
                { UnitType.Archer, 2 },
                { UnitType.Pike, 2 }
            };
            
            _squadFactory.CreateMixedSquad(unitCounts, position, Quaternion.identity);
        }
    }
}