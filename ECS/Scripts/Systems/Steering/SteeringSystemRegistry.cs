using Core.ECS;
using UnityEngine;

namespace Systems.Steering
{
    /// <summary>
    /// Helper class to register all steering systems with the world
    /// </summary>
    public static class SteeringSystemRegistry
    {
        /// <summary>
        /// Register all steering systems with the world
        /// </summary>
        public static void RegisterSystems(World world)
        {
            // Base steering systems
            world.RegisterSystem(new SeekSystem());
            world.RegisterSystem(new SeparationSystem());
            world.RegisterSystem(new AlignmentSystem());
            world.RegisterSystem(new CohesionSystem());
            world.RegisterSystem(new ArrivalSystem());
            world.RegisterSystem(new FleeSystem());
            world.RegisterSystem(new ObstacleAvoidanceSystem());
            world.RegisterSystem(new PathFollowingSystem());
            
            // Advanced behaviors
            world.RegisterSystem(new JumpAttackSystem());
            world.RegisterSystem(new AmbushMoveSystem());
            world.RegisterSystem(new ChargeSystem());
            world.RegisterSystem(new PhalanxSystem());
            world.RegisterSystem(new TestudoSystem());
            world.RegisterSystem(new ProtectSystem());
            world.RegisterSystem(new CoverSystem());
            world.RegisterSystem(new SurroundSystem());
            
            // Main steering system (applies forces to entities)
            world.RegisterSystem(new SteeringSystem());
            
            Debug.Log("All steering systems registered");
        }
    }
}