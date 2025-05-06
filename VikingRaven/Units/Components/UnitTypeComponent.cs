using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class UnitTypeComponent : BaseComponent
    {
        [SerializeField] private UnitType _unitType;
        
        public UnitType UnitType => _unitType;

        public void SetUnitType(UnitType unitType)
        {
            _unitType = unitType;
            
            // Configure unit based on type
            var combatComponent = Entity.GetComponent<CombatComponent>();
            
            if (combatComponent != null)
            {
                switch (_unitType)
                {
                    case UnitType.Infantry:
                        // Configure infantry combat values
                        // Higher health, medium damage, short range
                        break;
                    
                    case UnitType.Archer:
                        // Configure archer combat values
                        // Lower health, medium damage, long range
                        break;
                    
                    case UnitType.Pike:
                        // Configure pike combat values
                        // Medium health, high damage, medium range
                        break;
                }
            }
        }
    }

    public enum UnitType
    {
        Infantry,
        Archer,
        Pike
    }
}