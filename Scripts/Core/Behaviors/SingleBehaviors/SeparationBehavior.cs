using System.Collections.Generic;
using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Implementation of Separation steering behavior
    /// Steers away from neighbors to maintain distance
    /// </summary>
    [System.Serializable]
    public class SeparationBehavior : SteeringBehaviorBase
    {
        public SeparationBehavior()
        {
            behaviorName = "Separation";
        }
        
        protected override Vector3 CalculateSteeringForce(SteeringContext context)
        {
            Vector3 steeringForce = Vector3.zero;
            int neighborCount = 0;
            var neighbors = GetNeighbors(context);
            
            foreach (var neighbor in neighbors)
            {
                if (Vector3.Distance(neighbor.position, context.Position) < 0.001f)
                    continue;
                Vector3 toNeighbor = context.Position - neighbor.position;
                float distance = toNeighbor.magnitude;
                if (distance < context.SeparationRadius)
                {
                    Vector3 separationForce = toNeighbor.normalized / Mathf.Max(0.1f, distance);
                    steeringForce += separationForce;
                    neighborCount++;
                }
            }
            if (neighborCount > 0)
                steeringForce /= neighborCount;
            
            if (steeringForce.magnitude > 0)
            {
                steeringForce = steeringForce.normalized * context.MaxSpeed;
                steeringForce = Vector3.ClampMagnitude(steeringForce - context.Velocity, context.MaxForce);
            }
            
            return steeringForce;
        }
        private List<Transform> GetNeighbors(SteeringContext context)
        {
            return new List<Transform>();
        }
    }
}