using System.Collections.Generic;
using UnityEngine;

namespace Core.Patterns
{
    /// <summary>
    /// Command invoker for managing command execution and history
    /// </summary>
    public class GenericCommandInvoker : MonoBehaviourSingleton<GenericCommandInvoker>
    {
        [SerializeField] private int maxHistorySize = 50;
        [SerializeField] private bool enabledLogging = true;
        
        // Command history
        protected Stack<ICommand> commandHistory = new Stack<ICommand>();
        protected Stack<ICommand> undoneCommands = new Stack<ICommand>();
        
        // Events
        public delegate void CommandEvent(ICommand command);
        public event CommandEvent OnCommandExecuted;
        public event CommandEvent OnCommandUndone;
        public event CommandEvent OnCommandRedone;
        
        /// <summary>
        /// Execute a command and add it to the history
        /// </summary>
        public virtual void ExecuteCommand(ICommand command)
        {
            command.Execute();
            
            // Add to history
            commandHistory.Push(command);
            
            // Clear undone commands
            undoneCommands.Clear();
            
            // Trigger event
            OnCommandExecuted?.Invoke(command);
            
            // Log if enabled
            if (enabledLogging)
            {
                Debug.Log($"[CommandInvoker] Executed: {command.GetDescription()}");
            }
            
            // Limit history size
            EnsureHistorySizeLimit();
        }
        
        /// <summary>
        /// Undo the last command
        /// </summary>
        public virtual bool UndoCommand()
        {
            if (commandHistory.Count > 0)
            {
                ICommand command = commandHistory.Pop();
                command.Undo();
                
                // Add to undone commands
                undoneCommands.Push(command);
                
                // Trigger event
                OnCommandUndone?.Invoke(command);
                
                // Log if enabled
                if (enabledLogging)
                {
                    Debug.Log($"[CommandInvoker] Undone: {command.GetDescription()}");
                }
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Redo the last undone command
        /// </summary>
        public virtual bool RedoCommand()
        {
            if (undoneCommands.Count > 0)
            {
                ICommand command = undoneCommands.Pop();
                command.Execute();
                
                // Add back to history
                commandHistory.Push(command);
                
                // Trigger event
                OnCommandRedone?.Invoke(command);
                
                // Log if enabled
                if (enabledLogging)
                {
                    Debug.Log($"[CommandInvoker] Redone: {command.GetDescription()}");
                }
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Can undo?
        /// </summary>
        public bool CanUndo => commandHistory.Count > 0;
        
        /// <summary>
        /// Can redo?
        /// </summary>
        public bool CanRedo => undoneCommands.Count > 0;
        
        /// <summary>
        /// Clear all command history
        /// </summary>
        public virtual void ClearHistory()
        {
            commandHistory.Clear();
            undoneCommands.Clear();
            
            if (enabledLogging)
            {
                Debug.Log("[CommandInvoker] History cleared");
            }
        }
        
        /// <summary>
        /// Get the command history
        /// </summary>
        public IReadOnlyCollection<ICommand> CommandHistory => commandHistory;
        
        /// <summary>
        /// Get the undone commands
        /// </summary>
        public IReadOnlyCollection<ICommand> UndoneCommands => undoneCommands;
        
        /// <summary>
        /// Ensure the history size is within limits
        /// </summary>
        protected virtual void EnsureHistorySizeLimit()
        {
            // Limit history size
            if (commandHistory.Count > maxHistorySize)
            {
                // Remove oldest commands
                Stack<ICommand> tempStack = new Stack<ICommand>();
                
                int commandsToKeep = maxHistorySize;
                while (commandHistory.Count > 0 && commandsToKeep > 0)
                {
                    tempStack.Push(commandHistory.Pop());
                    commandsToKeep--;
                }
                
                // Clear the remaining old commands
                commandHistory.Clear();
                
                // Restore the commands we want to keep
                while (tempStack.Count > 0)
                {
                    commandHistory.Push(tempStack.Pop());
                }
            }
        }
    }
}