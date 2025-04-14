using Troops.Base;
namespace Core.Patterns
{
    /// <summary>
    /// Interface for all troop behavior strategies
    /// </summary>
    public interface ITroopBehaviorStrategy
    {
        /// <summary>
        /// Check if this strategy should be executed
        /// </summary>
        bool ShouldExecute(TroopBase troop);
        
        /// <summary>
        /// Execute the behavior strategy
        /// </summary>
        void Execute(TroopBase troop);
        
        /// <summary>
        /// Get the name of this strategy
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Get the priority of this strategy (higher values = higher priority)
        /// </summary>
        int Priority { get; }
    }
}