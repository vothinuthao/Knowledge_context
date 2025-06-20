#region Supporting Data Structures

using System;

namespace VikingRaven.Units
{
    public enum CombatStateType
    {
        Idle,
        Patrolling,
        Guarding,
        Aggro,
        CombatEngaged,
        Retreat,
        Exhausted,
        WeaponBroken,
        Knockback,
        Stunned
    }
    public enum StateTransitionReason
    {
        Manual,
        Intelligent,
        Forced,
        DamageTaken,
        LowHealth,
        Exhausted,
        WeaponBroken,
        WeaponRepaired,
        CombatEnded,
        EnemyDetected,
        HeavyDamage,
        StatusEffect
    }
    [Serializable]
    public struct StateTransition
    {
        public CombatStateType TargetState;
        public StateTransitionReason Reason;
        public float Timestamp;
        
        public StateTransition(CombatStateType targetState, StateTransitionReason reason, float timestamp)
        {
            TargetState = targetState;
            Reason = reason;
            Timestamp = timestamp;
        }
    }
    #endregion
}