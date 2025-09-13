using UnityEngine;

namespace RavenDeckbuilding.Systems.Cards.Strategies
{
    /// <summary>
    /// Strategy for targeting self or allies
    /// </summary>
    public class SelfTargetingStrategy : ICardStrategy
    {
        public string StrategyName => "Self Targeting";
        public CardStrategyCategory Category => CardStrategyCategory.Targeting;
        public int Priority => 1000; // High priority for targeting
        
        public bool CanExecute(CardExecutionContext context)
        {
            // Can target self or allies (same team)
            return context.target != null && 
                   (context.target == context.caster || IsSameTeam(context.caster, context.target));
        }
        
        public void Execute(CardExecutionContext context)
        {
            if (context.target == null)
            {
                // Default to self if no target
                context.target = context.caster;
            }
            
            Debug.Log($"Targeting ally: {context.target.name}");
        }
        
        private bool IsSameTeam(Core.Player player1, Core.Player player2)
        {
            // Simple team check - in a real game you'd have proper team logic
            return player1.PlayerTeam == player2.PlayerTeam;
        }
    }
}