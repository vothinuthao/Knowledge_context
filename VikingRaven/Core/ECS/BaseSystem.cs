using UnityEngine;
using VikingRaven.Core.Factory;

namespace VikingRaven.Core.ECS
{
    public abstract class BaseSystem : MonoBehaviour, ISystem
    {
        [SerializeField] private int _priority = 0;
        [SerializeField] private bool _isActive = true;
        
        public int Priority
        {
            get => _priority;
            set => _priority = value;
        }

        public bool IsActive { get => _isActive; set => _isActive = value; }
        protected EntityRegistry EntityRegistry => EntityRegistry.Instance;

        private void Awake()
        {
            // Auto-register with SystemRegistry
            SystemRegistry.Instance?.RegisterSystem(this);
        }

        public virtual void Initialize() 
        {
        }
        
        public abstract void Execute();
        
        public virtual void Cleanup() { }
        
        private void OnDestroy()
        {
            // Auto-unregister from SystemRegistry when destroyed
            if (SystemRegistry.HasInstance)
            {
                SystemRegistry.Instance.UnregisterSystem(this);
            }
        }
    }
}