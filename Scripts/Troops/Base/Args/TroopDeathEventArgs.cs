using System;

namespace Troops.Base
{
    /// <summary>
    /// Event arguments for troop death events
    /// </summary>
    public class TroopDeathEventArgs : EventArgs
    {
        public TroopBase Troop { get; private set; }
        public TroopBase Killer { get; private set; }
        public TroopDeathEventArgs(TroopBase troop, TroopBase killer = null)
        {
            Troop = troop;
            Killer = killer;
        }
    }
}