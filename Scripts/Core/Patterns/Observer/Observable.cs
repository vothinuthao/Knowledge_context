// Assets/Scripts/Core/Patterns/GenericObserver.cs

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Patterns
{
    /// <summary>
    /// Base implementation of observable subject
    /// </summary>
    /// <typeparam name="T">Type of data to observe</typeparam>
    public class Observable<T> : IObservable<T>
    {
        // List of observers
        protected List<IObserver<T>> observers = new List<IObserver<T>>();
        
        /// <summary>
        /// Add an observer
        /// </summary>
        public virtual void AddObserver(IObserver<T> observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }
        }
        
        /// <summary>
        /// Remove an observer
        /// </summary>
        public virtual void RemoveObserver(IObserver<T> observer)
        {
            observers.Remove(observer);
        }
        
        /// <summary>
        /// Notify all observers with data
        /// </summary>
        public virtual void NotifyObservers(T data)
        {
            // Create a copy of the list to prevent issues if observers modify the list during iteration
            List<IObserver<T>> observersCopy = new List<IObserver<T>>(observers);
            
            foreach (var observer in observersCopy)
            {
                try
                {
                    observer.OnNotify(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error notifying observer: {e.Message}");
                }
            }
        }
        
        /// <summary>
        /// Clear all observers
        /// </summary>
        public virtual void ClearObservers()
        {
            observers.Clear();
        }
        
        /// <summary>
        /// Get the number of observers
        /// </summary>
        public int ObserverCount => observers.Count;
    }
    
    /// <summary>
    /// MonoBehaviour implementation of Observable
    /// </summary>
    /// <typeparam name="T">Type of data to observe</typeparam>
    public class MonoBehaviourObservable<T> : MonoBehaviour, IObservable<T>
    {
        // Delegate the implementation to a contained Observable
        protected Observable<T> observable = new Observable<T>();
        
        /// <summary>
        /// Add an observer
        /// </summary>
        public void AddObserver(IObserver<T> observer)
        {
            observable.AddObserver(observer);
        }
        
        /// <summary>
        /// Remove an observer
        /// </summary>
        public void RemoveObserver(IObserver<T> observer)
        {
            observable.RemoveObserver(observer);
        }
        
        /// <summary>
        /// Notify all observers with data
        /// </summary>
        public void NotifyObservers(T data)
        {
            observable.NotifyObservers(data);
        }
        
        /// <summary>
        /// Clear all observers when the object is destroyed
        /// </summary>
        protected virtual void OnDestroy()
        {
            observable.ClearObservers();
        }
    }
    
    /// <summary>
    /// Generic event arguments
    /// </summary>
    /// <typeparam name="T">Type of data</typeparam>
    public class EventArgs<T> : EventArgs
    {
        /// <summary>
        /// The data
        /// </summary>
        public T Data { get; private set; }
        
        /// <summary>
        /// Constructor
        /// </summary>
        public EventArgs(T data)
        {
            Data = data;
        }
    }
    
    /// <summary>
    /// Event-based observer pattern implementation
    /// </summary>
    /// <typeparam name="T">Type of data to observe</typeparam>
    public class EventObservable<T>
    {
        /// <summary>
        /// Event handler delegate
        /// </summary>
        public delegate void EventHandler(object sender, EventArgs<T> e);
        
        /// <summary>
        /// The event
        /// </summary>
        public event EventHandler OnEvent;
        
        /// <summary>
        /// Raise the event with data
        /// </summary>
        public virtual void RaiseEvent(object sender, T data)
        {
            OnEvent?.Invoke(sender, new EventArgs<T>(data));
        }
    }
}