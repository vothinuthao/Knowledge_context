using System.Collections.Generic;

namespace RavenDeckbuilding.Core.Architecture.Registry
{
    /// <summary>
    /// Generic registry interface for managing objects by ID
    /// </summary>
    public interface IRegistry<TKey, TValue>
    {
        void Register(TKey key, TValue value);
        bool Unregister(TKey key);
        TValue Get(TKey key);
        bool Contains(TKey key);
        IEnumerable<TValue> GetAll();
        IEnumerable<TKey> GetAllKeys();
        void Clear();
        int Count { get; }
    }
}