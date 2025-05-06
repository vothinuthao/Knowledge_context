using System;
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

        private void Update()
        {
            if (!IsActive)
                return;
                
            DetectEnemies();
        }

        private void DetectEnemies()
        {
            // This is a simplified version. In a real game, you might use Physics.OverlapSphere
            // or a more optimized spatial partitioning system
            
            var myTransform = Entity.GetComponent<TransformComponent>();
            if (myTransform == null)
                return;
                
            Collider[] colliders = Physics.OverlapSphere(myTransform.Position, _aggroRange, _enemyLayers);
            
            // Create a list of entities from colliders
            List<IEntity> newEnemiesInRange = new List<IEntity>();
            
            foreach (var collider in colliders)
            {
                // Try to get an entity from the collider's GameObject
                var entityComponent = collider.GetComponent<BaseEntity>();
                if (entityComponent != null)
                {
                    IEntity enemy = entityComponent;
                    newEnemiesInRange.Add(enemy);
                    
                    // Check if this is a new enemy
                    if (!_enemiesInRange.Contains(enemy))
                    {
                        OnEnemyDetected?.Invoke(enemy);
                    }
                }
            }
            
            // Check for enemies that went out of range
            foreach (var oldEnemy in _enemiesInRange)
            {
                if (!newEnemiesInRange.Contains(oldEnemy))
                {
                    OnEnemyLost?.Invoke(oldEnemy);
                }
            }
            
            // Update the list
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
    }
}