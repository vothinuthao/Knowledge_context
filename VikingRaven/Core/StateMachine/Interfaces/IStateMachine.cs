namespace VikingRaven.Core.StateMachine
{
    public interface IStateMachine
    {
        IState CurrentState { get; }
        IState PreviousState { get; }
        
        void ChangeState(IState newState);
        void RevertToPreviousState();
        void Update();
    }
}