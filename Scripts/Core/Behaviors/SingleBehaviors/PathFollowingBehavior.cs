using System.Collections.Generic;
using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Implementation of Path Following steering behavior
    /// Follows a series of waypoints
    /// </summary>
    [System.Serializable]
    public class PathFollowingBehavior : SteeringBehaviorBase
    {
        [SerializeField] private float waypointReachedDistance = 0.5f;
        [SerializeField] private float lookAheadDistance = 2.0f;
        [SerializeField] private List<Vector3> path = new List<Vector3>();
        [SerializeField] private int currentWaypointIndex = 0;
        [SerializeField] private bool loopPath = false;
        
        public PathFollowingBehavior()
        {
            behaviorName = "PathFollowing";
        }
        
        public void SetPath(List<Vector3> newPath, bool loop = false)
        {
            path = newPath;
            loopPath = loop;
            currentWaypointIndex = 0;
        }
        
        protected override Vector3 CalculateSteeringForce(SteeringContext context)
        {
            if (path.Count == 0)
                return Vector3.zero;
            if (currentWaypointIndex < path.Count)
            {
                Vector3 toWaypoint = path[currentWaypointIndex] - context.Position;
                float distance = toWaypoint.magnitude;
                if (distance < waypointReachedDistance)
                {
                    currentWaypointIndex++;
                    if (currentWaypointIndex >= path.Count && loopPath)
                        currentWaypointIndex = 0;
                }
            }
            if (currentWaypointIndex >= path.Count)
                return -context.Velocity;
            Vector3 targetPoint = CalculateTargetPoint(context);
            var seekContext = new SteeringContext
            {
                Position = context.Position,
                Velocity = context.Velocity,
                MaxSpeed = context.MaxSpeed,
                MaxForce = context.MaxForce
            };
            GameObject tempTarget = new GameObject("TempTarget");
            tempTarget.transform.position = targetPoint;
            seekContext.Target = tempTarget.transform;
            var seekBehavior = new ContextSeekBehavior();
            Vector3 steeringForce = seekBehavior.CalculateForce(seekContext);
            Object.Destroy(tempTarget);
            
            return steeringForce;
        }
        
        /// <summary>
        /// Calculate target point along path at a point ahead of the current position
        /// </summary>
        private Vector3 CalculateTargetPoint(SteeringContext context)
        {
            if (currentWaypointIndex >= path.Count - 1)
                return path[^1];
            Vector3 currentWaypoint = path[currentWaypointIndex];
            Vector3 nextWaypoint = path[currentWaypointIndex + 1];
            Vector3 pathDirection = (nextWaypoint - currentWaypoint).normalized;
            Vector3 toAgent = context.Position - currentWaypoint;
            float dotProduct = Vector3.Dot(toAgent, pathDirection);
            Vector3 projection = currentWaypoint + pathDirection * dotProduct;
            Vector3 targetPoint = projection + pathDirection * lookAheadDistance;
            float segmentLength = Vector3.Distance(currentWaypoint, nextWaypoint);
            float distanceFromStart = Vector3.Distance(currentWaypoint, projection) + lookAheadDistance;
            if (distanceFromStart > segmentLength)
                targetPoint = nextWaypoint;
            
            Debug.DrawLine(context.Position, projection, Color.blue);
            Debug.DrawLine(projection, targetPoint, Color.green);
            
            return targetPoint;
        }
    }
}