using System.Collections.Generic;
using System.Linq;
using Core.Utils;
using UnityEngine;

namespace VikingRaven.Core.ECS
{
    public class SystemRegistry : Singleton<SystemRegistry>, ISystemRegistry
    {
        private readonly List<ISystem> _systems = new List<ISystem>();
        private bool _systemsSorted = false;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Debug.Log("SystemRegistry initialized as singleton");
        }

        public void RegisterSystem(ISystem system)
        {
            if (!_systems.Contains(system))
            {
                _systems.Add(system);
                _systemsSorted = false;
                
                // Initialize the system immediately if it's added after startup
                system.Initialize();
                
                Debug.Log($"System registered: {system.GetType().Name}");
            }
        }

        public void UnregisterSystem(ISystem system)
        {
            _systems.Remove(system);
        }

        public void ExecuteAllSystems()
        {
            if (!_systemsSorted)
            {
                _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                _systemsSorted = true;
            }

            foreach (var system in _systems.Where(s => s.IsActive))
            {
                system.Execute();
            }
        }

        public void InitializeAllSystems()
        {
            foreach (var system in _systems)
            {
                system.Initialize();
            }
            
            Debug.Log($"Initialized {_systems.Count} systems");
        }

        public void CleanupAllSystems()
        {
            foreach (var system in _systems)
            {
                system.Cleanup();
            }
            
            _systems.Clear();
        }

        private void OnDestroy()
        {
            CleanupAllSystems();
        }
    }
}