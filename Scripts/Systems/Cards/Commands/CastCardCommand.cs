using RavenDeckbuilding.Core.Architecture.Command;
using UnityEngine;

namespace RavenDeckbuilding.Systems.Cards
{
    /// <summary>
    /// Command for casting a card
    /// </summary>
    public class CastCardCommand : ICommand<CardExecutionContext>
    {
        private StrategyBaseCard _card;
        private CardExecutionContext _context;
        private bool _executed = false;
        
        public float EstimatedExecutionTime => 0.5f; // 0.5ms estimated
        public int Priority => 100; // Normal priority
        
        public CastCardCommand(StrategyBaseCard card, CardExecutionContext context)
        {
            _card = card;
            _context = context;
        }
        
        public bool CanExecute(CardExecutionContext context)
        {
            return _card != null && _card.CanCast(context);
        }
        
        public void Execute(CardExecutionContext context)
        {
            if (CanExecute(context))
            {
                _card.ExecuteCard(context);
                _executed = true;
            }
        }
        
        public bool CanUndo()
        {
            return false; // Card casting typically cannot be undone
        }
        
        public void Undo(CardExecutionContext context)
        {
            // Most card effects cannot be undone
            Debug.LogWarning("Card casting cannot be undone");
        }
    }
}