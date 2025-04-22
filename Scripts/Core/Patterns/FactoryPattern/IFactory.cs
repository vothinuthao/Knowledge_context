namespace Core.Patterns.FactoryPattern
{
    public interface IFactory<TProduct>
    {
        TProduct Create();
    }
}