namespace Core.Patterns.StrategyPattern
{
    public interface IStrategy<in TContext, out TOutput>
    {
        TOutput Execute(TContext context);
        float GetWeight();
    }
}