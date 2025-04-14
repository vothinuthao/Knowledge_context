using System.Collections.Generic;
using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Implementation of Cohesion steering behavior
    /// Steers towards the center of mass of neighbors
    /// </summary>
    [System.Serializable]
    public class CohesionBehavior : SteeringBehaviorBase
    {
        public CohesionBehavior()
        {
            behaviorName = "Cohesion";
        }
        
        protected override Vector3 CalculateSteeringForce(SteeringContext context)
        {
            Vector3 centerOfMass = Vector3.zero;
            int neighborCount = 0;
            var neighbors = GetNeighbors(context);
            
            foreach (var neighbor in neighbors)
            {
                if (Vector3.Distance(neighbor.position, context.Position) < 0.001f)
                    continue;
                float distance = Vector3.Distance(neighbor.position, context.Position);
                if (distance < context.NeighborRadius)
                {
                    centerOfMass += neighbor.position;
                    neighborCount++;
                }
            }
            if (neighborCount == 0)
                return Vector3.zero;
            centerOfMass /= neighborCount;
            var seekContext = new SteeringContext
            {
                Position = context.Position,
                Velocity = context.Velocity,
                MaxSpeed = context.MaxSpeed,
                MaxForce = context.MaxForce
            };
            GameObject tempTarget = new GameObject("TempTarget");
            tempTarget.transform.position = centerOfMass;
            seekContext.Target = tempTarget.transform;
            var seekBehavior = new SeekBehavior();
            Vector3 steeringForce = seekBehavior.CalculateForce(seekContext);
            Object.Destroy(tempTarget);
            return steeringForce;
        }
        private List<Transform> GetNeighbors(SteeringContext context)
        {
            return new List<Transform>();
        }
    }
}