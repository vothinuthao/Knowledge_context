using Sirenix.OdinInspector;
using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    [System.Serializable]
    public class CombatDebugHelper : MonoBehaviour
    {
        [Title("Combat Debug Helper")]
        [InfoBox("Helper script to access CombatComponent debug functions easily")]
    
        [SerializeField]
        private BaseEntity _targetEntity;
    
        private CombatComponent _combatComponent;
    
        private void Start() 
        {
            if (_targetEntity != null)
            {
                _combatComponent = _targetEntity.GetComponent<CombatComponent>();
            }
        }
    
        [Button("Find Combat Component")]
        private void FindCombatComponent()
        {
            if (_targetEntity == null)
                _targetEntity = GetComponent<BaseEntity>();
            
            if (_targetEntity != null)
            {
                _combatComponent = _targetEntity.GetComponent<CombatComponent>();
                Debug.Log(_combatComponent != null ? "Combat Component found!" : "Combat Component not found!");
            }
        }
    
        [Button("Debug Range Attack"), EnableIf("@_combatComponent != null")]
        private void DebugRangeAttack()
        {
            _combatComponent?.GetType().GetMethod("DebugRangeAttackSystem", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_combatComponent, null);
        }
    
        [Button("Full Combat Analysis"), EnableIf("@_combatComponent != null")]
        private void FullCombatAnalysis()
        {
            _combatComponent?.GetType().GetMethod("PerformFullCombatAnalysis", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_combatComponent, null);
        }
    }
}