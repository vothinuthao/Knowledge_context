using System;
using Troop;

public interface ITroopState
{
    void Enter(TroopController troop);
    void Update(TroopController troop);
    void Exit(TroopController troop);
    bool CanTransitionTo(Type nextStateType);
    TroopState GetStateEnum();
}