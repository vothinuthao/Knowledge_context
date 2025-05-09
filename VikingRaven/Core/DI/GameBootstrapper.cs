using UnityEngine;
using VikingRaven.Core.ECS;
using Zenject;

namespace VikingRaven.Core.DI
{
    public class GameBootstrapper : MonoBehaviour
    {
        [Inject] private SystemRegistry _systemRegistry;
        
        /// <summary>
        /// Initialize all systems after Zenject has injected dependencies
        /// </summary>
        private void Start()
        {
            Debug.Log("GameBootstrapper: Initializing all systems");
            _systemRegistry.InitializeAllSystems();
        }
        
        /// <summary>
        /// Execute all systems every frame
        /// </summary>
        private void Update()
        {
            _systemRegistry.ExecuteAllSystems();
        }
    }
}