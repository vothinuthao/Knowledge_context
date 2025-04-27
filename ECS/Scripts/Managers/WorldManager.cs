using Components;
using Components.Combat;
using Components.Squad;
using Core.ECS;
using Factories;
using Systems.Behavior;
using Systems.Movement;
using Systems.Squad;
using Systems.Steering;
using UnityEngine;
using UnityEngine.Serialization;

namespace Managers
{
    public class WorldManager : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private GameObject _troopPrefab; // Add this field
        private TroopFactory _troopFactory;

        public World World { get; private set; }
        
        private void Awake()
        {
            InitializeWorld();
        }
        
        public void InitializeWorld()
        {
            World = new World();
            if (_troopPrefab)
                _troopFactory = new TroopFactory(World, _troopPrefab);
            
            World.RegisterSystem(new SquadCommandSystem());
            World.RegisterSystem(new SquadFormationSystem());
    
            World.RegisterSystem(new GridSquadMovementSystem());
            World.RegisterSystem(new MovementSystem());
            World.RegisterSystem(new RotationSystem());
    
            // Behavior & Steering
            World.RegisterSystem(new BehaviorSystem());
            World.RegisterSystem(new EntityDetectionSystem());
            World.RegisterSystem(new SteeringSystem());
            World.RegisterSystem(new SeekSystem());
            World.RegisterSystem(new SeparationSystem());
            World.RegisterSystem(new ArrivalSystem());
    
            Debug.Log($"World initialized with {World.GetRegisteredSystemCount()} systems");
        }
        
        private void Update()
        {
            if (World != null)
            {
                World.Update(Time.deltaTime);
            }
        }
        
        public Entity CreateSquad(SquadConfig config, Vector3 position)
        {
            Entity squadEntity = World.CreateEntity();
            squadEntity.AddComponent(new SquadComponent(config.MaxTroops));
            squadEntity.AddComponent(new PositionComponent(position));
            squadEntity.AddComponent(new RotationComponent());
            
            var squadComponent = squadEntity.GetComponent<SquadComponent>();
            squadComponent.Formation = config.DefaultFormation;
            squadComponent.MovementSpeed = config.MovementSpeed;
            squadComponent.RotationSpeed = config.RotationSpeed;
            squadComponent.CombatRange = config.CombatRange;
            
            return squadEntity;
        }
        
        public Entity CreateTroop(TroopConfig config, Vector3 position, int squadId = -1)
        {
            Entity troopEntity = _troopFactory?.CreateTroop(position, Quaternion.identity, TroopType.Warrior);
            if (troopEntity != null)
            {
                troopEntity.AddComponent(new TroopComponent(squadId, -1));
                if (config)
                {
                    troopEntity.AddComponent(new TroopComponent(squadId, -1));
                    troopEntity.AddComponent(new PositionComponent(position));
                    troopEntity.AddComponent(new VelocityComponent(config.MoveSpeed));
                    troopEntity.AddComponent(new HealthComponent(config.Health));
                    troopEntity.AddComponent(new CombatComponent(config.AttackPower, config.AttackRange, config.AttackCooldown));
                }
            }
            return troopEntity;
        }
    }
}