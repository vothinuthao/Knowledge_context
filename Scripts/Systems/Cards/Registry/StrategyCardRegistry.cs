using RavenDeckbuilding.Core.Architecture.Registry;

namespace RavenDeckbuilding.Systems.Cards
{
    /// <summary>
    /// Registry for managing all cards using Registry pattern
    /// </summary>
    public class StrategyCardRegistry : BaseRegistry<string, StrategyBaseCard>
    {
        public static StrategyCardRegistry Instance { get; private set; }
        
        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            base.Awake();
        }
        
        /// <summary>
        /// Register card automatically
        /// </summary>
        public void RegisterCard(StrategyBaseCard card)
        {
            if (card != null && !string.IsNullOrEmpty(card.CardId))
            {
                Register(card.CardId, card);
            }
        }
        
        /// <summary>
        /// Unregister card automatically
        /// </summary>
        public void UnregisterCard(StrategyBaseCard card)
        {
            if (card != null && !string.IsNullOrEmpty(card.CardId))
            {
                Unregister(card.CardId);
            }
        }
        
        /// <summary>
        /// Get card by ID with type casting
        /// </summary>
        public T GetCard<T>(string cardId) where T : StrategyBaseCard
        {
            return Get(cardId) as T;
        }
        
        /// <summary>
        /// Get all cards of specific type
        /// </summary>
        public System.Collections.Generic.IEnumerable<T> GetCardsOfType<T>() where T : StrategyBaseCard
        {
            foreach (var card in GetAll())
            {
                if (card is T typedCard)
                    yield return typedCard;
            }
        }
    }
}