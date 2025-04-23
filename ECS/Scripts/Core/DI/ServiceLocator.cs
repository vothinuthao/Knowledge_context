using System;

namespace Core.DI
{
    /// <summary>
    /// Service locator pattern implementation for dependency injection
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceProvider _serviceProvider;
        
        /// <summary>
        /// Initialize the service locator with a service provider
        /// </summary>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        /// <summary>
        /// Get a service of the specified type
        /// </summary>
        public static T Get<T>() where T : class
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("ServiceLocator is not initialized");
            }
            
            return _serviceProvider.GetService<T>();
        }
    }
}