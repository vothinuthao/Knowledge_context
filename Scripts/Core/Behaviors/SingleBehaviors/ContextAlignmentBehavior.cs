using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Context-based implementation of Alignment steering behavior
    /// Marks directions aligned with neighbors' heading as interesting
    /// </summary>
    [System.Serializable]
    public class ContextAlignmentBehavior : SteeringBehaviorBase, IContextSteeringBehavior, IContextConfigurable
    {
        [SerializeField] private float interestStrength = 1.0f;
        [SerializeField] private AnimationCurve directionFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private bool useDynamicWeight = true;
        [SerializeField] private float minWeight = 0.5f;
        [SerializeField] private float maxWeight = 2.0f;
        
        public ContextAlignmentBehavior()
        {
            behaviorName = "ContextAlignment";
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
            Vector3 averageHeading = Vector3.zero;
            int neighborCount = 0;
            
            foreach (var neighbor in context.VisibleNeighbors)
            {
                if (Vector3.Distance(neighbor.position, context.Position) < 0.001f)
                    continue;
                    
                float distance = Vector3.Distance(neighbor.position, context.Position);
                if (distance < context.NeighborRadius)
                {
                    Rigidbody rb = neighbor.GetComponent<Rigidbody>();
                    if (rb != null && rb.linearVelocity.magnitude > 0.1f)
                    {
                        averageHeading += rb.linearVelocity.normalized;
                    }
                    else
                    {
                        averageHeading += neighbor.forward;
                    }
                    neighborCount++;
                }
            }
            
            if (neighborCount == 0)
                return Vector3.zero;
                
            averageHeading.Normalize();
            
            Vector3 desiredVelocity = averageHeading * context.MaxSpeed;
            return Vector3.ClampMagnitude(desiredVelocity - context.Velocity, context.MaxForce);
        }
        
        public void FillContextMaps(SteeringContext context, float weight)
        {
            // Calculate average heading
            Vector3 averageHeading = Vector3.zero;
            int neighborCount = 0;
            
            foreach (var neighbor in context.VisibleNeighbors)
            {
                if (Vector3.Distance(neighbor.position, context.Position) < 0.001f)
                    continue;
                    
                float distance = Vector3.Distance(neighbor.position, context.Position);
                if (distance < context.NeighborRadius)
                {
                    Rigidbody rb = neighbor.GetComponent<Rigidbody>();
                    if (rb && rb.linearVelocity.magnitude > 0.1f)
                    {
                        averageHeading += rb.linearVelocity.normalized;
                    }
                    else
                    {
                        // Otherwise use forward vector
                        averageHeading += neighbor.forward;
                    }
                    neighborCount++;
                }
            }
            if (neighborCount == 0 || averageHeading.magnitude < 0.001f)
                return;
                
            averageHeading.Normalize();
            float finalWeight = useDynamicWeight ? 
                Mathf.Clamp(weight * context.DynamicAlignmentWeight, minWeight, maxWeight) : 
                weight;
                
            int headingIndex = context.GetIndexFromDirection(averageHeading);
            
            float interestValue = interestStrength * finalWeight;
            context.InterestMap[headingIndex] += interestValue;
            
            int spreadAmount = Mathf.CeilToInt(context.DirectionCount / 8f);
            for (int i = 1; i <= spreadAmount; i++)
            {
                float falloff = directionFalloff.Evaluate(i / (float)spreadAmount);
                
                int leftIndex = (headingIndex - i + context.DirectionCount) % context.DirectionCount;
                int rightIndex = (headingIndex + i) % context.DirectionCount;
                
                context.InterestMap[leftIndex] += interestValue * falloff;
                context.InterestMap[rightIndex] += interestValue * falloff;
            }
        }
    }
}