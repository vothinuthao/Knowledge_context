using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Implementation of Arrival steering behavior
    /// Seeks towards a target position and slows down upon approach
    /// </summary>
    [System.Serializable]
    public class ArrivalBehavior : SteeringBehaviorBase
    {
        public ArrivalBehavior()
        {
            behaviorName = "Arrival";
        }
        protected override Vector3 CalculateSteeringForce(SteeringContext context)
        {
            if (context.Target == null)
                return Vector3.zero;
            Vector3 toTarget = context.Target.position - context.Position;
            float distance = toTarget.magnitude;
            if (distance < context.ArrivalRadius)
                return -context.Velocity;
            Vector3 desiredVelocity = toTarget.normalized * context.MaxSpeed;
            if (distance < context.SlowingRadius)
            {
                float speedFactor = Mathf.Clamp01(distance / context.SlowingRadius);
                desiredVelocity *= speedFactor;
            }
            return Vector3.ClampMagnitude(desiredVelocity - context.Velocity, context.MaxForce);
        }
    }
}