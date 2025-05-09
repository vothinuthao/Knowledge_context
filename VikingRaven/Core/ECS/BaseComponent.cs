using UnityEngine;

namespace VikingRaven.Core.ECS
{
    public abstract class BaseComponent : MonoBehaviour, IComponent
    {
        [SerializeField] private bool _isActive = true;
        
        private IEntity _entity;
        
        public bool IsActive { get => _isActive; set => _isActive = value; }
        public IEntity Entity { get => _entity; set => _entity = value; }

        public virtual void Initialize() { }
        public virtual void Cleanup() { }
    }
}