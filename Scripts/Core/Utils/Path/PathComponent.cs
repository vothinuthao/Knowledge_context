using System.Collections.Generic;
using SteeringBehavior;
using UnityEngine;

namespace Utils
{
    public class PathComponent : MonoBehaviour
    {
        public BehaviorPath CurrentBehaviorPath { get; set; } = new BehaviorPath();
    
        public void SetPath(List<Vector3> waypoints, bool isLooped = false)
        {
            CurrentBehaviorPath.SetWaypoints(new List<Vector3>(waypoints));
            CurrentBehaviorPath.SetCurrentWaypoint(0);
            CurrentBehaviorPath.SetIsLooped(isLooped);
        }
    }
}