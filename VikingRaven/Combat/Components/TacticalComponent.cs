using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Combat.Components
{
    public class TacticalComponent : MonoBehaviour, IComponent
    {
        [SerializeField] private bool _isActive = true;
        [SerializeField] private TacticalRole _assignedRole = TacticalRole.None;
        [SerializeField] private float _threatLevel = 0f;
        [SerializeField] private float _tacticalImportance = 1f;
        
        // Tactical instructions
        [SerializeField] private TacticalObjective _currentObjective = TacticalObjective.None;
        [SerializeField] private Vector3 _objectivePosition;
        [SerializeField] private IEntity _objectiveTarget;
        
        private IEntity _entity;
        
        public bool IsActive { get => _isActive; set => _isActive = value; }
        public IEntity Entity { get => _entity; set => _entity = value; }
        
        public TacticalRole AssignedRole
        {
            get => _assignedRole;
            set => _assignedRole = value;
        }
        
        public float ThreatLevel
        {
            get => _threatLevel;
            set => _threatLevel = value;
        }
        
        public float TacticalImportance
        {
            get => _tacticalImportance;
            set => _tacticalImportance = value;
        }
        
        public TacticalObjective CurrentObjective
        {
            get => _currentObjective;
            set => _currentObjective = value;
        }
        
        public Vector3 ObjectivePosition
        {
            get => _objectivePosition;
            set => _objectivePosition = value;
        }
        
        public IEntity ObjectiveTarget
        {
            get => _objectiveTarget;
            set => _objectiveTarget = value;
        }
        
        public void Initialize() { }
        public void Cleanup() { }
    }

    public enum TacticalRole
    {
        None,
        Frontline,
        Support,
        Flanker,
        Defender,
        Scout
    }

    public enum TacticalObjective
    {
        None,
        Attack,
        Defend,
        Move,
        Hold,
        Retreat,
        Scout
    }
}