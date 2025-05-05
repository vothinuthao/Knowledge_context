using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Utils;
using UnityEngine;

namespace VikingRaven.Core.DI
{
    public class DependencyContainer : Singleton<DependencyContainer>
    {
        private readonly Dictionary<Type, object> _dependencies = new Dictionary<Type, object>();
        // private static DependencyContainer _instance;
        //
        // public static DependencyContainer Instance
        // {
        //     get
        //     {
        //         if (_instance == null)
        //         {
        //             var containerObject = new GameObject("DependencyContainer");
        //             _instance = containerObject.AddComponent<DependencyContainer>();
        //             DontDestroyOnLoad(containerObject);
        //         }
        //         return _instance;
        //     }
        // }

        public void Register<T>(T instance)
        {
            var type = typeof(T);
            
            if (_dependencies.ContainsKey(type))
            {
                Debug.LogWarning($"Dependency of type {type.Name} is already registered. Overriding.");
            }
            
            _dependencies[type] = instance;
            
            // Also register interfaces that this type implements
            foreach (var interfaceType in type.GetInterfaces())
            {
                if (!_dependencies.ContainsKey(interfaceType))
                {
                    _dependencies[interfaceType] = instance;
                }
            }
        }

        public T Resolve<T>()
        {
            var type = typeof(T);
            
            if (_dependencies.TryGetValue(type, out var instance))
            {
                return (T)instance;
            }
            
            Debug.LogError($"Dependency of type {type.Name} is not registered.");
            return default;
        }

        public void InjectDependencies(object target)
        {
            var type = target.GetType();
            
            // Inject into fields with [Inject] attribute
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<InjectAttribute>() != null);
            
            foreach (var field in fields)
            {
                if (_dependencies.TryGetValue(field.FieldType, out var dependency))
                {
                    field.SetValue(target, dependency);
                }
                else
                {
                    Debug.LogError($"Failed to inject dependency of type {field.FieldType.Name} into field {field.Name} of {type.Name}");
                }
            }
            
            // Inject into properties with [Inject] attribute
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<InjectAttribute>() != null && p.CanWrite);
            
            foreach (var property in properties)
            {
                if (_dependencies.TryGetValue(property.PropertyType, out var dependency))
                {
                    property.SetValue(target, dependency);
                }
                else
                {
                    Debug.LogError($"Failed to inject dependency of type {property.PropertyType.Name} into property {property.Name} of {type.Name}");
                }
            }
        }

        [Obsolete("Obsolete")]
        public void InjectDependenciesInScene()
        {
            var monoBehaviours = FindObjectsOfType<MonoBehaviour>();
            
            foreach (var monoBehaviour in monoBehaviours)
            {
                InjectDependencies(monoBehaviour);
            }
        }
    }
}