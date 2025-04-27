using Components;
using Components.Combat;
using Components.Squad;
using Core.ECS;
using Data;
using Systems.Behavior;
using Systems.Movement;
using Systems.Squad;
using Systems.Steering;
using UnityEngine;

namespace Managers
{
    public partial class WorldManager : MonoBehaviour
    {
        [SerializeField] private GameConfig _gameConfig;
        
        public World World { get; private set; }
        
        private void Awake()
        {
            InitializeWorld();
        }
        
        public void InitializeWorld()
        {
            // World = new World();
            // World.RegisterSystem(new GridSquadMovementSystem());
            // World.RegisterSystem(new BehaviorSystem());
            // World.RegisterSystem(new SquadCommandSystem());
            // Debug.Log($"World initialized with {World.GetRegisteredSystemCount()} systems");
            World = new World();
    
            // Squad systems
            World.RegisterSystem(new SquadCommandSystem());
            World.RegisterSystem(new SquadFormationSystem());
    
            // Movement systems
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
            Entity troopEntity = World.CreateEntity();
            
            troopEntity.AddComponent(new TroopComponent(squadId, -1));
            troopEntity.AddComponent(new PositionComponent(position));
            troopEntity.AddComponent(new VelocityComponent(config.MoveSpeed));
            troopEntity.AddComponent(new HealthComponent(config.Health));
            troopEntity.AddComponent(new CombatComponent(config.AttackPower, config.AttackRange, config.AttackCooldown));
            
            return troopEntity;
        }
    }
}