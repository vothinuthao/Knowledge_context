using System;
using System.Collections.Generic;
using UnityEngine;

namespace VikingRaven.Core.StateMachine
{
    public class StateMachineInGame : MonoBehaviour, IStateMachine
    {
        private IState _currentState;
        private IState _previousState;
        private Dictionary<Type, IState> _states = new Dictionary<Type, IState>();
        
        public IState CurrentState => _currentState;
        public IState PreviousState => _previousState;

        public void RegisterState<T>(T state) where T : IState
        {
            var type = typeof(T);
            
            if (_states.ContainsKey(type))
            {
                Debug.LogWarning($"State of type {type.Name} is already registered. Overriding.");
            }
            
            _states[type] = state;
        }

        public T GetState<T>() where T : class, IState
        {
            var type = typeof(T);
            
            if (_states.TryGetValue(type, out var state))
            {
                return state as T;
            }
            
            return null;
        }

        public void ChangeState(IState newState)
        {
            if (newState == _currentState)
                return;
                
            _previousState = _currentState;
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }

        public void ChangeState<T>() where T : class, IState
        {
            var type = typeof(T);
            if (_states.TryGetValue(type, out var state))
            {
                ChangeState(state);
            }
            else
            {
                Debug.LogError($"Failed to change state: State of type {type.Name} is not registered.");
            }
        }

        public void RevertToPreviousState()
        {
            if (_previousState != null)
            {
                ChangeState(_previousState);
            }
        }

        public void Update()
        {
            _currentState?.Execute();
        }
    }
}