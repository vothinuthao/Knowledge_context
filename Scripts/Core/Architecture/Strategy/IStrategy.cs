namespace RavenDeckbuilding.Core.Architecture.Strategy
{
    /// <summary>
    /// Generic strategy interface
    /// </summary>
    public interface IStrategy<TContext, TResult>
    {
        TResult Execute(TContext context);
        bool CanExecute(TContext context);
        int Priority { get; }
    }
    
    /// <summary>
    /// Generic strategy interface without return value
    /// </summary>
    public interface IStrategy<TContext>
    {
        void Execute(TContext context);
        bool CanExecute(TContext context);
        int Priority { get; }
    }
}