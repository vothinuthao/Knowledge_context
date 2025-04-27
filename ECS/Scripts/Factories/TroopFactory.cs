using Components;
using Components.Steering;
using Core.ECS;
using Movement;
using Squad;
using Steering;
using UnityEngine;

namespace Factories
{
    /// <summary>
    /// Factory for creating troop entities
    /// </summary>
    public class TroopFactory
    {
        private World _world;
        private GameObject _troopPrefab;
        
        public TroopFactory(World world, GameObject troopPrefab)
        {
            _world = world;
            _troopPrefab = troopPrefab;
        }
        
        /// <summary>
        /// Create a troop entity
        /// </summary>
        public Entity CreateTroop(Vector3 position, Quaternion rotation, TroopType troopType)
        {
            Entity entity = _world.CreateEntity();
            entity.AddComponent(new PositionComponent(position));
            entity.AddComponent(new RotationComponent(rotation, 10f));
            entity.AddComponent(new VelocityComponent(GetMoveSpeedForType(troopType)));
            entity.AddComponent(new AccelerationComponent());
            entity.AddComponent(new SteeringDataComponent());
            AddBehaviorsForType(entity, troopType);
            GameObject troopObject = Object.Instantiate(_troopPrefab, position, rotation);
            troopObject.name = $"Troop_{troopType}_{entity.Id}";
            var entityBehaviour = troopObject.GetComponent<EntityBehaviour>();
            entityBehaviour.Initialize(entity, _world);
            
            return entity;
        }
        
        /// <summary>
        /// Add a troop to a squad
        /// </summary>
        public void AddTroopToSquad(Entity troopEntity, Entity squadEntity)
        {
            // Check if squad has formation component
            if (!squadEntity.HasComponent<SquadFormationComponent>())
            {
                return;
            }
            
            var formation = squadEntity.GetComponent<SquadFormationComponent>();
            
            // Find empty position
            Vector2Int gridPosition = formation.FindEmptyPosition();
            
            // Skip if no empty position
            if (gridPosition.x < 0 || gridPosition.y < 0)
            {
                return;
            }
            
            // Mark position as occupied
            formation.SetPositionOccupied(gridPosition.x, gridPosition.y, true);
            
            // Add squad member component
            troopEntity.AddComponent(new SquadMemberComponent(squadEntity.Id, gridPosition));
            
            // Calculate desired position
            Vector3 desiredPosition = Vector3.zero;
            
            if (squadEntity.HasComponent<PositionComponent>() && squadEntity.HasComponent<RotationComponent>())
            {
                Vector3 squadPosition = squadEntity.GetComponent<PositionComponent>().Position;
                Quaternion squadRotation = squadEntity.GetComponent<RotationComponent>().Rotation;
                
                Vector3 localPosition = formation.CalculateLocalPosition(gridPosition.x, gridPosition.y);
                desiredPosition = squadPosition + squadRotation * localPosition;
            }
            
            // Set desired position
            if (troopEntity.HasComponent<SquadMemberComponent>())
            {
                troopEntity.GetComponent<SquadMemberComponent>().DesiredPosition = desiredPosition;
            }
            
            // Set initial position
            if (troopEntity.HasComponent<PositionComponent>())
            {
                troopEntity.GetComponent<PositionComponent>().Position = desiredPosition;
            }
            
            // Set target position for steering
            if (troopEntity.HasComponent<SteeringDataComponent>())
            {
                troopEntity.GetComponent<SteeringDataComponent>().TargetPosition = desiredPosition;
            }
        }
        
        /// <summary>
        /// Get move speed for troop type
        /// </summary>
        private float GetMoveSpeedForType(TroopType troopType)
        {
            switch (troopType)
            {
                case TroopType.Scout:
                    return 4.5f;
                case TroopType.Berserker:
                case TroopType.Assassin:
                    return 4.0f;
                case TroopType.Warrior:
                    return 3.5f;
                case TroopType.Archer:
                case TroopType.Commander:
                    return 3.0f;
                case TroopType.HeavyInfantry:
                case TroopType.Defender:
                    return 2.5f;
                default:
                    return 3.0f;
            }
        }
        
        /// <summary>
        /// Add behavior components based on troop type
        /// </summary>
        private void AddBehaviorsForType(Entity entity, TroopType troopType)
        {
            // Add common behaviors
            entity.AddComponent(new SeekComponent(1.0f));
            entity.AddComponent(new SeparationComponent(2.0f, 2.0f));
        }
    }
    
    public enum TroopType
    {
        Warrior,
        HeavyInfantry,
        Berserker,
        Archer,
        Scout,
        Commander,
        Defender,
        Assassin
    }
}