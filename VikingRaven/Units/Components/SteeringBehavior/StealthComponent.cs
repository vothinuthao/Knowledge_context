using System;
using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class StealthComponent : BaseComponent
    {
        [SerializeField] private bool _isStealthed = false;
        [SerializeField] private float _stealthMovementSpeedFactor = 0.5f;
        [SerializeField] private float _detectionRadius = 5f;
        [SerializeField] private float _breakStealthDuration = 0.5f;
        
        private float _breakStealthTimer = 0f;
        
        public bool IsStealthed => _isStealthed && _breakStealthTimer <= 0f;
        public float StealthMovementSpeedFactor => _stealthMovementSpeedFactor;
        public float DetectionRadius => _detectionRadius;

        public event Action OnStealthEnter;
        public event Action OnStealthExit;
        public event Action<IEntity> OnDetected;

        public void EnterStealth()
        {
            if (!_isStealthed)
            {
                _isStealthed = true;
                OnStealthEnter?.Invoke();
            }
        }

        public void ExitStealth()
        {
            if (_isStealthed)
            {
                _isStealthed = false;
                OnStealthExit?.Invoke();
            }
        }

        public void BreakStealthTemporarily()
        {
            _breakStealthTimer = _breakStealthDuration;
        }

        private void Update()
        {
            if (_breakStealthTimer > 0)
            {
                _breakStealthTimer -= Time.deltaTime;
            }
        }

        // Check if an entity can detect this stealthed unit
        public bool CanBeDetectedBy(IEntity entity)
        {
            if (!IsStealthed)
                return true;
                
            var myTransform = Entity.GetComponent<TransformComponent>();
            var entityTransform = entity.GetComponent<TransformComponent>();
            
            if (myTransform == null || entityTransform == null)
                return false;
                
            // Calculate distance
            float distance = Vector3.Distance(myTransform.Position, entityTransform.Position);
            
            // If entity is within detection radius, it can detect this stealthed unit
            return distance <= _detectionRadius;
        }

        // Used when taking damage to break stealth
        public void TakeDamage(float amount, IEntity source)
        {
            BreakStealthTemporarily();
            OnDetected?.Invoke(source);
        }
    }
}