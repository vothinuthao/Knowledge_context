using RavenDeckbuilding.Core.Architecture.Strategy;

namespace RavenDeckbuilding.Systems.Cards
{
    /// <summary>
    /// Strategy interface for card behaviors
    /// </summary>
    public interface ICardStrategy : IStrategy<CardExecutionContext>
    {
        string StrategyName { get; }
        CardStrategyCategory Category { get; }
    }
    
    public enum CardStrategyCategory
    {
        Targeting,
        Effect,
        Visual,
        Audio
    }
}