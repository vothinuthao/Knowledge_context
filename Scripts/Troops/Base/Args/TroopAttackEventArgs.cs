using UnityEngine;
using System;

namespace Troops.Base
{
    /// <summary>
    /// Event arguments for troop attack events
    /// </summary>
    public class TroopAttackEventArgs : EventArgs
    {
        public TroopBase Attacker { get; private set; }
        public TroopBase Target { get; private set; }
        public float Damage { get; private set; }
        public bool IsCritical { get; private set; }
        
        public TroopAttackEventArgs(TroopBase attacker, TroopBase target, float damage, bool isCritical = false)
        {
            Attacker = attacker;
            Target = target;
            Damage = damage;
            IsCritical = isCritical;
        }
    }
}