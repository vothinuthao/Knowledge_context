using UnityEngine;

namespace RavenDeckbuilding.Systems.Cards.Strategies
{
    /// <summary>
    /// Strategy for visual effects
    /// </summary>
    public class VFXStrategy : ICardStrategy
    {
        private GameObject _effectPrefab;
        
        public string StrategyName => "Visual Effects";
        public CardStrategyCategory Category => CardStrategyCategory.Visual;
        public int Priority => 100; // Low priority for visuals
        
        public VFXStrategy(GameObject effectPrefab)
        {
            _effectPrefab = effectPrefab;
        }
        
        public bool CanExecute(CardExecutionContext context)
        {
            return _effectPrefab != null;
        }
        
        public void Execute(CardExecutionContext context)
        {
            if (_effectPrefab != null)
            {
                Vector3 spawnPosition = context.target?.transform.position ?? context.targetPosition;
                GameObject effect = Object.Instantiate(_effectPrefab, spawnPosition, Quaternion.identity);
                
                // Auto-destroy effect after 5 seconds
                Object.Destroy(effect, 5f);
                
                Debug.Log($"Spawned VFX at {spawnPosition}");
            }
        }
    }
}