using System;
using System.Collections.Generic;
using Core.Patterns;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Event manager using Singleton pattern and leveraging Observable
    /// </summary>
    public class EventManager : MonoBehaviourSingleton<EventManager>
    {
        private Dictionary<EventType, EventObservable<EventArgs>> _eventObservables = new Dictionary<EventType, EventObservable<System.EventArgs>>();
        private Dictionary<EventType, EventHandler> _eventHandlers = new Dictionary<EventType, System.EventHandler>();
        private Observable<EventData> _globalObservable = new Observable<EventData>();
        
        protected override void OnSingletonCreated()
        {
            foreach (EventType eventType in Enum.GetValues(typeof(EventType)))
            {
                _eventObservables[eventType] = new EventObservable<System.EventArgs>();
            }
        }
        
        /// <summary>
        /// Add a listener for a specific event type (traditional approach)
        /// </summary>
        public void AddListener(EventType eventType, System.EventHandler handler)
        {
            _eventHandlers.TryAdd(eventType, null);
            _eventHandlers[eventType] += handler;
        }
        
        /// <summary>
        /// Remove a listener for a specific event type (traditional approach)
        /// </summary>
        public void RemoveListener(EventType eventType, System.EventHandler handler)
        {
            if (_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType] -= handler;
            }
        }
        
        /// <summary>
        /// Subscribe to events using Observable pattern
        /// </summary>
        public void Subscribe<T>(EventType eventType, EventObservable<System.EventArgs>.EventHandler handler) where T : System.EventArgs
        {
            if (_eventObservables.TryGetValue(eventType, out var observable))
            {
                observable.OnEvent += handler;
            }
        }
        
        /// <summary>
        /// Unsubscribe from events using Observable pattern
        /// </summary>
        public void Unsubscribe<T>(EventType eventType, EventObservable<System.EventArgs>.EventHandler handler) where T : System.EventArgs
        {
            if (_eventObservables.TryGetValue(eventType, out var observable))
            {
                observable.OnEvent -= handler;
            }
        }
        
        /// <summary>
        /// Add a global observer that receives all events
        /// </summary>
        public void AddGlobalObserver(Patterns.IObserver<EventData> observer)
        {
            _globalObservable.AddObserver(observer);
        }
        
        /// <summary>
        /// Remove a global observer
        /// </summary>
        public void RemoveGlobalObserver(Patterns.IObserver<EventData> observer)
        {
            _globalObservable.RemoveObserver(observer);
        }
        
        /// <summary>
        /// Trigger an event with no data
        /// </summary>
        public void TriggerEvent(EventType eventType)
        {
            TriggerEvent(eventType, EventArgs.Empty);
        }
        
        /// <summary>
        /// Trigger an event with data
        /// </summary>
        public void TriggerEvent<T>(EventType eventType, T data)
        {
            TriggerEvent(eventType, new EventArgs<T>(data));
        }
        
        /// <summary>
        /// Trigger an event with custom event args
        /// </summary>
        public void TriggerEvent(EventType eventType, EventArgs args)
        {
            if (_eventHandlers.ContainsKey(eventType))
            {
                EventHandler handler = _eventHandlers[eventType];
                handler?.Invoke(this, args);
            }
            if (_eventObservables.TryGetValue(eventType, out var observable))
            {
                observable.RaiseEvent(this, args);
            }
            EventData eventData = new EventData(eventType, args);
            _globalObservable.NotifyObservers(eventData);
        }
    }
}