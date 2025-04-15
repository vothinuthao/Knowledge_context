using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Context-based implementation of Flee steering behavior
    /// Marks directions toward threats as dangerous
    /// </summary>
    [System.Serializable]
    public class ContextFleeBehavior : SteeringBehaviorBase, IContextSteeringBehavior, IContextConfigurable
    {
        [SerializeField] private float dangerStrength = 2.0f;
        [SerializeField] private AnimationCurve directionFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private float panicMultiplier = 1.5f;
        [SerializeField] private bool useDynamicWeight = true;
        [SerializeField] private float minWeight = 0.5f;
        [SerializeField] private float maxWeight = 3.0f;
        
        public ContextFleeBehavior()
        {
            behaviorName = "ContextFlee";
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
            if (!context.Target)
                return Vector3.zero;
                
            Vector3 fromTarget = context.Position - context.Target.position;
            float distance = fromTarget.magnitude;
            if (context.FleeRadius > 0 && distance > context.FleeRadius)
                return Vector3.zero;
                
            Vector3 desiredVelocity = fromTarget.normalized * context.MaxSpeed;
            return Vector3.ClampMagnitude(desiredVelocity - context.Velocity, context.MaxForce);
        }
        
        public void FillContextMaps(SteeringContext context, float weight)
        {
            if (context.Target == null && context.VisibleEnemies.Count == 0)
                return;
                
            float finalWeight = weight;
            if (context.Target != null)
            {
                ProcessThreat(context, context.Target, finalWeight);
            }
            foreach (var enemy in context.VisibleEnemies)
            {
                ProcessThreat(context, enemy, finalWeight * panicMultiplier);
            }
            foreach (var memoryEntity in context.MemoryEntities)
            {
                Transform entity = memoryEntity.Key;
                if (!entity.CompareTag("Enemy"))
                    continue;
                    
                float memoryFactor = memoryEntity.Value / 3.0f;
                ProcessThreat(context, entity, finalWeight * memoryFactor * 0.7f);
            }
        }
        
        private void ProcessThreat(SteeringContext context, Transform threat, float weight)
        {
            Vector3 toThreat = threat.position - context.Position;
            float distance = toThreat.magnitude;
            if (context.FleeRadius > 0 && distance > context.FleeRadius)
                return;
            float dangerValue = useDynamicWeight ? 
                Mathf.Clamp(weight * (1 - distance / context.FleeRadius), minWeight, maxWeight) : 
                weight;
            dangerValue *= dangerStrength;
            int threatIndex = context.GetIndexFromDirection(toThreat);
            context.DangerMap[threatIndex] += dangerValue;
            int spreadAmount = Mathf.CeilToInt(context.DirectionCount / 6f);
            for (int i = 1; i <= spreadAmount; i++)
            {
                float falloff = directionFalloff.Evaluate(i / (float)spreadAmount);
                int leftIndex = (threatIndex - i + context.DirectionCount) % context.DirectionCount;
                int rightIndex = (threatIndex + i) % context.DirectionCount;
                
                context.DangerMap[leftIndex] += dangerValue * falloff;
                context.DangerMap[rightIndex] += dangerValue * falloff;
            }
            Vector3 awayFromThreat = -toThreat.normalized;
            int awayIndex = context.GetIndexFromDirection(awayFromThreat);
            context.InterestMap[awayIndex] += dangerValue * 0.5f;
            for (int i = 1; i <= spreadAmount; i++)
            {
                float falloff = directionFalloff.Evaluate(i / (float)spreadAmount);
                
                int leftIndex = (awayIndex - i + context.DirectionCount) % context.DirectionCount;
                int rightIndex = (awayIndex + i) % context.DirectionCount;
                context.InterestMap[leftIndex] += dangerValue * 0.5f * falloff;
                context.InterestMap[rightIndex] += dangerValue * 0.5f * falloff;
            }
        }
    }
}