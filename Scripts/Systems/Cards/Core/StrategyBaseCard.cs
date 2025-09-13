using UnityEngine;
using System.Linq;
using RavenDeckbuilding.Core.Architecture.Strategy;
using RavenDeckbuilding.Core.Architecture.Command;
using RavenDeckbuilding.Core.Architecture.Pooling;

namespace RavenDeckbuilding.Systems.Cards
{
    /// <summary>
    /// Base card class using Strategy pattern for behaviors
    /// </summary>
    public abstract class StrategyBaseCard : MonoBehaviour, IPoolable
    {
        [Header("Card Data")]
        [SerializeField] protected CardData cardData;
        
        [Header("Runtime")]
        [SerializeField] protected CardState currentState = CardState.InHand;
        
        // Strategy executor for card behaviors
        protected StrategyExecutor<CardExecutionContext, ICardStrategy> _strategyExecutor;
        
        // Card properties
        public string CardName => cardData?.cardName ?? "Unknown";
        public string CardId => cardData?.cardId ?? "unknown";
        public CardData Data => cardData;
        public CardState State => currentState;
        
        // Events
        public System.Action<StrategyBaseCard, CardState, CardState> OnStateChanged;
        
        protected virtual void Awake()
        {
            _strategyExecutor = new StrategyExecutor<CardExecutionContext, ICardStrategy>();
            InitializeStrategies();
        }
        
        /// <summary>
        /// Override to add card-specific strategies
        /// </summary>
        protected abstract void InitializeStrategies();
        
        /// <summary>
        /// Check if card can be cast
        /// </summary>
        public virtual bool CanCast(CardExecutionContext context)
        {
            if (currentState != CardState.Ready) return false;
            if (context.caster.CurrentMana < cardData.manaCost) return false;
            
            var validStrategies = _strategyExecutor.GetValidStrategies(context);
            return validStrategies.Any();
        }
        
        /// <summary>
        /// Create command for casting this card
        /// </summary>
        public virtual ICommand<CardExecutionContext> CreateCastCommand(CardExecutionContext context)
        {
            return new CastCardCommand(this, context);
        }
        
        /// <summary>
        /// Execute card strategies
        /// </summary>
        public virtual void ExecuteCard(CardExecutionContext context)
        {
            if (!CanCast(context)) return;
            
            ChangeState(CardState.Casting);
            _strategyExecutor.ExecuteAll(context);
            ChangeState(CardState.OnCooldown);
            
            StartCoroutine(CooldownCoroutine());
        }
        
        /// <summary>
        /// Show preview of card effects
        /// </summary>
        public abstract void ShowPreview(CardExecutionContext context);
        
        /// <summary>
        /// Hide preview
        /// </summary>
        public abstract void HidePreview();
        
        protected virtual void ChangeState(CardState newState)
        {
            var oldState = currentState;
            currentState = newState;
            OnStateChanged?.Invoke(this, oldState, newState);
        }
        
        protected virtual System.Collections.IEnumerator CooldownCoroutine()
        {
            yield return new WaitForSeconds(cardData.cooldown);
            ChangeState(CardState.Ready);
        }
        
        // IPoolable implementation
        public virtual void OnPoolGet()
        {
            gameObject.SetActive(true);
            currentState = CardState.InHand;
        }
        
        public virtual void OnPoolReturn()
        {
            gameObject.SetActive(false);
            HidePreview();
            currentState = CardState.InHand;
        }
        
        public virtual bool IsAvailableForPool => currentState == CardState.Exhausted || !gameObject.activeInHierarchy;
    }
    
    public enum CardState
    {
        InHand,
        Ready,
        Casting,
        OnCooldown,
        Exhausted
    }
}