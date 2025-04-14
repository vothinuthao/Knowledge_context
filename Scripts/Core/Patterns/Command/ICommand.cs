using System;

namespace Core.Patterns
{
    /// <summary>
    /// Interface for command pattern
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Execute the command
        /// </summary>
        void Execute();
        
        /// <summary>
        /// Undo the command
        /// </summary>
        void Undo();
        
        /// <summary>
        /// Get a description of the command
        /// </summary>
        string GetDescription();
        
        /// <summary>
        /// Get timestamp of when the command was executed
        /// </summary>
        DateTime Timestamp { get; }
    }
}