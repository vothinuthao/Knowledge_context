using UnityEngine;
using RavenDeckbuilding.Core;

namespace RavenDeckbuilding.Systems.Cards.Strategies
{
    /// <summary>
    /// Strategy for healing targets
    /// </summary>
    public class HealEffectStrategy : ICardStrategy
    {
        private float _healAmount;
        
        public string StrategyName => "Heal Effect";
        public CardStrategyCategory Category => CardStrategyCategory.Effect;
        public int Priority => 500; // Medium priority for effects
        
        public HealEffectStrategy(float healAmount)
        {
            _healAmount = healAmount;
        }
        
        public bool CanExecute(CardExecutionContext context)
        {
            return context.target != null && _healAmount > 0;
        }
        
        public void Execute(CardExecutionContext context)
        {
            if (context.target != null)
            {
                // Apply healing to target
                context.target.Heal((int)_healAmount);
                
                Debug.Log($"Healed {context.target.name} for {_healAmount}");
            }
        }
    }
}