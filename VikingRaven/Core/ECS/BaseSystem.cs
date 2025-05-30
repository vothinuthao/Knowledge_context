﻿using UnityEngine;
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
        }

        public virtual void Initialize() 
        {
        }
        
        public abstract void Execute();
        
        public virtual void Cleanup() { }
    }
}