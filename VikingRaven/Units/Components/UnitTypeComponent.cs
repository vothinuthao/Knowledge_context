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
            if (!System.Enum.IsDefined(typeof(UnitType), unitType))
            {
                Debug.LogError($"UnitTypeComponent: Invalid UnitType value: {unitType}");
                _unitType = UnitType.Infantry;
            }
            else
            {
                _unitType = unitType;
            }
    
            if (Entity == null)
            {
                Debug.LogError("UnitTypeComponent: Entity is null when setting UnitType");
                return;
            }
    
            // Configure unit based on type
            var combatComponent = Entity.GetComponent<CombatComponent>();
    
            if (combatComponent != null)
            {
                switch (_unitType)
                {
                    case UnitType.Infantry:
                        // Configure infantry-specific values
                        combatComponent.SetAttackRange(2.0f);
                        combatComponent.SetAttackDamage(15.0f);
                        combatComponent.SetAttackCooldown(1.2f);
                        break;
            
                    case UnitType.Archer:
                        // Configure archer-specific values
                        combatComponent.SetAttackRange(8.0f);
                        combatComponent.SetAttackDamage(10.0f);
                        combatComponent.SetAttackCooldown(2.0f);
                        break;
            
                    case UnitType.Pike:
                        // Configure pike-specific values
                        combatComponent.SetAttackRange(3.0f);
                        combatComponent.SetAttackDamage(20.0f);
                        combatComponent.SetAttackCooldown(1.5f);
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