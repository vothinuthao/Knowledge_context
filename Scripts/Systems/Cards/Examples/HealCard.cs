using RavenDeckbuilding.Systems.Cards.Strategies;
using UnityEngine;

namespace RavenDeckbuilding.Systems.Cards
{
    /// <summary>
    /// Example heal card implementation
    /// </summary>
    public class HealCard : StrategyBaseCard
    {
        [Header("Heal Settings")]
        [SerializeField] private float healAmount = 30f;
        
        public void Initialize(CardData data)
        {
            cardData = data;
            healAmount = data.effects.Length > 0 ? data.effects[0].value : healAmount;
        }
        
        protected override void InitializeStrategies()
        {
            // Add self-targeting strategy (heals self or ally)
            _strategyExecutor.AddStrategy(new SelfTargetingStrategy());
            
            // Add heal effect strategy
            _strategyExecutor.AddStrategy(new HealEffectStrategy(healAmount));
            
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
                // Show heal preview
                PreviewManager.Instance?.ShowHealPreview(context.target.transform.position, healAmount);
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