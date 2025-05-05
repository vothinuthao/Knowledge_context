using UnityEngine;
using VikingRaven.Core.DI;

namespace VikingRaven.Core.ECS
{
    public abstract class BaseSystem : MonoBehaviour, ISystem
    {
        [SerializeField] private int _priority = 0;
        [SerializeField] private bool _isActive = true;
        
        public int Priority => _priority;
        public bool IsActive { get => _isActive; set => _isActive = value; }
        
        [Inject] protected IEntityRegistry EntityRegistry { get; set; }

        public virtual void Initialize() { }
        public abstract void Execute();
        public virtual void Cleanup() { }
    }
}