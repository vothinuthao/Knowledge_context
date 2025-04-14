using UnityEngine;
using System;

namespace Troops.Base
{
    /// <summary>
    /// Event arguments for troop damaged events
    /// </summary>
    public class TroopDamagedEventArgs : EventArgs
    {
        public TroopBase Troop { get; private set; }
        public TroopBase Attacker { get; private set; }
        public float Damage { get; private set; }
        public float RemainingHealth { get; private set; }
        
        public TroopDamagedEventArgs(TroopBase troop, TroopBase attacker, float damage, float remainingHealth)
        {
            Troop = troop;
            Attacker = attacker;
            Damage = damage;
            RemainingHealth = remainingHealth;
        }
    }
}