// Complete behavior system implementation
using System;
using System.Collections.Generic;
using System.Linq;
using Behaviors.CoreBehaviors;
using UnityEngine;
using Core.ECS;
using Components.Squad;
using Core.Performance;
using Movement;
using Systems.CoreBehaviors;

namespace Systems.Behavior
{
    /// <summary>
    /// Priority order for behaviors
    /// </summary>
    public enum BehaviorPriority
    {
        FORMATION_KEEP = 100,    // Always maintain formation
        SEEK = 90,              // Move to target position
        SEPARATION = 80,         // Avoid collision with allies
        COMBAT = 70,            // Attack/defend behavior
        SPECIAL = 60            // Special behaviors (charge, retreat)
    }
    
    /// <summary>
    /// Context provided to behaviors for calculation
    /// </summary>
    public class BehaviorContext
    {
        public Entity Entity { get; set; }
        public World World { get; set; }
        public Vector3 CurrentPosition { get; set; }
        public Vector3 TargetPosition { get; set; }
        public float DistanceToTarget { get; set; }
        public List<Entity> NearbyAllies { get; set; }
        public List<Entity> NearbyEnemies { get; set; }
        public SquadState SquadState { get; set; }
        public float DeltaTime { get; set; }
        public SpatialGrid SpatialGrid { get; set; }
    }
    
    /// <summary>
    /// Base interface for all behaviors
    /// </summary>
    public interface IBehavior
    {
        BehaviorPriority Priority { get; }
        bool IsActive(BehaviorContext context);
        Vector3 CalculateForce(BehaviorContext context);
    }
    
    /// <summary>
    /// Main behavior system that manages all behaviors
    /// </summary>
    public class BehaviorSystem : ISystem
    {
        private World _world;
        private BehaviorManager _behaviorManager;
        private SpatialGrid _spatialGrid;
        private ObjectPool<BehaviorContext> _contextPool;
        
        public int Priority => 90;
        
        public void Initialize(World world)
        {
            _world = world;
            _behaviorManager = new BehaviorManager();
            _spatialGrid = new SpatialGrid(60, 60, 5.0f);
            _contextPool = new ObjectPool<BehaviorContext>(100);
            
            // Register core behaviors
            _behaviorManager.RegisterBehavior(new FormationKeepBehavior());
            _behaviorManager.RegisterBehavior(new SeekBehavior());
            _behaviorManager.RegisterBehavior(new SeparationBehavior());
        }
        
        public void Update(float deltaTime)
        {
            // Update spatial grid
            UpdateSpatialGrid();
            
            // Process behaviors for all entities with TroopComponent and PositionComponent
            foreach (var entity in _world.GetEntitiesWith<TroopComponent, PositionComponent, VelocityComponent>())
            {
                var troopComponent = entity.GetComponent<TroopComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                var velocityComponent = entity.GetComponent<VelocityComponent>();
                
                // Get behavior context from pool
                var context = CreateBehaviorContext(entity, troopComponent, positionComponent, deltaTime);
                
                // Calculate steering force
                Vector3 steeringForce = _behaviorManager.CalculateSteeringForce(context);
                
                // Apply force to velocity
                ApplySteeringForce(entity, steeringForce, velocityComponent);
                
                // Return context to pool
                _contextPool.Return(context);
            }
        }
        
        private void UpdateSpatialGrid()
        {
            _spatialGrid.Clear();
            
            foreach (var entity in _world.GetEntitiesWith<PositionComponent>())
            {
                var position = entity.GetComponent<PositionComponent>().Position;
                _spatialGrid.Insert(entity, position);
            }
        }
        
        private BehaviorContext CreateBehaviorContext(Entity entity, TroopComponent troop, 
            PositionComponent position, float deltaTime)
        {
            var context = _contextPool.Get();
            context.Entity = entity;
            context.World = _world;
            context.CurrentPosition = position.Position;
            context.DeltaTime = deltaTime;
            context.SpatialGrid = _spatialGrid;
            
            // Get squad information
            if (troop.SquadId != -1)
            {
                var squadEntity = _world.GetEntityById(troop.SquadId);
                if (squadEntity != null && squadEntity.HasComponent<SquadComponent>())
                {
                    var squadComponent = squadEntity.GetComponent<SquadComponent>();
                    context.SquadState = squadComponent.State;
                    
                    // Calculate target position based on formation
                    if (squadEntity.HasComponent<PositionComponent>())
                    {
                        var squadPosition = squadEntity.GetComponent<PositionComponent>().Position;
                        context.TargetPosition = squadPosition + squadComponent.GetMemberOffset(troop.FormationIndex);
                        context.DistanceToTarget = Vector3.Distance(context.CurrentPosition, context.TargetPosition);
                    }
                }
            }
            
            // Get nearby entities
            context.NearbyAllies = FindNearbyAllies(entity, position.Position, 5.0f);
            context.NearbyEnemies = FindNearbyEnemies(entity, position.Position, 10.0f);
            
            return context;
        }
        
        private List<Entity> FindNearbyAllies(Entity entity, Vector3 position, float radius)
        {
            var allies = new List<Entity>();
            var nearbyEntities = _spatialGrid.GetNearbyEntities(position, radius);
            
            foreach (var nearby in nearbyEntities)
            {
                if (nearby.Id == entity.Id) continue;
                
                // Check if same squad
                if (nearby.HasComponent<TroopComponent>())
                {
                    var nearbyTroop = nearby.GetComponent<TroopComponent>();
                    var myTroop = entity.GetComponent<TroopComponent>();
                    
                    if (nearbyTroop.SquadId == myTroop.SquadId)
                    {
                        allies.Add(nearby);
                    }
                }
            }
            
            return allies;
        }
        
        private List<Entity> FindNearbyEnemies(Entity entity, Vector3 position, float radius)
        {
            var enemies = new List<Entity>();
            var nearbyEntities = _spatialGrid.GetNearbyEntities(position, radius);
            
            foreach (var nearby in nearbyEntities)
            {
                if (nearby.Id == entity.Id) continue;
                
                // Check if different squad
                if (nearby.HasComponent<TroopComponent>())
                {
                    var nearbyTroop = nearby.GetComponent<TroopComponent>();
                    var myTroop = entity.GetComponent<TroopComponent>();
                    
                    if (nearbyTroop.SquadId != myTroop.SquadId)
                    {
                        enemies.Add(nearby);
                    }
                }
            }
            
            return enemies;
        }
        
        private void ApplySteeringForce(Entity entity, Vector3 steeringForce, VelocityComponent velocity)
        {
            // Limit steering force
            float maxForce = 10.0f;
            if (steeringForce.magnitude > maxForce)
            {
                steeringForce = steeringForce.normalized * maxForce;
            }
            
            // Apply force to velocity
            velocity.Velocity += steeringForce * Time.deltaTime;
            
            // Limit velocity
            if (velocity.Velocity.magnitude > velocity.MaxSpeed)
            {
                velocity.Velocity = velocity.Velocity.normalized * velocity.MaxSpeed;
            }
        }
    }
}