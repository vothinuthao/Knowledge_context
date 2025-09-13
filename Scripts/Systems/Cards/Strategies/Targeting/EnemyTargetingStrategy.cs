using UnityEngine;

namespace RavenDeckbuilding.Systems.Cards.Strategies
{
    /// <summary>
    /// Strategy for targeting enemies
    /// </summary>
    public class EnemyTargetingStrategy : ICardStrategy
    {
        public string StrategyName => "Enemy Targeting";
        public CardStrategyCategory Category => CardStrategyCategory.Targeting;
        public int Priority => 1000; // High priority for targeting
        
        public bool CanExecute(CardExecutionContext context)
        {
            return context.target != null && context.target != context.caster;
        }
        
        public void Execute(CardExecutionContext context)
        {
            // Validate target is still valid
            if (context.target == null)
            {
                Debug.LogWarning("Target became null during execution");
                return;
            }
            
            // Ensure we have line of sight or range check
            float distance = Vector3.Distance(context.caster.transform.position, context.target.transform.position);
            if (distance > context.cardInstance.data.castRange)
            {
                Debug.LogWarning($"Target out of range: {distance} > {context.cardInstance.data.castRange}");
                return;
            }
            
            // Target acquired successfully
            Debug.Log($"Targeting enemy: {context.target.name}");
        }
    }
}