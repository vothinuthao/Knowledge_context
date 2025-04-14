namespace Core.Patterns
{
    /// <summary>
    /// Generic interface for observer
    /// </summary>
    /// <typeparam name="T">Type of data observed</typeparam>
    public interface IObserver<T>
    {
        /// <summary>
        /// Called when observed subject changes
        /// </summary>
        void OnNotify(T data);
    }
    
    /// <summary>
    /// Generic interface for observable subject
    /// </summary>
    /// <typeparam name="T">Type of data to observe</typeparam>
    public interface IObservable<T>
    {
        /// <summary>
        /// Add an observer
        /// </summary>
        void AddObserver(IObserver<T> observer);
        
        /// <summary>
        /// Remove an observer
        /// </summary>
        void RemoveObserver(IObserver<T> observer);
        
        /// <summary>
        /// Notify all observers with data
        /// </summary>
        void NotifyObservers(T data);
    }
}