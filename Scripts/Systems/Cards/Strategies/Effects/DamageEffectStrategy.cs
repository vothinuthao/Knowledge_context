using UnityEngine;
using RavenDeckbuilding.Core;

namespace RavenDeckbuilding.Systems.Cards.Strategies
{
    /// <summary>
    /// Strategy for dealing damage to targets
    /// </summary>
    public class DamageEffectStrategy : ICardStrategy
    {
        private float _damageAmount;
        
        public string StrategyName => "Damage Effect";
        public CardStrategyCategory Category => CardStrategyCategory.Effect;
        public int Priority => 500; // Medium priority for effects
        
        public DamageEffectStrategy(float damage)
        {
            _damageAmount = damage;
        }
        
        public bool CanExecute(CardExecutionContext context)
        {
            return context.target != null && _damageAmount > 0;
        }
        
        public void Execute(CardExecutionContext context)
        {
            if (context.target != null)
            {
                // Apply damage to target
                context.target.TakeDamage((int)_damageAmount);
                
                Debug.Log($"Dealt {_damageAmount} damage to {context.target.name}");
            }
        }
    }
}