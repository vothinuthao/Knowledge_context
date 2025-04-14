namespace Troops.Base
{
    /// <summary>
    /// Interface for troop state pattern
    /// </summary>
    public interface ITroopState
    {
        /// <summary>
        /// Enter this state
        /// </summary>
        void Enter(TroopBase troop);
        
        /// <summary>
        /// Update logic for this state
        /// </summary>
        void Update(TroopBase troop);
        
        /// <summary>
        /// Exit this state
        /// </summary>
        void Exit(TroopBase troop);
        
        /// <summary>
        /// Get the state type
        /// </summary>
        TroopState GetStateType();
    }
}