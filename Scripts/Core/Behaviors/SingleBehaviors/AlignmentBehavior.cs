using System.Collections.Generic;
using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Implementation of Alignment steering behavior
    /// Steers to align velocity with neighbors
    /// </summary>
    [System.Serializable]
    public class AlignmentBehavior : SteeringBehaviorBase
    {
        public AlignmentBehavior()
        {
            behaviorName = "Alignment";
        }
        
        protected override Vector3 CalculateSteeringForce(SteeringContext context)
        {
            Vector3 averageHeading = Vector3.zero;
            int neighborCount = 0;
            var neighbors = GetNeighborsWithVelocities(context);
            
            foreach (var neighbor in neighbors)
            {
                if (Vector3.Distance(neighbor.transform.position, context.Position) < 0.001f)
                    continue;
                float distance = Vector3.Distance(neighbor.transform.position, context.Position);
                if (distance < context.NeighborRadius)
                {
                    averageHeading += neighbor.forward;
                    neighborCount++;
                }
            }
            if (neighborCount == 0)
                return Vector3.zero;
            averageHeading.Normalize();
            Vector3 desiredVelocity = averageHeading * context.MaxSpeed;
            return Vector3.ClampMagnitude(desiredVelocity - context.Velocity, context.MaxForce);
        }
        private List<Transform> GetNeighborsWithVelocities(SteeringContext context)
        {
            return new List<Transform>();
        }
    }
}