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
        private Dictionary<EventTypeInGame, EventObservable<EventArgs>> _eventObservables = new Dictionary<EventTypeInGame, EventObservable<System.EventArgs>>();
        private Dictionary<EventTypeInGame, EventHandler> _eventHandlers = new Dictionary<EventTypeInGame, System.EventHandler>();
        private Observable<EventData> _globalObservable = new Observable<EventData>();
        
        protected override void OnSingletonCreated()
        {
            foreach (EventTypeInGame eventType in Enum.GetValues(typeof(EventTypeInGame)))
            {
                _eventObservables[eventType] = new EventObservable<System.EventArgs>();
            }
        }
        
        /// <summary>
        /// Add a listener for a specific event type (traditional approach)
        /// </summary>
        public void AddListener(EventTypeInGame eventTypeInGame, System.EventHandler handler)
        {
            _eventHandlers.TryAdd(eventTypeInGame, null);
            _eventHandlers[eventTypeInGame] += handler;
        }
        
        /// <summary>
        /// Remove a listener for a specific event type (traditional approach)
        /// </summary>
        public void RemoveListener(EventTypeInGame eventTypeInGame, System.EventHandler handler)
        {
            if (_eventHandlers.ContainsKey(eventTypeInGame))
            {
                _eventHandlers[eventTypeInGame] -= handler;
            }
        }
        
        /// <summary>
        /// Subscribe to events using Observable pattern
        /// </summary>
        public void Subscribe<T>(EventTypeInGame eventTypeInGame, EventObservable<System.EventArgs>.EventHandler handler) where T : System.EventArgs
        {
            if (_eventObservables.TryGetValue(eventTypeInGame, out var observable))
            {
                observable.OnEvent += handler;
            }
        }
        
        /// <summary>
        /// Unsubscribe from events using Observable pattern
        /// </summary>
        public void Unsubscribe<T>(EventTypeInGame eventTypeInGame, EventObservable<System.EventArgs>.EventHandler handler) where T : System.EventArgs
        {
            if (_eventObservables.TryGetValue(eventTypeInGame, out var observable))
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
        public void TriggerEvent(EventTypeInGame eventTypeInGame)
        {
            TriggerEvent(eventTypeInGame, EventArgs.Empty);
        }
        
        /// <summary>
        /// Trigger an event with data
        /// </summary>
        public void TriggerEvent<T>(EventTypeInGame eventTypeInGame, T data)
        {
            TriggerEvent(eventTypeInGame, new EventArgs<T>(data));
        }
        
        /// <summary>
        /// Trigger an event with custom event args
        /// </summary>
        public void TriggerEvent(EventTypeInGame eventTypeInGame, EventArgs args)
        {
            if (_eventHandlers.ContainsKey(eventTypeInGame))
            {
                EventHandler handler = _eventHandlers[eventTypeInGame];
                handler?.Invoke(this, args);
            }
            if (_eventObservables.TryGetValue(eventTypeInGame, out var observable))
            {
                observable.RaiseEvent(this, args);
            }
            EventData eventData = new EventData(eventTypeInGame, args);
            _globalObservable.NotifyObservers(eventData);
        }
    }
}