using UnityEngine;
using VikingRaven.Core.DI;
using VikingRaven.Core.Factory;

namespace VikingRaven.SystemDebugger_Tool
{
    public class SystemDebugger : MonoBehaviour
    {
        [SerializeField] private DependencyInstaller _dependencyInstaller;
        [SerializeField] private UnitFactory _unitFactory;
        [SerializeField] private SquadFactory _squadFactory;
    
        private void Start()
        {
            Debug.Log("===== SYSTEM DEBUGGER =====");
        
            if (_dependencyInstaller == null)
                _dependencyInstaller = FindObjectOfType<DependencyInstaller>();
        
            Debug.Log($"DependencyInstaller: {(_dependencyInstaller != null ? "Found" : "MISSING")}");
            Debug.Log($"DependencyInstaller Initialized: {(_dependencyInstaller != null ? _dependencyInstaller.IsInitialized.ToString() : "N/A")}");
        
            if (_unitFactory == null)
                _unitFactory = FindObjectOfType<UnitFactory>();
            if (_squadFactory == null)
                _squadFactory = FindObjectOfType<SquadFactory>();
        
            Debug.Log($"UnitFactory: {(_unitFactory != null ? "Found" : "MISSING")}");
            Debug.Log($"SquadFactory: {(_squadFactory != null ? "Found" : "MISSING")}");
        
            if (_squadFactory != null)
            {
                var field = _squadFactory.GetType().GetField("_unitFactory", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public);
            
                if (field != null)
                {
                    var value = field.GetValue(_squadFactory);
                    Debug.Log($"SquadFactory._unitFactory: {(value != null ? "Has Value" : "NULL")}");
                }
                else
                {
                    Debug.Log("Could not find _unitFactory field in SquadFactory");
                }
            }
        
            Debug.Log("===== END DEBUGGER =====");
        }
    }
}