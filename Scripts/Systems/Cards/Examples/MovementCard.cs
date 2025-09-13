using System;
using RavenDeckbuilding.Systems.Cards.Strategies;
using UnityEngine;

namespace RavenDeckbuilding.Systems.Cards
{
    /// <summary>
    /// Example movement card implementation
    /// </summary>
    public class MovementCard : StrategyBaseCard
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveDistance = 5f;
        [SerializeField] private float moveSpeed = 10f;
        
        public void Initialize(CardData data)
        {
            cardData = data;
            moveDistance = data.effects.Length > 0 ? data.effects[0].value : moveDistance;
            moveSpeed = data.effects.Length > 1 ? data.effects[1].value : moveSpeed;
        }
        
        protected override void InitializeStrategies()
        {
            // Add position targeting strategy
            _strategyExecutor.AddStrategy(new PositionTargetingStrategy());
            
            // Add movement effect strategy
            _strategyExecutor.AddStrategy(new MovementEffectStrategy(moveDistance, moveSpeed));
            
            // Add visual effect strategy
            if (cardData?.impactEffectPrefab != null)
                _strategyExecutor.AddStrategy(new VFXStrategy(cardData.impactEffectPrefab));
            
            // Add audio strategy
            if (cardData?.castSound != null)
                _strategyExecutor.AddStrategy(new AudioStrategy(cardData.castSound));
        }
        
        public override void ShowPreview(CardExecutionContext context)
        {
            if (context.targetPosition != Vector3.zero)
            {
                // Show movement path preview
                ShowMovementPreview(context.caster.transform.position, context.targetPosition);
            }
        }
        
        [Obsolete("Obsolete")]
        public override void HidePreview()
        {
            PreviewManager.Instance?.HideAllPreviews();
        }
        
        private void ShowMovementPreview(Vector3 start, Vector3 end)
        {
            // Simple line renderer preview for movement
            Debug.DrawLine(start, end, Color.yellow, 1f);
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