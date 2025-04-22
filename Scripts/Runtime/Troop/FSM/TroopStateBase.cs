using System;
using System.Collections.Generic;
using Troop;

public abstract class TroopStateBase : ITroopState
{
    protected List<Type> allowedTransitions = new List<Type>();
    protected TroopState stateEnum;

    public abstract void Enter(TroopController troop);
    public abstract void Update(TroopController troop);
    public abstract void Exit(TroopController troop);

    public bool CanTransitionTo(Type nextStateType)
    {
        return allowedTransitions.Contains(nextStateType);
    }

    public TroopState GetStateEnum()
    {
        return stateEnum;
    }
}