using System;

namespace Core.Patterns
{
    /// <summary>
    /// Base class for commands
    /// </summary>
    public abstract class ACommandBase : ICommand
    {
        protected string description;
        protected DateTime timestamp;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public ACommandBase(string description)
        {
            this.description = description;
            this.timestamp = DateTime.Now;
        }
        
        /// <summary>
        /// Execute the command
        /// </summary>
        public abstract void Execute();
        
        /// <summary>
        /// Undo the command
        /// </summary>
        public abstract void Undo();
        
        /// <summary>
        /// Get a description of the command
        /// </summary>
        public virtual string GetDescription()
        {
            return description;
        }
        
        /// <summary>
        /// Get timestamp of when the command was executed
        /// </summary>
        public DateTime Timestamp => timestamp;
    }
}