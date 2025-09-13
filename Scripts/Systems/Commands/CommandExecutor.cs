using UnityEngine;
using RavenDeckbuilding.Core.Interfaces;
using RavenDeckbuilding.Core.Data;
using RavenDeckbuilding.Systems.Events;
using System.Collections.Generic;

namespace RavenDeckbuilding.Systems.Commands
{
    /// <summary>
    /// Frame-budget aware command processor that ensures smooth performance
    /// Executes maximum 5 commands per frame within 2ms budget
    /// </summary>
    public class CommandExecutor : MonoBehaviour
    {
        [Header("Performance Configuration")]
        [SerializeField] private float frameBudgetMs = 2f; // 2ms frame budget
        [SerializeField] private int maxCommandsPerFrame = 5;
        [SerializeField] private float rollbackTimeoutMs = 10f; // Max time to wait for rollback
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = false;
        [SerializeField] private bool showPerformanceStats = false;
        [SerializeField] private bool validateCommands = true;

        // Core systems
        private CommandPriorityQueue _commandQueue;
        private GameState _gameState;
        
        // Execution tracking
        private readonly List<ICardCommand> _executingCommands = new List<ICardCommand>();
        private readonly List<ICardCommand> _completedCommands = new List<ICardCommand>();
        private readonly List<ICardCommand> _failedCommands = new List<ICardCommand>();
        
        // Performance monitoring
        private float _totalExecutionTime;
        private int _commandsExecutedThisFrame;
        private int _totalCommandsExecuted;
        private float _averageExecutionTime;
        private int _frameCount;
        
        // Rollback system
        private readonly Stack<ICardCommand> _rollbackStack = new Stack<ICardCommand>();
        private bool _isRollbackPending;
        private float _rollbackStartTime;
        
        // Pre-allocated arrays for zero allocation
        private readonly ICardCommand[] _executableCommands = new ICardCommand[16];
        private readonly ICardCommand[] _tempCommands = new ICardCommand[16];

        public bool IsProcessingCommands => _commandsExecutedThisFrame > 0;
        public int QueuedCommandCount => _commandQueue?.Count ?? 0;
        public int ExecutedCommandCount => _totalCommandsExecuted;
        public float AverageExecutionTime => _averageExecutionTime;

        private void Awake()
        {
            _commandQueue = new CommandPriorityQueue();
            _gameState = FindObjectOfType<GameState>();
            
            if (_gameState == null)
            {
                Debug.LogError("CommandExecutor: GameState not found in scene!");
            }
        }

        private void Update()
        {
            if (_gameState == null) return;

            float frameStartTime = Time.realtimeSinceStartup;
            float frameBudgetSeconds = frameBudgetMs / 1000f;
            
            _commandsExecutedThisFrame = 0;
            
            // Handle rollback timeout
            if (_isRollbackPending)
            {
                HandleRollbackTimeout(frameStartTime);
            }
            
            // Execute commands within frame budget
            ExecuteCommandsWithBudget(frameStartTime, frameBudgetSeconds);
            
            // Update performance statistics
            _totalExecutionTime += (Time.realtimeSinceStartup - frameStartTime);
            
            if (showPerformanceStats)
                UpdatePerformanceStats();
        }

        private void ExecuteCommandsWithBudget(float frameStartTime, float budgetSeconds)
        {
            // Get executable commands within time budget
            int executableCount = _commandQueue.GetExecutableCommands(
                _executableCommands, 
                Mathf.Min(maxCommandsPerFrame, _executableCommands.Length), 
                budgetSeconds
            );
            
            for (int i = 0; i < executableCount; i++)
            {
                if (Time.realtimeSinceStartup - frameStartTime >= budgetSeconds)
                {
                    if (enableDebugLogging)
                        Debug.Log($"Frame budget exceeded, stopping execution. Executed {_commandsExecutedThisFrame} commands");
                    break;
                }
                
                ICardCommand command = _commandQueue.Dequeue();
                if (command != null)
                {
                    ExecuteSingleCommand(command);
                    _commandsExecutedThisFrame++;
                    
                    if (_commandsExecutedThisFrame >= maxCommandsPerFrame)
                    {
                        if (enableDebugLogging)
                            Debug.Log("Max commands per frame reached");
                        break;
                    }
                }
            }
        }

