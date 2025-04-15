using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Context-based implementation of Cohesion steering behavior
    /// Marks directions toward center of mass of neighbors as interesting
    /// </summary>
    [System.Serializable]
    public class ContextCohesionBehavior : SteeringBehaviorBase, IContextSteeringBehavior, IContextConfigurable
    {
        [SerializeField] private float interestStrength = 1.0f;
        [SerializeField] private AnimationCurve directionFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private bool useDynamicWeight = true;
        [SerializeField] private float minWeight = 0.5f;
        [SerializeField] private float maxWeight = 2.0f;
        
        public ContextCohesionBehavior()
        {
            behaviorName = "ContextCohesion";
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
            Vector3 centerOfMass = Vector3.zero;
            int neighborCount = 0;
            
            foreach (var neighbor in context.VisibleNeighbors)
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
            
            Vector3 toCenter = centerOfMass - context.Position;
            Vector3 desiredVelocity = toCenter.normalized * context.MaxSpeed;
            
            return Vector3.ClampMagnitude(desiredVelocity - context.Velocity, context.MaxForce);
        }
        
        public void FillContextMaps(SteeringContext context, float weight)
        {
            Vector3 centerOfMass = Vector3.zero;
            int neighborCount = 0;
            
            foreach (var neighbor in context.VisibleNeighbors)
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
                return;
                
            centerOfMass /= neighborCount;
            
            float finalWeight = useDynamicWeight ? 
                Mathf.Clamp(weight * context.DynamicCohesionWeight, minWeight, maxWeight) : 
                weight;
            
            Vector3 toCenter = centerOfMass - context.Position;
            if (toCenter.magnitude < 0.001f)
                return;
            
            int centerIndex = context.GetIndexFromDirection(toCenter);
            float interestValue = interestStrength * finalWeight;
            context.InterestMap[centerIndex] += interestValue;
            int spreadAmount = Mathf.CeilToInt(context.DirectionCount / 8f);
            for (int i = 1; i <= spreadAmount; i++)
            {
                float falloff = directionFalloff.Evaluate(i / (float)spreadAmount);
                int leftIndex = (centerIndex - i + context.DirectionCount) % context.DirectionCount;
                int rightIndex = (centerIndex + i) % context.DirectionCount;
                
                context.InterestMap[leftIndex] += interestValue * falloff;
                context.InterestMap[rightIndex] += interestValue * falloff;
            }
        }
    }
}