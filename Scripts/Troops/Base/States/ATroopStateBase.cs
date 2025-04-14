using Core;

namespace Troops.Base
{
    /// <summary>
    /// Base class for all troop states
    /// </summary>
    public abstract class ATroopStateBase : ITroopState
    {
        protected TroopState stateType;
        
        public ATroopStateBase(TroopState stateType)
        {
            this.stateType = stateType;
        }
        
        public virtual void Enter(TroopBase troop)
        {
            // Trigger state changed event
            EventManager.Instance.TriggerEvent(EventTypeInGame.TroopStateChanged, 
                new TroopStateChangedEventArgs(troop, stateType));
        }
        
        public abstract void Update(TroopBase troop);
        
        public virtual void Exit(TroopBase troop) { }
        
        public TroopState GetStateType()
        {
            return stateType;
        }
    }
}