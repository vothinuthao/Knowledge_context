using RavenDeckbuilding.Systems.Cards.Strategies;
using UnityEngine;

namespace RavenDeckbuilding.Systems.Cards
{
    /// <summary>
    /// Example damage card implementation
    /// </summary>
    public class DamageCard : StrategyBaseCard
    {
        [Header("Damage Settings")]
        [SerializeField] private float damage = 50f;
        
        public void Initialize(CardData data)
        {
            cardData = data;
            damage = data.effects.Length > 0 ? data.effects[0].value : damage;
        }
        
        protected override void InitializeStrategies()
        {
            // Add targeting strategy
            _strategyExecutor.AddStrategy(new EnemyTargetingStrategy());
            
            // Add damage effect strategy
            _strategyExecutor.AddStrategy(new DamageEffectStrategy(damage));
            
            // Add visual effect strategy
            if (cardData?.impactEffectPrefab != null)
                _strategyExecutor.AddStrategy(new VFXStrategy(cardData.impactEffectPrefab));
            
            // Add audio strategy
            if (cardData?.castSound != null)
                _strategyExecutor.AddStrategy(new AudioStrategy(cardData.castSound));
        }
        
        public override void ShowPreview(CardExecutionContext context)
        {
            if (context.target != null)
            {
                // Show damage preview
                PreviewManager.Instance?.ShowDamagePreview(context.target.transform.position, damage);
            }
        }
        
        public override void HidePreview()
        {
            PreviewManager.Instance?.HideAllPreviews();
        }
        
        void OnEnable()
        {
            StrategyCardRegistry.Instance?.RegisterCard(this);
        }
        
        void OnDisable()
        {
            StrategyCardRegistry.Instance?.UnregisterCard(this);
        }
    }
}