using System;
using System.Collections.Generic;
using Troop;
using UnityEngine;

public class TroopStateMachine
{
    private TroopController _owner;
    private ITroopState _currentState;
    private Dictionary<Type, ITroopState> _stateCache = new Dictionary<Type, ITroopState>();

    public TroopStateMachine(TroopController owner)
    {
        _owner = owner;
    }

    public TroopState CurrentStateEnum => _currentState?.GetStateEnum() ?? TroopState.Idle;

    public T GetState<T>() where T : ITroopState, new()
    {
        var type = typeof(T);

        if (!_stateCache.TryGetValue(type, out var state))
        {
            state = new T();
            _stateCache[type] = state;
        }

        return (T)state;
    }

    public void ChangeState<T>() where T : ITroopState, new()
    {
        var newState = GetState<T>();
        ChangeState(newState);
    }

    public void ChangeState(ITroopState newState)
    {
        if (_currentState == newState)
            return;

        // Check if transition is allowed
        if (_currentState != null)
        {
            if (!_currentState.CanTransitionTo(newState.GetType()))
            {
                Debug.LogWarning($"Transition from {_currentState.GetType().Name} to {newState.GetType().Name} is not allowed");
                return;
            }

            _currentState.Exit(_owner);
        }

        _currentState = newState;
        _currentState.Enter(_owner);
    }

    public void Update()
    {
        if (_currentState != null)
        {
            _currentState.Update(_owner);
        }
    }
}