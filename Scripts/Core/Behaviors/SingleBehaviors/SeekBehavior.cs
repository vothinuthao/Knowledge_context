
using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Implementation of Seek steering behavior
    /// Seeks towards a target position
    /// </summary>
    [System.Serializable]
    public class SeekBehavior : SteeringBehaviorBase
    {
        public SeekBehavior()
        {
            behaviorName = "Seek";
        }
        
        protected override Vector3 CalculateSteeringForce(SteeringContext context)
        {
            if (context.Target == null)
                return Vector3.zero;
            Vector3 desiredVelocity = (context.Target.position - context.Position).normalized * context.MaxSpeed;
            return Vector3.ClampMagnitude(desiredVelocity - context.Velocity, context.MaxForce);
        }
    }
}