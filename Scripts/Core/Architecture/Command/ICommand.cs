namespace RavenDeckbuilding.Core.Architecture.Command
{
    /// <summary>
    /// Generic command interface
    /// </summary>
    public interface ICommand<TContext, TResult>
    {
        TResult Execute(TContext context);
        bool CanExecute(TContext context);
        void Undo(TContext context);
        bool CanUndo();
        float EstimatedExecutionTime { get; }
        int Priority { get; }
    }
    
    /// <summary>
    /// Generic command interface without return value
    /// </summary>
    public interface ICommand<TContext>
    {
        void Execute(TContext context);
        bool CanExecute(TContext context);
        void Undo(TContext context);
        bool CanUndo();
        float EstimatedExecutionTime { get; }
        int Priority { get; }
    }
}