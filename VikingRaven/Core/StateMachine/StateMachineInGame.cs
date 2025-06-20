using System;
using System.Collections.Generic;
using UnityEngine;

namespace VikingRaven.Core.StateMachine
{
    /// <summary>
    /// Pure C# implementation of state machine for game entities
    /// No longer depends on MonoBehaviour for better performance and flexibility
    /// </summary>
    public class StateMachineInGame : IStateMachine
    {
        #region Private Fields
        
        private IState _currentState;
        private IState _previousState;
        private Dictionary<Type, IState> _states = new Dictionary<Type, IState>();
        private readonly int _entityId;
        private bool _isActive = true;
        
        #endregion

        #region Public Properties

        public IState CurrentState => _currentState;
        public IState PreviousState => _previousState;
        public bool IsActive => _isActive;
        public int RegisteredStatesCount => _states.Count;
        public int EntityId => _entityId;

        #endregion

        #region Events

        public event Action<IState, IState> OnStateChanged;
        public event Action<IState> OnStateEntered;
        public event Action<IState> OnStateExited;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize state machine with entity identifier
        /// </summary>
        /// <param name="entityId">Unique identifier for the entity using this state machine</param>
        public StateMachineInGame(int entityId = 0)
        {
            _entityId = entityId;
            
        }

        #endregion

        #region State Registration

        /// <summary>
        /// Register a state in the state machine
        /// </summary>
        /// <typeparam name="T">Type of state to register</typeparam>
        /// <param name="state">State instance to register</param>
        public void RegisterState<T>(T state) where T : IState
        {
            var type = typeof(T);
            
            if (_states.ContainsKey(type))
            {
                Debug.LogWarning($"StateMachineInGame: State of type {type.Name} is already registered for entity {_entityId}. Overriding.");
            }
            
            _states[type] = state;
            
            Debug.Log($"StateMachineInGame: Registered state {type.Name} for entity {_entityId}");
        }

        /// <summary>
        /// Unregister a state from the state machine
        /// </summary>
        /// <typeparam name="T">Type of state to unregister</typeparam>
        public bool UnregisterState<T>() where T : class, IState
        {
            var type = typeof(T);
            
            if (_states.ContainsKey(type))
            {
                // Exit current state if it's the one being unregistered
                if (_currentState != null && _currentState.GetType() == type)
                {
                    _currentState.Exit();
                    _currentState = null;
                }
                
                // Remove from previous state if it's the one being unregistered
                if (_previousState != null && _previousState.GetType() == type)
                {
                    _previousState = null;
                }
                
                _states.Remove(type);
                Debug.Log($"StateMachineInGame: Unregistered state {type.Name} for entity {_entityId}");
                return true;
            }
            
            return false;
        }

        #endregion

        #region State Retrieval

        /// <summary>
        /// Get a registered state of specific type
        /// </summary>
        /// <typeparam name="T">Type of state to retrieve</typeparam>
        /// <returns>State instance or null if not found</returns>
        public T GetState<T>() where T : class, IState
        {
            var type = typeof(T);
            
            if (_states.TryGetValue(type, out var state))
            {
                return state as T;
            }
            
            Debug.LogWarning($"StateMachineInGame: State of type {type.Name} not found for entity {_entityId}");
            return null;
        }

