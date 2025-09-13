using UnityEngine;
using System.Collections;

namespace RavenDeckbuilding.Systems.Cards.Strategies
{
    /// <summary>
    /// Strategy for moving player to target position
    /// </summary>
    public class MovementEffectStrategy : ICardStrategy
    {
        private float _moveDistance;
        private float _moveSpeed;
        
        public string StrategyName => "Movement Effect";
        public CardStrategyCategory Category => CardStrategyCategory.Effect;
        public int Priority => 500; // Medium priority for effects
        
        public MovementEffectStrategy(float moveDistance, float moveSpeed)
        {
            _moveDistance = moveDistance;
            _moveSpeed = moveSpeed;
        }
        
        public bool CanExecute(CardExecutionContext context)
        {
            return context.caster != null && 
                   context.targetPosition != Vector3.zero && 
                   _moveDistance > 0 && 
                   _moveSpeed > 0;
        }
        
        public void Execute(CardExecutionContext context)
        {
            if (context.caster != null)
            {
                Vector3 startPosition = context.caster.transform.position;
                Vector3 direction = (context.targetPosition - startPosition).normalized;
                Vector3 targetPosition = startPosition + direction * _moveDistance;
                
                // Start movement coroutine
                context.caster.StartCoroutine(MoveToPosition(context.caster.transform, targetPosition));
                
                Debug.Log($"Moving {context.caster.name} to {targetPosition}");
            }
        }
        
        private IEnumerator MoveToPosition(Transform playerTransform, Vector3 targetPosition)
        {
            Vector3 startPosition = playerTransform.position;
            float journeyLength = Vector3.Distance(startPosition, targetPosition);
            float journeyTime = journeyLength / _moveSpeed;
            float elapsedTime = 0;
            
            while (elapsedTime < journeyTime)
            {
                elapsedTime += Time.deltaTime;
                float fractionOfJourney = elapsedTime / journeyTime;
                playerTransform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
                yield return null;
            }
            
            // Ensure we end exactly at the target
            playerTransform.position = targetPosition;
        }
    }
}