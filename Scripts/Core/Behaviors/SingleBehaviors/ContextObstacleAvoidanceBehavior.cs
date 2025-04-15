using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Context-based implementation of Obstacle Avoidance steering behavior
    /// Uses context maps to mark directions with obstacles as dangerous
    /// </summary>
    [System.Serializable]
    public class ContextObstacleAvoidanceBehavior : SteeringBehaviorBase, IContextSteeringBehavior, IContextConfigurable
    {
        [SerializeField] private float lookAheadDistance = 3.0f;
        [SerializeField] private float dangerWeight = 2.0f;
        [SerializeField] private int rayCount = 8;
        [SerializeField] private AnimationCurve directionFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private bool useDynamicWeight = true;
        [SerializeField] private float minWeight = 1.0f;
        [SerializeField] private float maxWeight = 3.0f;
        
        public ContextObstacleAvoidanceBehavior()
        {
            behaviorName = "ContextObstacleAvoidance";
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
            this.dangerWeight = dangerStrength;
            this.directionFalloff = directionFalloff;
            this.useDynamicWeight = useDynamicWeight;
            this.minWeight = minWeight;
            this.maxWeight = maxWeight;
        }
        
        protected override Vector3 CalculateSteeringForce(SteeringContext context)
        {
            Vector3 steeringForce = Vector3.zero;
            float startAngle = -90f;
            float angleStep = 180f / (rayCount - 1);
            
            for (int i = 0; i < rayCount; i++)
            {
                float angle = startAngle + i * angleStep;
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 rayDirection = rotation * context.Forward;
                
                RaycastHit hit;
                if (Physics.Raycast(context.Position, rayDirection, out hit, lookAheadDistance, context.ObstacleLayer))
                {
                    float weight = 1.0f - (hit.distance / lookAheadDistance);
                    Vector3 avoidDirection = Vector3.Cross(hit.normal, Vector3.up).normalized;
                    
                    if (Vector3.Dot(avoidDirection, context.Forward) < 0)
                        avoidDirection = -avoidDirection;
                    
                    steeringForce += avoidDirection * (weight * dangerWeight);
                }
            }
            
            return steeringForce;
        }
        
        public void FillContextMaps(SteeringContext context, float weight)
        {
            float finalWeight = useDynamicWeight ? 
                Mathf.Clamp(weight * 1.5f, minWeight, maxWeight) : weight;
            int raysToUse = Mathf.Min(context.DirectionCount, 32);
            float angleStep = 360f / raysToUse;
            
            for (int i = 0; i < raysToUse; i++)
            {
                float angle = i * angleStep;
                Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                
                RaycastHit hit;
                if (Physics.Raycast(context.Position, rayDirection, out hit, lookAheadDistance, context.ObstacleLayer))
                {
                    float dangerValue = (lookAheadDistance - hit.distance) / lookAheadDistance;
                    dangerValue *= dangerWeight * finalWeight;
                    
                    int dirIndex = context.GetIndexFromDirection(rayDirection);
                    
                    context.DangerMap[dirIndex] += dangerValue;
                    
                    int spreadAmount = Mathf.CeilToInt(context.DirectionCount / 16f);
                    for (int j = 1; j <= spreadAmount; j++)
                    {
                        float falloff = directionFalloff.Evaluate(j / (float)spreadAmount);
                        
                        int leftIndex = (dirIndex - j + context.DirectionCount) % context.DirectionCount;
                        int rightIndex = (dirIndex + j) % context.DirectionCount;
                        
                        context.DangerMap[leftIndex] += dangerValue * falloff;
                        context.DangerMap[rightIndex] += dangerValue * falloff;
                    }
                }
            }
            
            foreach (var obstacle in context.VisibleObstacles)
            {
                Vector3 toObstacle = obstacle.position - context.Position;
                float distance = toObstacle.magnitude;
                
                if (distance < lookAheadDistance)
                {
                    int dirIndex = context.GetIndexFromDirection(toObstacle);
                    float dangerValue = (lookAheadDistance - distance) / lookAheadDistance;
                    dangerValue *= dangerWeight * finalWeight;
                    
                    context.DangerMap[dirIndex] += dangerValue;
                }
            }
        }
    }
}