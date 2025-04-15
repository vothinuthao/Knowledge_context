using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Context object that provides environment information for steering behaviors
    /// </summary>
    public partial class SteeringContext
    {
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 Forward { get; set; }
        public float MaxSpeed { get; set; }
        public float MaxForce { get; set; }
        public Transform Target { get; set; }
        public LayerMask ObstacleLayer { get; set; }
        public float SlowingRadius { get; set; }
        public float ArrivalRadius { get; set; }
        public float SeparationRadius { get; set; }
        public float NeighborRadius { get; set; }
        public float FleeRadius { get; set; }
    }
}