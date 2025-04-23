using Core.ECS;
using UnityEngine;

namespace Squad
{
    /// <summary>
    /// Enum for squad states
    /// </summary>
    public enum SquadState
    {
        Idle,
        Moving,
        Attacking,
        Defending
    }
    
    /// <summary>
    /// Component for squad state data
    /// </summary>
    public class SquadStateComponent : IComponent
    {
        // Current state of the squad
        public SquadState CurrentState { get; set; } = SquadState.Idle;
        
        // Target position for movement
        public Vector3 TargetPosition { get; set; }
        
        // Target entity ID for attack
        public int TargetEntityId { get; set; } = -1;
        
        // Whether the squad is currently moving
        public bool IsMoving => CurrentState == SquadState.Moving;
        
        // Whether troops should be locked in position
        public bool ShouldLockTroops => CurrentState == SquadState.Idle;
        
        public SquadStateComponent()
        {
            TargetPosition = Vector3.zero;
        }
    }
}