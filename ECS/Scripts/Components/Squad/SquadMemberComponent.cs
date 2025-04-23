using Core.ECS;
using UnityEngine;

namespace Squad
{
    /// <summary>
    /// Component for entities that belong to a squad
    /// </summary>
    public class SquadMemberComponent : IComponent
    {
        // Reference to the squad entity ID
        public int SquadEntityId { get; set; }
        
        // Position within the squad (grid coordinates)
        public Vector2Int GridPosition { get; set; }
        
        // Desired world position based on squad formation
        public Vector3 DesiredPosition { get; set; }
        
        // Whether this troop should lock position when squad is idle
        public bool LockPositionWhenIdle { get; set; } = true;
        
        public SquadMemberComponent(int squadEntityId, Vector2Int gridPosition)
        {
            SquadEntityId = squadEntityId;
            GridPosition = gridPosition;
            DesiredPosition = Vector3.zero;
        }
    }
}