using System.Collections.Generic;

namespace Core.Patterns
{
    /// <summary>
    /// Composite command that groups multiple commands into one
    /// </summary>
    public class CompositeCommand : ACommandBase
    {
        protected List<ICommand> commands = new List<ICommand>();
        
        /// <summary>
        /// Constructor
        /// </summary>
        public CompositeCommand(string description) : base(description)
        {
        }
        
        /// <summary>
        /// Add a command to the composite
        /// </summary>
        public void AddCommand(ICommand command)
        {
            commands.Add(command);
        }
        
        /// <summary>
        /// Execute all commands
        /// </summary>
        public override void Execute()
        {
            foreach (var command in commands)
            {
                command.Execute();
            }
        }
        
        /// <summary>
        /// Undo all commands in reverse order
        /// </summary>
        public override void Undo()
        {
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                commands[i].Undo();
            }
        }
        
        /// <summary>
        /// Get a description of the composite command
        /// </summary>
        public override string GetDescription()
        {
            if (string.IsNullOrEmpty(description))
            {
                return $"Composite Command ({commands.Count} commands)";
            }
            
            return $"{description} ({commands.Count} commands)";
        }
    }
}