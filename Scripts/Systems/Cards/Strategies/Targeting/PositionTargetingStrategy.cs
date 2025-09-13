using UnityEngine;

namespace RavenDeckbuilding.Systems.Cards.Strategies
{
    /// <summary>
    /// Strategy for targeting positions on the ground
    /// </summary>
    public class PositionTargetingStrategy : ICardStrategy
    {
        public string StrategyName => "Position Targeting";
        public CardStrategyCategory Category => CardStrategyCategory.Targeting;
        public int Priority => 1000; // High priority for targeting
        
        public bool CanExecute(CardExecutionContext context)
        {
            return context.targetPosition != Vector3.zero;
        }
        
        public void Execute(CardExecutionContext context)
        {
            // Validate target position is within range
            float distance = Vector3.Distance(context.caster.transform.position, context.targetPosition);
            if (distance > context.cardInstance.data.castRange)
            {
                Debug.LogWarning($"Target position out of range: {distance} > {context.cardInstance.data.castRange}");
                return;
            }
            
            // Target position acquired successfully
            Debug.Log($"Targeting position: {context.targetPosition}");
        }
    }
}