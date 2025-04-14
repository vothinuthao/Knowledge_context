namespace Troops.Base
{
    /// <summary>
    /// Event args for troop state changed events
    /// </summary>
    public class TroopStateChangedEventArgs : System.EventArgs
    {
        public TroopBase Troop { get; private set; }
        public TroopState NewState { get; private set; }
        
        public TroopStateChangedEventArgs(TroopBase troop, TroopState newState)
        {
            Troop = troop;
            NewState = newState;
        }
    }
}