        /// <summary>
        /// Check if a state type is registered
        /// </summary>
        /// <typeparam name="T">Type of state to check</typeparam>
        /// <returns>True if state is registered</returns>
        public bool HasState<T>() where T : class, IState
        {
            return _states.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Get all registered state types
        /// </summary>
        /// <returns>Array of registered state types</returns>
        public Type[] GetRegisteredStateTypes()
        {
            var types = new Type[_states.Count];
            _states.Keys.CopyTo(types, 0);
            return types;
        }

        #endregion

        #region State Transitions

        /// <summary>
        /// Change to a new state instance
        /// </summary>
        /// <param name="newState">Target state to transition to</param>
        public void ChangeState(IState newState)
        {
            if (!_isActive)
            {
                Debug.LogWarning($"StateMachineInGame: Cannot change state - state machine is inactive for entity {_entityId}");
                return;
            }

            if (newState == _currentState)
            {
                Debug.Log($"StateMachineInGame: Already in state {newState?.GetType().Name} for entity {_entityId}");
                return;
            }

            var previousState = _currentState;
            
            // Exit current state
            _currentState?.Exit();
            OnStateExited?.Invoke(_currentState);
            
            // Update state references
            _previousState = previousState;
            _currentState = newState;
            
            // Enter new state
            _currentState?.Enter();
            OnStateEntered?.Invoke(_currentState);
            
            // Notify state change
            OnStateChanged?.Invoke(previousState, _currentState);
            
            Debug.Log($"StateMachineInGame: State changed from {previousState?.GetType().Name ?? "null"} to {_currentState?.GetType().Name ?? "null"} for entity {_entityId}");
        }

        /// <summary>
        /// Change to a state by type
        /// </summary>
        /// <typeparam name="T">Type of state to change to</typeparam>
        public void ChangeState<T>() where T : class, IState
        {
            var type = typeof(T);
            if (_states.TryGetValue(type, out var state))
            {
                ChangeState(state);
            }
            else
            {
                Debug.LogError($"StateMachineInGame: Failed to change state - State of type {type.Name} is not registered for entity {_entityId}");
            }
        }

        /// <summary>
        /// Revert to the previous state
        /// </summary>
        public void RevertToPreviousState()
        {
            if (_previousState != null)
            {
                Debug.Log($"StateMachineInGame: Reverting to previous state {_previousState.GetType().Name} for entity {_entityId}");
                ChangeState(_previousState);
            }
            else
            {
                Debug.LogWarning($"StateMachineInGame: No previous state to revert to for entity {_entityId}");
            }
        }

        #endregion

        #region State Machine Control

        /// <summary>
        /// Update the state machine - call this from Update() method
        /// </summary>
        public void Update()
        {
            if (!_isActive) return;
            
            _currentState?.Execute();
        }

        /// <summary>
        /// Update the state machine with delta time
        /// </summary>
        /// <param name="deltaTime">Time since last update</param>
        public void Update(float deltaTime)
        {
            if (!_isActive) return;
            
            // Some states might need delta time information
            _currentState?.Execute();
        }

        /// <summary>
        /// Pause the state machine
        /// </summary>
        public void Pause()
        {
            _isActive = false;
            Debug.Log($"StateMachineInGame: Paused state machine for entity {_entityId}");
        }

        /// <summary>
        /// Resume the state machine
        /// </summary>
        public void Resume()
        {
            _isActive = true;
            Debug.Log($"StateMachineInGame: Resumed state machine for entity {_entityId}");
        }

        /// <summary>
        /// Reset the state machine to no current state
        /// </summary>
        public void Reset()
        {
            _currentState?.Exit();
            OnStateExited?.Invoke(_currentState);
            
            _currentState = null;
            _previousState = null;
            
            Debug.Log($"StateMachineInGame: Reset state machine for entity {_entityId}");
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clean up the state machine and all states
        /// </summary>
        public void Cleanup()
        {
            // Exit current state
            _currentState?.Exit();
            
            // Clear all state references
            _currentState = null;
            _previousState = null;
            _states.Clear();
            
            // Clear events
            OnStateChanged = null;
            OnStateEntered = null;
            OnStateExited = null;
            
            _isActive = false;
            
            Debug.Log($"StateMachineInGame: Cleaned up state machine for entity {_entityId}");
        }

        #endregion

        #region Debug and Utility

        /// <summary>
        /// Get current state machine information
        /// </summary>
        /// <returns>Debug information string</returns>
        public string GetDebugInfo()
        {
            return $"StateMachine [{_entityId}] - Current: {_currentState?.GetType().Name ?? "null"}, " +
                   $"Previous: {_previousState?.GetType().Name ?? "null"}, " +
                   $"States: {_states.Count}, Active: {_isActive}";
        }

        /// <summary>
        /// Log current state machine status
        /// </summary>
        public void LogStatus()
        {
            Debug.Log($"=== StateMachine Status [{_entityId}] ===");
            Debug.Log($"Current State: {_currentState?.GetType().Name ?? "None"}");
            Debug.Log($"Previous State: {_previousState?.GetType().Name ?? "None"}");
            Debug.Log($"Registered States: {_states.Count}");
            Debug.Log($"Active: {_isActive}");
            
            if (_states.Count > 0)
            {
                Debug.Log("Registered State Types:");
                foreach (var stateType in _states.Keys)
                {
                    Debug.Log($"  - {stateType.Name}");
                }
            }
        }

        #endregion
    }
}