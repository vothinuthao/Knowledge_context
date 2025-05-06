using UnityEngine;

namespace VikingRaven.Core.Events
{
    public abstract class EventListener : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            RegisterEvents();
        }
        
        protected virtual void OnDisable()
        {
            UnregisterEvents();
        }
        
        protected abstract void RegisterEvents();
        protected abstract void UnregisterEvents();
    }
}