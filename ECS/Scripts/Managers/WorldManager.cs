using Components;
using Components.Combat;
using Components.Squad;
using Core.ECS;
using Core.Singleton;
using Factories;
using Systems.Behavior;
using Systems.Movement;
using Systems.Squad;
using Systems.Steering;
using UnityEngine;
using UnityEngine.Serialization;

namespace Managers
{
    public class WorldManager : ManualSingletonMono<WorldManager>
    {
        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private GameObject _troopPrefab;
        private TroopFactory _troopFactory;

        public World World { get; private set; }

        protected override void Awake()
        
        {
            base.Awake();
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
            AddComponentSafely(squadEntity,new SquadComponent(config.MaxTroops));
            AddComponentSafely(squadEntity,new PositionComponent(position));
            AddComponentSafely(squadEntity,new RotationComponent());
            
            var squadComponent = squadEntity.GetComponent<SquadComponent>();
            squadComponent.Formation = config.DefaultFormation;
            squadComponent.MovementSpeed = config.MovementSpeed;
            squadComponent.RotationSpeed = config.RotationSpeed;
            squadComponent.CombatRange = config.CombatRange;
            
            return squadEntity;
        }
        
        public Entity CreateTroop(TroopConfig config, Vector3 position, int squadId = -1)
        {
            if (_troopFactory == null)
            {
                Debug.LogError("TroopFactory is null! Cannot create troop.");
                return null;
            }
            
            Entity troopEntity = _troopFactory?.CreateTroop(position, Quaternion.identity, TroopType.Warrior);
            if (troopEntity != null)
            {
                // troopEntity.AddComponent(new TroopComponent(squadId, -1));
                if (config)
                {
                    AddComponentSafely(troopEntity,new TroopComponent(squadId, -1));
                    AddComponentSafely(troopEntity,new PositionComponent(position));
                    AddComponentSafely(troopEntity,new VelocityComponent(config.MoveSpeed));
                    AddComponentSafely(troopEntity,new HealthComponent(config.Health));
                    AddComponentSafely(troopEntity,new CombatComponent(config.AttackPower, config.AttackRange, config.AttackCooldown));
                }
            }
            return troopEntity;
        }
        private void AddComponentSafely<T>(Entity entity, T component) where T : IComponent
        {
            if (entity == null)
            {
                Debug.LogError($"Cannot add {typeof(T).Name} to null entity");
                return;
            }
            if (entity.HasComponent<T>())
            {
                Debug.LogWarning($"Entity {entity.Id} already has component of type {typeof(T).Name}. Skipping.");
                return;
            }
            
            entity.AddComponent(component);
        }
    }
}