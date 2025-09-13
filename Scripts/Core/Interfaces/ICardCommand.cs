using RavenDeckbuilding.Core.Data;

namespace RavenDeckbuilding.Core.Interfaces
{
    public enum CommandResult
    {
        Success,
        Failed,
        Blocked,
        InvalidTarget,
        InsufficientResources,
        OnCooldown,
        Cancelled
    }

    /// <summary>
    /// Interface for all executable card commands with rollback capability
    /// Ensures reliable, ordered execution of all card abilities
    /// </summary>
    public interface ICardCommand
    {
        /// <summary>
        /// Unique sequence identifier for command ordering
        /// </summary>
        uint SequenceId { get; }
        
        /// <summary>
        /// Command execution priority (higher values execute first)
        /// Use for interrupt mechanics and priority systems
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Timestamp when command was created
        /// </summary>
        float Timestamp { get; }
        
        /// <summary>
        /// Estimated execution time in seconds for frame budgeting
        /// Should be as accurate as possible for smooth performance
        /// </summary>
        float EstimatedExecutionTime { get; }
        
        /// <summary>
        /// Check if command can be executed in current game state
        /// Must be fast (<0.1ms) and have no side effects
        /// </summary>
        bool CanExecute(GameState gameState);
        
        /// <summary>
        /// Execute the command and return result
        /// Should complete within EstimatedExecutionTime
        /// </summary>
        CommandResult Execute(GameState gameState);
        
        /// <summary>
        /// Rollback the command effects if execution needs to be undone
        /// Must be able to restore previous state exactly
        /// </summary>
        void Rollback(GameState gameState);
        
        /// <summary>
        /// Optional validation before adding to command queue
        /// Used for early rejection of invalid commands
        /// </summary>
        bool IsValid();
        
        /// <summary>
        /// Optional cleanup when command is discarded or completed
        /// </summary>
        void Dispose();
    }
    
    /// <summary>
    /// Base implementation providing common command functionality
    /// </summary>
    public abstract class BaseCardCommand : ICardCommand
    {
        private static uint _sequenceCounter = 0;
        
        public uint SequenceId { get; private set; }
        public abstract int Priority { get; }
        public float Timestamp { get; private set; }
        public abstract float EstimatedExecutionTime { get; }
        
        protected BaseCardCommand()
        {
            SequenceId = ++_sequenceCounter;
            Timestamp = UnityEngine.Time.realtimeSinceStartup;
        }
        
        public abstract bool CanExecute(GameState gameState);
        public abstract CommandResult Execute(GameState gameState);
        public abstract void Rollback(GameState gameState);
        
        public virtual bool IsValid() => true;
        public virtual void Dispose() { }
        
        public override string ToString()
        {
            return $"{GetType().Name}[{SequenceId}] Priority:{Priority} Time:{Timestamp:F3}";
        }
    }
}