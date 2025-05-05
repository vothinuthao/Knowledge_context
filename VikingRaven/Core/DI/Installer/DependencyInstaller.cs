using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Core.DI
{
    public class DependencyInstaller : MonoBehaviour
    {
        [SerializeField] private EntityRegistry _entityRegistry;
        [SerializeField] private SystemRegistry _systemRegistry;
        
        private void Awake()
        {
            // Register core dependencies
            DependencyContainer.Instance.Register<IEntityRegistry>(_entityRegistry);
            DependencyContainer.Instance.Register<ISystemRegistry>(_systemRegistry);
            
            // Inject dependencies to all objects in the scene
            DependencyContainer.Instance.InjectDependenciesInScene();
        }
    }
}