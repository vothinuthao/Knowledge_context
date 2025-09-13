namespace RavenDeckbuilding.Core.Architecture.Pooling
{
    /// <summary>
    /// Interface for objects that can be pooled
    /// </summary>
    public interface IPoolable
    {
        void OnPoolGet();
        void OnPoolReturn();
        bool IsAvailableForPool { get; }
    }
    
    /// <summary>
    /// Interface for object pools
    /// </summary>
    public interface IObjectPool<T>
    {
        T Get();
        void Return(T item);
        void Clear();
        int CountActive { get; }
        int CountInactive { get; }
    }
}