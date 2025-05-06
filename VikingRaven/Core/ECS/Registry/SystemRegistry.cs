using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VikingRaven.Core.ECS
{
    public class SystemRegistry : MonoBehaviour, ISystemRegistry
    {
        private readonly List<ISystem> _systems = new List<ISystem>();
        private bool _systemsSorted = false;

        public void RegisterSystem(ISystem system)
        {
            if (!_systems.Contains(system))
            {
                _systems.Add(system);
                _systemsSorted = false;
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