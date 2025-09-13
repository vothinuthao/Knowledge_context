using System;

namespace RavenDeckbuilding.Core.Architecture.Factory
{
    /// <summary>
    /// Generic factory interface for creating objects
    /// </summary>
    public interface IFactory<T>
    {
        T Create();
        T Create(object parameters);
        bool CanCreate(Type type);
    }
    
    /// <summary>
    /// Generic factory interface for creating objects with specific data
    /// </summary>
    public interface IFactory<TProduct, TData>
    {
        TProduct Create(TData data);
        bool CanCreate(TData data);
    }
    
    /// <summary>
    /// Generic abstract factory for families of related objects
    /// </summary>
    public interface IAbstractFactory<TProductFamily>
    {
        T CreateProduct<T>() where T : TProductFamily;
        T CreateProduct<T>(object parameters) where T : TProductFamily;
    }
}