using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Core.DI
{
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private DependencyInstaller _dependencyInstaller;
        [SerializeField] private SystemRegistry _systemRegistry;
        
        private void Start()
        {
            // Initialize all systems after dependencies are injected
            _systemRegistry.InitializeAllSystems();
        }
        
        private void Update()
        {
            // Execute all systems every frame
            _systemRegistry.ExecuteAllSystems();
        }
    }
}