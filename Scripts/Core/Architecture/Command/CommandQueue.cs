using System;
using System.Collections.Generic;
using UnityEngine;

namespace RavenDeckbuilding.Core.Architecture.Command
{
    /// <summary>
    /// Generic command queue with priority and execution budgeting
    /// </summary>
    public class CommandQueue<TContext, TCommand> where TCommand : ICommand<TContext>
    {
        private struct CommandEntry : IComparable<CommandEntry>
        {
            public TCommand command;
            public float timestamp;
            
            public int CompareTo(CommandEntry other)
            {
                // Higher priority first, then by timestamp
                int priorityComparison = other.command.Priority.CompareTo(command.Priority);
                return priorityComparison != 0 ? priorityComparison : timestamp.CompareTo(other.timestamp);
            }
        }
        
        private SortedSet<CommandEntry> _commandQueue;
        private Stack<TCommand> _undoStack;
        private float _frameExecutionBudget;
        private int _maxCommandsPerFrame;
        
        public CommandQueue(float executionBudgetMs = 2f, int maxCommandsPerFrame = 5)
        {
            _commandQueue = new SortedSet<CommandEntry>();
            _undoStack = new Stack<TCommand>();
            _frameExecutionBudget = executionBudgetMs;
            _maxCommandsPerFrame = maxCommandsPerFrame;
        }
        
        /// <summary>
        /// Enqueue command for execution
        /// </summary>
        public void Enqueue(TCommand command)
        {
            if (command == null) return;
            
            _commandQueue.Add(new CommandEntry
            {
                command = command,
                timestamp = Time.unscaledTime
            });
        }
        
        /// <summary>
        /// Execute commands within frame budget
        /// </summary>
        public void ExecuteCommands(TContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int executedCount = 0;
            
            while (_commandQueue.Count > 0 && 
                   executedCount < _maxCommandsPerFrame &&
                   stopwatch.ElapsedMilliseconds < _frameExecutionBudget)
            {
                var entry = _commandQueue.Min;
                _commandQueue.Remove(entry);
                
                if (entry.command.CanExecute(context))
                {
                    try
                    {
                        entry.command.Execute(context);
                        
                        // Add to undo stack if supported
                        if (entry.command.CanUndo())
                        {
                            _undoStack.Push(entry.command);
                        }
                        
                        executedCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Command execution error: {ex.Message}");
                    }
                }
            }
            
            stopwatch.Stop();
            
            // Log performance warnings
            if (stopwatch.ElapsedMilliseconds > _frameExecutionBudget)
            {
                Debug.LogWarning($"Command execution exceeded budget: {stopwatch.ElapsedMilliseconds}ms");
            }
        }
        
        /// <summary>
        /// Undo last command
        /// </summary>
        public bool UndoLastCommand(TContext context)
        {
            if (_undoStack.Count > 0)
            {
                var command = _undoStack.Pop();
                if (command.CanUndo())
                {
                    try
                    {
                        command.Undo(context);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Command undo error: {ex.Message}");
                    }
                }
            }
            return false;
        }
        
        /// <summary>
        /// Clear all queued commands
        /// </summary>
        public void Clear()
        {
            _commandQueue.Clear();
        }
        
        /// <summary>
        /// Clear undo history
        /// </summary>
        public void ClearUndoHistory()
        {
            _undoStack.Clear();
        }
        
        public int QueuedCommandCount => _commandQueue.Count;
        public int UndoStackCount => _undoStack.Count;
    }
}