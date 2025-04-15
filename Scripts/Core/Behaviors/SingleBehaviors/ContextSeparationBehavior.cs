using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Context-based implementation of Separation steering behavior
    /// Steers away from neighbors to maintain distance
    /// </summary>
    [System.Serializable]
    public class ContextSeparationBehavior : SteeringBehaviorBase, IContextSteeringBehavior, IContextConfigurable
    {
        [SerializeField] private float dangerStrength = 1.5f;
        [SerializeField] private AnimationCurve directionFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private bool useDynamicWeight = true;
        [SerializeField] private float minWeight = 0.5f;
        [SerializeField] private float maxWeight = 2.5f;
        
        public ContextSeparationBehavior()
        {
            behaviorName = "ContextSeparation";
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
            this.dangerStrength = dangerStrength;
            this.directionFalloff = directionFalloff;
            this.useDynamicWeight = useDynamicWeight;
            this.minWeight = minWeight;
            this.maxWeight = maxWeight;
        }
        
        protected override Vector3 CalculateSteeringForce(SteeringContext context)
        {
            Vector3 steeringForce = Vector3.zero;
            int neighborCount = 0;
            
            foreach (var neighbor in context.VisibleNeighbors)
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
        
        public void FillContextMaps(SteeringContext context, float weight)
        {
            float finalWeight = useDynamicWeight ? 
                Mathf.Clamp(weight * context.DynamicSeparationWeight, minWeight, maxWeight) : 
                weight;
            
            foreach (var neighbor in context.VisibleNeighbors)
            {
                if (Vector3.Distance(neighbor.position, context.Position) < 0.001f)
                    continue;
                
                Vector3 toNeighbor = neighbor.position - context.Position;
                float distance = toNeighbor.magnitude;
                
                if (distance < context.SeparationRadius)
                {
                    float dangerValue = (context.SeparationRadius - distance) / context.SeparationRadius;
                    dangerValue *= dangerStrength * finalWeight;
                    int dirIndex = context.GetIndexFromDirection(toNeighbor);
                    context.DangerMap[dirIndex] += dangerValue;
                    
                    int spreadAmount = Mathf.CeilToInt(context.DirectionCount / 8f);
                    for (int i = 1; i <= spreadAmount; i++)
                    {
                        float falloff = directionFalloff.Evaluate(i / (float)spreadAmount);
                        int leftIndex = (dirIndex - i + context.DirectionCount) % context.DirectionCount;
                        int rightIndex = (dirIndex + i) % context.DirectionCount;
                        context.DangerMap[leftIndex] += dangerValue * falloff;
                        context.DangerMap[rightIndex] += dangerValue * falloff;
                    }
                }
            }
            foreach (var memoryEntity in context.MemoryEntities)
            {
                Transform entity = memoryEntity.Key;
                float memoryFactor = memoryEntity.Value / 3.0f;
                
                Vector3 toEntity = entity.position - context.Position;
                float distance = toEntity.magnitude;
                
                if (distance < context.SeparationRadius)
                {
                    float dangerValue = (context.SeparationRadius - distance) / context.SeparationRadius;
                    dangerValue *= dangerStrength * finalWeight * memoryFactor * 0.5f;
                    
                    int dirIndex = context.GetIndexFromDirection(toEntity);
                    context.DangerMap[dirIndex] += dangerValue;
                }
            }
        }
    }
}