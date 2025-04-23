namespace Core.DI
{
    /// <summary>
    /// Interface for service providers
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// Get a service of the specified type
        /// </summary>
        T GetService<T>() where T : class;
    }
}