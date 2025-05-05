using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class FormationComponent : BaseComponent
    {
        [SerializeField] private int _formationSlotIndex;
        [SerializeField] private Vector3 _formationOffset;
        [SerializeField] private int _squadId;
        [SerializeField] private FormationType _currentFormationType;
        
        public int FormationSlotIndex => _formationSlotIndex;
        public Vector3 FormationOffset => _formationOffset;
        public int SquadId => _squadId;
        public FormationType CurrentFormationType => _currentFormationType;

        public void SetFormationSlot(int slotIndex)
        {
            _formationSlotIndex = slotIndex;
        }

        public void SetFormationOffset(Vector3 offset)
        {
            _formationOffset = offset;
        }

        public void SetSquadId(int squadId)
        {
            _squadId = squadId;
        }

        public void SetFormationType(FormationType formationType)
        {
            _currentFormationType = formationType;
            
            // Update formation offset based on formation type
            switch (_currentFormationType)
            {
                case FormationType.Line:
                    // Line formation: units side by side
                    _formationOffset = new Vector3(_formationSlotIndex * 1.5f, 0, 0);
                    break;
                
                case FormationType.Column:
                    // Column formation: units in a line front to back
                    _formationOffset = new Vector3(0, 0, _formationSlotIndex * 1.5f);
                    break;
                
                case FormationType.Phalanx:
                    // Phalanx: grid formation
                    int rowSize = Mathf.CeilToInt(Mathf.Sqrt(_formationSlotIndex + 1));
                    int row = _formationSlotIndex / rowSize;
                    int col = _formationSlotIndex % rowSize;
                    _formationOffset = new Vector3(col * 1.0f, 0, row * 1.0f);
                    break;
                
                case FormationType.Testudo:
                    // Testudo: tight grid formation
                    rowSize = Mathf.CeilToInt(Mathf.Sqrt(_formationSlotIndex + 1));
                    row = _formationSlotIndex / rowSize;
                    col = _formationSlotIndex % rowSize;
                    _formationOffset = new Vector3(col * 0.7f, 0, row * 0.7f);
                    break;
                
                case FormationType.Circle:
                    // Circle formation
                    float angle = (_formationSlotIndex * Mathf.PI * 2) / 8; // Assuming 8 units max
                    _formationOffset = new Vector3(Mathf.Cos(angle) * 3.0f, 0, Mathf.Sin(angle) * 3.0f);
                    break;
            }
        }
    }

    public enum FormationType
    {
        None,
        Line,
        Column,
        Phalanx,
        Testudo,
        Circle
    }
}