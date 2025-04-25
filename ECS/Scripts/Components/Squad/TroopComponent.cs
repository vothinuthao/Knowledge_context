using Core.ECS;
using UnityEngine;

namespace Components.Squad
{
    /// <summary>
    /// Component for individual troop entities
    /// </summary>
    public class TroopComponent : IComponent
    {
        public int SquadId { get; set; } = -1;
        public int FormationIndex { get; set; } = -1;
        public TroopRole Role { get; set; } = TroopRole.SOLDIER;
        public float Morale { get; set; } = 1.0f;
        
        public TroopComponent()
        {
        }
        
        public TroopComponent(int squadId, int formationIndex)
        {
            SquadId = squadId;
            FormationIndex = formationIndex;
        }
    }
    
    public enum TroopRole
    {
        SOLDIER,
        LEADER,
        SPECIALIST
    }
}