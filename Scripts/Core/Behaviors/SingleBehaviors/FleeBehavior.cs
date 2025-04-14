using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Implementation of Flee steering behavior
    /// Moves away from a target position
    /// </summary>
    [System.Serializable]
    public class FleeBehavior : SteeringBehaviorBase
    {
        public FleeBehavior()
        {
            behaviorName = "Flee";
        }
        
        protected override Vector3 CalculateSteeringForce(SteeringContext context)
        {
            if (context.Target == null)
                return Vector3.zero;
            Vector3 fromTarget = context.Position - context.Target.position;
            float distance = fromTarget.magnitude;
            if (context.FleeRadius > 0 && distance > context.FleeRadius)
                return Vector3.zero;
            Vector3 desiredVelocity = fromTarget.normalized * context.MaxSpeed;
            return Vector3.ClampMagnitude(desiredVelocity - context.Velocity, context.MaxForce);
        }
    }
}