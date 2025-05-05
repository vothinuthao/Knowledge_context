using System;
using System.Collections.Generic;

namespace Core.DI
{
    /// <summary>
    /// A simple dependency injection container
    /// </summary>
    public class ServiceContainer : IServiceProvider
    {
        // Dictionary mapping service types to service instances
        private Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
        // Dictionary mapping service types to factory functions
        private Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
        
        /// <summary>
        /// Register a singleton service instance
        /// </summary>
        public void RegisterSingleton<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }
        
        /// <summary>
        /// Register a singleton service by type
        /// </summary>
        public void RegisterSingleton<TInterface, TImplementation>() 
            where TInterface : class 
            where TImplementation : class, TInterface, new()
        {
            _factories[typeof(TInterface)] = () => new TImplementation();
        }
        
        /// <summary>
        /// Register a singleton service with a factory function
        /// </summary>
        public void RegisterSingleton<T>(Func<T> factory) where T : class
        {
            _factories[typeof(T)] = () => factory();
        }
        
        /// <summary>
        /// Get a service of the specified type
        /// </summary>
        public T GetService<T>() where T : class
        {
            Type type = typeof(T);
            
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }
            
            if (_factories.TryGetValue(type, out var factory))
            {
                var instance = (T)factory();
                _services[type] = instance; // Cache the instance
                return instance;
            }
            
            throw new InvalidOperationException($"No service of type {type.Name} has been registered");
        }
    }
}