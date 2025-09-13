namespace RavenDeckbuilding.Core.Architecture.Observer
{
    /// <summary>
    /// Generic observer interface
    /// </summary>
    public interface IObserver<TEventData>
    {
        void OnNotify(TEventData eventData);
        bool ShouldReceiveEvent(TEventData eventData);
        int Priority { get; }
    }
    
    /// <summary>
    /// Generic subject interface
    /// </summary>
    public interface ISubject<TEventData>
    {
        void Subscribe(IObserver<TEventData> observer);
        void Unsubscribe(IObserver<TEventData> observer);
        void NotifyObservers(TEventData eventData);
    }
}