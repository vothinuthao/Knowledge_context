using System;
using System.Collections.Generic;
using UnityEngine;

namespace VikingRaven.Core.Events
{
    public class EventManager : MonoBehaviour
    {
        private static EventManager _instance;
        
        private Dictionary<Type, List<Action<GameEvent>>> _eventListeners = 
            new Dictionary<Type, List<Action<GameEvent>>>();
        
        private Queue<GameEvent> _eventQueue = new Queue<GameEvent>();
        
        public static EventManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("EventManager");
                    _instance = go.AddComponent<EventManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        public void RegisterListener<T>(Action<T> listener) where T : GameEvent
        {
            Type eventType = typeof(T);
            
            if (!_eventListeners.ContainsKey(eventType))
            {
                _eventListeners[eventType] = new List<Action<GameEvent>>();
            }
            
            // Wrap the typed listener into a generic one
            Action<GameEvent> wrapper = (e) => listener((T)e);
            
            _eventListeners[eventType].Add(wrapper);
        }
        
        public void UnregisterListener<T>(Action<T> listener) where T : GameEvent
        {
            Type eventType = typeof(T);
            
            if (!_eventListeners.ContainsKey(eventType))
                return;
                
            // We can't directly remove the wrapper, so we need to remove all instances
            // with the same target and method
            
            var list = _eventListeners[eventType];
            Delegate targetDelegate = listener;
            
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Delegate existingDelegate = list[i];
                
                if (existingDelegate.Target == targetDelegate.Target &&
                    existingDelegate.Method == targetDelegate.Method)
                {
                    list.RemoveAt(i);
                }
            }
            
            if (list.Count == 0)
            {
                _eventListeners.Remove(eventType);
            }
        }
        
        public void QueueEvent(GameEvent gameEvent)
        {
            _eventQueue.Enqueue(gameEvent);
        }
        
        public void TriggerEvent(GameEvent gameEvent)
        {
            Type type = gameEvent.GetType();
            
            if (_eventListeners.TryGetValue(type, out var listeners))
            {
                foreach (var listener in listeners)
                {
                    listener.Invoke(gameEvent);
                }
            }
        }
        
        private void Update()
        {
            // Process queued events
            while (_eventQueue.Count > 0)
            {
                GameEvent gameEvent = _eventQueue.Dequeue();
                TriggerEvent(gameEvent);
            }
        }
    }
}