        private void ExecuteSingleCommand(ICardCommand command)
        {
            float executionStartTime = Time.realtimeSinceStartup;
            
            try
            {
                // Pre-execution validation
                if (validateCommands && !command.IsValid())
                {
                    HandleFailedCommand(command, CommandResult.Failed, "Command validation failed");
                    return;
                }
                
                // Check if command can execute in current state
                if (!command.CanExecute(_gameState))
                {
                    HandleFailedCommand(command, CommandResult.Blocked, "Command cannot execute in current state");
                    return;
                }
                
                // Add to executing list for tracking
                _executingCommands.Add(command);
                
                // Fire pre-execution event
                GameEventBus.FireCommandExecutionStarted(command);
                
                // Execute the command
                CommandResult result = command.Execute(_gameState);
                
                // Handle result
                HandleCommandResult(command, result);
                
                // Track performance
                float executionTime = Time.realtimeSinceStartup - executionStartTime;
                UpdateExecutionStats(executionTime);
                
                if (enableDebugLogging)
                {
                    Debug.Log($"Executed {command.GetType().Name}[{command.SequenceId}] " +
                             $"in {executionTime * 1000:F2}ms with result: {result}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Exception executing command {command.GetType().Name}: {ex.Message}");
                HandleFailedCommand(command, CommandResult.Failed, ex.Message);
            }
            finally
            {
                // Remove from executing list
                _executingCommands.Remove(command);
            }
        }

        private void HandleCommandResult(ICardCommand command, CommandResult result)
        {
            switch (result)
            {
                case CommandResult.Success:
                    _completedCommands.Add(command);
                    _rollbackStack.Push(command); // For potential rollback
                    GameEventBus.FireCommandExecutionCompleted(command, result);
                    _totalCommandsExecuted++;
                    break;
                    
                case CommandResult.Failed:
                case CommandResult.Blocked:
                case CommandResult.InvalidTarget:
                case CommandResult.InsufficientResources:
                case CommandResult.OnCooldown:
                    HandleFailedCommand(command, result, $"Command failed with result: {result}");
                    break;
                    
                case CommandResult.Cancelled:
                    GameEventBus.FireCommandCancelled(command);
                    command.Dispose();
                    break;
            }
        }

        private void HandleFailedCommand(ICardCommand command, CommandResult result, string reason)
        {
            _failedCommands.Add(command);
            GameEventBus.FireCommandExecutionFailed(command, result, reason);
            
            if (enableDebugLogging)
                Debug.LogWarning($"Command {command.GetType().Name}[{command.SequenceId}] failed: {reason}");
                
            command.Dispose();
        }

        private void HandleRollbackTimeout(float currentTime)
        {
            if (currentTime - _rollbackStartTime > rollbackTimeoutMs / 1000f)
            {
                Debug.LogError("Rollback timeout exceeded, clearing rollback stack");
                _rollbackStack.Clear();
                _isRollbackPending = false;
            }
        }

        // Public API
        public bool EnqueueCommand(ICardCommand command)
        {
            if (command == null) return false;
            
            if (validateCommands && !command.IsValid())
            {
                Debug.LogWarning($"Rejecting invalid command: {command.GetType().Name}");
                command.Dispose();
                return false;
            }
            
            bool success = _commandQueue.Enqueue(command);
            
            if (success)
            {
                GameEventBus.FireCommandEnqueued(command);
                
                if (enableDebugLogging)
                    Debug.Log($"Enqueued command: {command.GetType().Name}[{command.SequenceId}] Priority: {command.Priority}");
            }
            else
            {
                Debug.LogError("Failed to enqueue command - queue may be full");
                command.Dispose();
            }
            
            return success;
        }

        public bool CancelCommand(uint sequenceId)
        {
            // Try to remove from queue first
            if (_commandQueue.RemoveCommand(sequenceId))
            {
                if (enableDebugLogging)
                    Debug.Log($"Cancelled queued command with sequence ID: {sequenceId}");
                return true;
            }
            
            // Check if currently executing
            for (int i = 0; i < _executingCommands.Count; i++)
            {
                if (_executingCommands[i].SequenceId == sequenceId)
                {
                    // Mark for cancellation (will be handled in next frame)
                    Debug.LogWarning($"Cannot cancel currently executing command: {sequenceId}");
                    return false;
                }
            }
            
            return false;
        }

        public void InitiateRollback(int commandsToRollback = 1)
        {
            if (_rollbackStack.Count == 0)
            {
                Debug.LogWarning("No commands available for rollback");
                return;
            }
            
            _isRollbackPending = true;
            _rollbackStartTime = Time.realtimeSinceStartup;
            
            int rollbackCount = Mathf.Min(commandsToRollback, _rollbackStack.Count);
            
            for (int i = 0; i < rollbackCount; i++)
            {
                if (_rollbackStack.Count > 0)
                {
                    ICardCommand command = _rollbackStack.Pop();
                    try
                    {
                        command.Rollback(_gameState);
                        GameEventBus.FireCommandRolledBack(command);
                        
                        if (enableDebugLogging)
                            Debug.Log($"Rolled back command: {command.GetType().Name}[{command.SequenceId}]");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error rolling back command {command.GetType().Name}: {ex.Message}");
                    }
                    finally
                    {
                        command.Dispose();
                    }
                }
            }
            
            _isRollbackPending = false;
        }

        public void ClearQueue()
        {
            _commandQueue.Clear();
            _rollbackStack.Clear();
            
            // Dispose executing commands
            foreach (var cmd in _executingCommands)
            {
                cmd.Dispose();
            }
            _executingCommands.Clear();
            
            if (enableDebugLogging)
                Debug.Log("Command queue and execution state cleared");
        }

        private void UpdateExecutionStats(float executionTime)
        {
            _frameCount++;
            float alpha = 1f / _frameCount;
            _averageExecutionTime = Mathf.Lerp(_averageExecutionTime, executionTime, alpha);
        }

        private void UpdatePerformanceStats()
        {
            if (_frameCount % 60 == 0) // Log every 60 frames
            {
                float queueUtilization = _commandQueue.GetUtilization();
                float avgFrameTime = _totalExecutionTime / _frameCount * 1000f; // ms
                
                Debug.Log($"Command Executor Performance - " +
                         $"Queue: {QueuedCommandCount} ({queueUtilization * 100:F1}%), " +
                         $"Avg Execution: {_averageExecutionTime * 1000:F2}ms, " +
                         $"Frame Time: {avgFrameTime:F2}ms, " +
                         $"Total Executed: {_totalCommandsExecuted}");
            }
        }

        // Debug GUI
        private void OnGUI()
        {
            if (!showPerformanceStats) return;

            GUILayout.BeginArea(new Rect(10, 200, 350, 200));
            GUILayout.Label("=== Command Executor Stats ===");
            GUILayout.Label($"Queue: {QueuedCommandCount}");
            GUILayout.Label($"Executing: {_executingCommands.Count}");
            GUILayout.Label($"Completed: {_completedCommands.Count}");
            GUILayout.Label($"Failed: {_failedCommands.Count}");
            GUILayout.Label($"Avg Execution: {_averageExecutionTime * 1000:F2}ms");
            GUILayout.Label($"Commands/Frame: {_commandsExecutedThisFrame}");
            GUILayout.Label($"Total Executed: {_totalCommandsExecuted}");
            GUILayout.Label($"Rollback Stack: {_rollbackStack.Count}");
            
            if (_commandQueue.Count > 0)
            {
                var next = _commandQueue.Peek();
                GUILayout.Label($"Next: {next?.GetType().Name}");
            }
            
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            ClearQueue();
        }
    }
}