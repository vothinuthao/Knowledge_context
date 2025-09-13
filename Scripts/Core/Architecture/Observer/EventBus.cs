using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RavenDeckbuilding.Core.Architecture.Observer
{
    /// <summary>
    /// Generic event bus for decoupled communication
    /// </summary>
    public class EventBus<TEventData> : ISubject<TEventData>
    {
        private List<IObserver<TEventData>> _observers;
        private List<IObserver<TEventData>> _observersToAdd;
        private List<IObserver<TEventData>> _observersToRemove;
        private bool _isNotifying = false;
        
        public EventBus()
        {
            _observers = new List<IObserver<TEventData>>();
            _observersToAdd = new List<IObserver<TEventData>>();
            _observersToRemove = new List<IObserver<TEventData>>();
        }
        
        public void Subscribe(IObserver<TEventData> observer)
        {
            if (observer == null) return;
            
            if (_isNotifying)
            {
                _observersToAdd.Add(observer);
            }
            else
            {
                if (!_observers.Contains(observer))
                {
                    _observers.Add(observer);
                    SortObservers();
                }
            }
        }
        
        public void Unsubscribe(IObserver<TEventData> observer)
        {
            if (observer == null) return;
            
            if (_isNotifying)
            {
                _observersToRemove.Add(observer);
            }
            else
            {
                _observers.Remove(observer);
            }
        }
        
        public void NotifyObservers(TEventData eventData)
        {
            _isNotifying = true;
            
            try
            {
                foreach (var observer in _observers.ToList()) // ToList to avoid modification during iteration
                {
                    if (observer != null && observer.ShouldReceiveEvent(eventData))
                    {
                        try
                        {
                            observer.OnNotify(eventData);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Observer notification error: {ex.Message}");
                        }
                    }
                }
            }
            finally
            {
                _isNotifying = false;
                ProcessPendingChanges();
            }
        }
        
        private void ProcessPendingChanges()
        {
            // Add pending observers
            foreach (var observer in _observersToAdd)
            {
                if (!_observers.Contains(observer))
                {
                    _observers.Add(observer);
                }
            }
            _observersToAdd.Clear();
            
            // Remove pending observers
            foreach (var observer in _observersToRemove)
            {
                _observers.Remove(observer);
            }
            _observersToRemove.Clear();
            
            if (_observersToAdd.Count > 0 || _observersToRemove.Count > 0)
            {
                SortObservers();
            }
        }
        
        private void SortObservers()
        {
            _observers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
        
        public void Clear()
        {
            _observers.Clear();
            _observersToAdd.Clear();
            _observersToRemove.Clear();
        }
        
        public int ObserverCount => _observers.Count;
    }
}