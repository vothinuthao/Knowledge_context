using Core.ECS;
using UnityEngine;
using System.Collections.Generic;

namespace Steering
{
    /// <summary>
    /// Component for path following steering behavior
    /// </summary>
    public class PathFollowingComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 1.0f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Path radius (how close to path to stay)
        public float PathRadius { get; set; } = 1.0f;
        
        // Distance at which to move to next waypoint
        public float ArrivalDistance { get; set; } = 0.5f;
        
        // Current path to follow
        public List<Vector3> Waypoints { get; set; } = new List<Vector3>();
        
        // Current waypoint index
        public int CurrentWaypointIndex { get; set; } = 0;
        
        public PathFollowingComponent(float weight = 1.0f, float pathRadius = 1.0f, float arrivalDistance = 0.5f)
        {
            Weight = weight;
            PathRadius = pathRadius;
            ArrivalDistance = arrivalDistance;
        }
        
        /// <summary>
        /// Get current waypoint
        /// </summary>
        public Vector3 GetCurrentWaypoint()
        {
            if (Waypoints.Count == 0)
                return Vector3.zero;
                
            if (CurrentWaypointIndex < 0 || CurrentWaypointIndex >= Waypoints.Count)
                CurrentWaypointIndex = 0;
                
            return Waypoints[CurrentWaypointIndex];
        }
        
        /// <summary>
        /// Advance to next waypoint
        /// </summary>
        /// <returns>True if advanced to next waypoint, false if at end of path</returns>
        public bool AdvanceToNext()
        {
            if (Waypoints.Count == 0)
                return false;
                
            CurrentWaypointIndex++;
            
            if (CurrentWaypointIndex >= Waypoints.Count)
            {
                CurrentWaypointIndex = 0;
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Sets a new path to follow
        /// </summary>
        public void SetPath(List<Vector3> newPath)
        {
            Waypoints = new List<Vector3>(newPath);
            CurrentWaypointIndex = 0;
        }
    }
}