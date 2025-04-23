using System.Collections.Generic;
using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for steering behavior data
    /// </summary>
    public class SteeringDataComponent : IComponent
    {
        // Final steering force calculated from all behaviors
        public Vector3 SteeringForce { get; set; } = Vector3.zero;
        
        // Target position for steering
        public Vector3 TargetPosition { get; set; } = Vector3.zero;
        
        // Position to avoid
        public Vector3 AvoidPosition { get; set; } = Vector3.zero;
        
        // Nearby entities for flocking behaviors
        public List<int> NearbyAlliesIds { get; set; } = new List<int>();
        public List<int> NearbyEnemiesIds { get; set; } = new List<int>();
        
        // Maximum steering force
        public float MaxForce { get; set; } = 10.0f;
        
        // Whether steering is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Whether entity is in danger
        public bool IsInDanger { get; set; } = false;
        
        public SteeringDataComponent()
        {
        }
        
        /// <summary>
        /// Add accumulated steering force, respecting max force
        /// </summary>
        public void AddForce(Vector3 force)
        {
            SteeringForce += force;
            
            if (SteeringForce.magnitude > MaxForce)
            {
                SteeringForce = SteeringForce.normalized * MaxForce;
            }
        }
        
        /// <summary>
        /// Reset all forces to zero
        /// </summary>
        public void Reset()
        {
            SteeringForce = Vector3.zero;
        }
    }
}