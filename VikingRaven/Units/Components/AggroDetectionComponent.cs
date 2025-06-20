﻿using System;
using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class AggroDetectionComponent : BaseComponent
    {
        [SerializeField] private float _aggroRange = 10f;
        [SerializeField] private LayerMask _enemyLayers;
        [SerializeField] private List<IEntity> _enemiesInRange = new List<IEntity>();
        
        public float AggroRange => _aggroRange;
        
        public event Action<IEntity> OnEnemyDetected;
        public event Action<IEntity> OnEnemyLost;
        
        
        private void Awake()
        {
            _enemiesInRange ??= new List<IEntity>();
            if (_enemyLayers == 0)
            {
                _enemyLayers = LayerMask.GetMask("Enemy"); 
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            if (_enemiesInRange == null)
            {
                _enemiesInRange = new List<IEntity>();
            }
        }
        
        
        private void Update()
        {
            if (!IsActive)
                return;
        
            if (Entity == null)
            {
                return;
            }
            try
            {
                DetectEnemies();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Exception in AggroDetectionComponent.Update: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void DetectEnemies()
        {
            if (Entity == null)
            {
                Debug.LogError("AggroDetectionComponent: Entity is null");
                return;
            }
            
            var myTransform = Entity.GetComponent<TransformComponent>();
            if (myTransform == null)
                return;
                
            Collider[] colliders = Physics.OverlapSphere(myTransform.Position, _aggroRange, _enemyLayers);
            List<IEntity> newEnemiesInRange = new List<IEntity>();
            
            foreach (var collider in colliders)
            {
                var entityComponent = collider.GetComponent<BaseEntity>();
                if (entityComponent != null)
                {
                    IEntity enemy = entityComponent;
                    newEnemiesInRange.Add(enemy);
                    if (!_enemiesInRange.Contains(enemy))
                    {
                        OnEnemyDetected?.Invoke(enemy);
                    }
                }
            }
            foreach (var oldEnemy in _enemiesInRange)
            {
                if (!newEnemiesInRange.Contains(oldEnemy))
                {
                    OnEnemyLost?.Invoke(oldEnemy);
                }
            }
            _enemiesInRange = newEnemiesInRange;
        }

        public bool HasEnemyInRange()
        {
            return _enemiesInRange.Count > 0;
        }

        public IEntity GetClosestEnemy()
        {
            if (!HasEnemyInRange())
                return null;
                
            var myTransform = Entity.GetComponent<TransformComponent>();
            if (myTransform == null)
                return _enemiesInRange[0];
                
            IEntity closestEnemy = null;
            float closestDistance = float.MaxValue;
            
            foreach (var enemy in _enemiesInRange)
            {
                var enemyTransform = enemy.GetComponent<TransformComponent>();
                if (enemyTransform == null)
                    continue;
                    
                float distance = Vector3.Distance(myTransform.Position, enemyTransform.Position);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
            
            return closestEnemy;
        }

        public void SetAggroRange(float aggroRange)
        {
            _aggroRange = aggroRange;
        }
    }
}