using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Context-based implementation of Arrival steering behavior
    /// Similar to Seek but slows down when approaching the target
    /// </summary>
    [System.Serializable]
    public class ContextArrivalBehavior : SteeringBehaviorBase, IContextSteeringBehavior, IContextConfigurable
    {
        [SerializeField] private float interestStrength = 1.0f;
        [SerializeField] private float slowDownFactor = 0.8f;
        [SerializeField] private AnimationCurve directionFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private bool useDynamicWeight = true;
        [SerializeField] private float minWeight = 0.5f;
        [SerializeField] private float maxWeight = 2.0f;
        
        public ContextArrivalBehavior()
        {
            behaviorName = "ContextArrival";
        }
        
        public void ConfigureContextSettings(
            float interestStrength,
            float dangerStrength,
            float directionalBias,
            AnimationCurve directionFalloff,
            bool useDynamicWeight,
            float minWeight,
            float maxWeight,
            AnimationCurve weightByDistance)
        {
            this.interestStrength = interestStrength;
            this.directionFalloff = directionFalloff;
            this.useDynamicWeight = useDynamicWeight;
            this.minWeight = minWeight;
            this.maxWeight = maxWeight;
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
        
        public void FillContextMaps(SteeringContext context, float weight)
        {
            if (context.Target == null)
                return;
            Vector3 toTarget = context.Target.position - context.Position;
            float distance = toTarget.magnitude;
            if (distance < context.ArrivalRadius)
                return;
            float speedFactor = 1.0f;
            if (distance < context.SlowingRadius)
            {
                speedFactor = Mathf.Clamp01(distance / context.SlowingRadius);
                speedFactor = Mathf.Max(0.1f, speedFactor);
            }
            float finalWeight = weight;
            if (useDynamicWeight)
            {
                finalWeight = Mathf.Clamp(weight * speedFactor, minWeight, maxWeight);
            }
            int dirIndex = context.GetIndexFromDirection(toTarget);
            float interestValue = interestStrength * finalWeight * speedFactor;
            context.InterestMap[dirIndex] += interestValue;
            int spreadAmount = Mathf.CeilToInt(context.DirectionCount / 8f);
            for (int i = 1; i <= spreadAmount; i++)
            {
                float falloff = directionFalloff.Evaluate(i / (float)spreadAmount);
                int leftIndex = (dirIndex - i + context.DirectionCount) % context.DirectionCount;
                int rightIndex = (dirIndex + i) % context.DirectionCount;
                context.InterestMap[leftIndex] += interestValue * falloff;
                context.InterestMap[rightIndex] += interestValue * falloff;
            }
        }
    }
}