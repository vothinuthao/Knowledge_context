using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Context-based implementation of Seek steering behavior
    /// Marks directions toward the target as interesting
    /// </summary>
    [System.Serializable]
    public class ContextSeekBehavior : SteeringBehaviorBase, IContextSteeringBehavior, IContextConfigurable
    {
        [SerializeField] private float interestStrength = 1.0f;
        [SerializeField] private float directionalBias = 0.5f;
        [SerializeField] private AnimationCurve directionFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private bool useDynamicWeight = true;
        [SerializeField] private float minWeight = 0.5f;
        [SerializeField] private float maxWeight = 2.0f;
        [SerializeField] private AnimationCurve weightByDistance = AnimationCurve.Linear(0, 2, 1, 0.5f);
        
        public ContextSeekBehavior()
        {
            behaviorName = "ContextSeek";
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
            this.directionalBias = directionalBias;
            this.directionFalloff = directionFalloff;
            this.useDynamicWeight = useDynamicWeight;
            this.minWeight = minWeight;
            this.maxWeight = maxWeight;
            this.weightByDistance = weightByDistance;
        }
        
        protected override Vector3 CalculateSteeringForce(SteeringContext context)
        {
            // Legacy implementation for backward compatibility
            if (context.Target == null)
                return Vector3.zero;
            
            Vector3 desiredVelocity = (context.Target.position - context.Position).normalized * context.MaxSpeed;
            return Vector3.ClampMagnitude(desiredVelocity - context.Velocity, context.MaxForce);
        }
        
        public void FillContextMaps(SteeringContext context, float weight)
        {
            if (context.Target == null)
                return;
            Vector3 toTarget = context.Target.position - context.Position;
            float distance = toTarget.magnitude;
            float finalWeight = weight;
            if (useDynamicWeight)
            {
                float normalizedDistance = Mathf.Clamp01(distance / context.SlowingRadius);
                float weightMultiplier = weightByDistance.Evaluate(normalizedDistance);
                finalWeight = Mathf.Clamp(weight * weightMultiplier, minWeight, maxWeight);
            }
            int dirIndex = context.GetIndexFromDirection(toTarget);
            float interestValue = interestStrength * finalWeight;
            context.InterestMap[dirIndex] += interestValue;
            if (directionalBias > 0)
            {
                int forwardIndex = context.GetIndexFromDirection(context.Forward);
                context.InterestMap[forwardIndex] += interestValue * directionalBias;
            }
            int spreadAmount = Mathf.CeilToInt(context.DirectionCount / 8f);
            for (int i = 1; i <= spreadAmount; i++)
            {
                float falloffFactor = directionFalloff.Evaluate(i / (float)spreadAmount);
                
                // Wrap around the circular array
                int leftIndex = (dirIndex - i + context.DirectionCount) % context.DirectionCount;
                int rightIndex = (dirIndex + i) % context.DirectionCount;
                
                context.InterestMap[leftIndex] += interestValue * falloffFactor;
                context.InterestMap[rightIndex] += interestValue * falloffFactor;
            }
        }
    }
}