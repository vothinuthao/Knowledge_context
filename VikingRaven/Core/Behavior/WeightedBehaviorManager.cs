using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Core.Behavior
{
    public class WeightedBehaviorManager : MonoBehaviour
    {
        private readonly List<IBehavior> _behaviors = new List<IBehavior>();
        private IBehavior _currentBehavior;
        private IEntity _entity;

        public void SetEntity(IEntity entity)
        {
            _entity = entity;
        }

        public void RegisterBehavior(IBehavior behavior)
        {
            if (!_behaviors.Contains(behavior))
            {
                _behaviors.Add(behavior);
                behavior.Initialize();
            }
        }

        public void UnregisterBehavior(IBehavior behavior)
        {
            if (_behaviors.Contains(behavior))
            {
                behavior.Cleanup();
                _behaviors.Remove(behavior);
            }
        }

        public void Update()
        {
            if (_entity == null || !_entity.IsActive)
                return;

            // Calculate weights for all behaviors
            foreach (var behavior in _behaviors.Where(b => b.IsActive))
            {
                behavior.CalculateWeight();
            }

            // Find the behavior with the highest weight
            var highestWeightBehavior = _behaviors
                .Where(b => b.IsActive)
                .OrderByDescending(b => b.Weight)
                .FirstOrDefault();

            // If we have a new highest weight behavior, switch to it
            if (highestWeightBehavior != null && highestWeightBehavior != _currentBehavior)
            {
                _currentBehavior = highestWeightBehavior;
                Debug.Log($"Entity {_entity.Id} switched to behavior: {_currentBehavior.Name}");
            }

            // Execute the current behavior
            _currentBehavior?.Execute();
        }

        public void CleanupAllBehaviors()
        {
            foreach (var behavior in _behaviors)
            {
                behavior.Cleanup();
            }
            
            _behaviors.Clear();
            _currentBehavior = null;
        }

        private void OnDestroy()
        {
            CleanupAllBehaviors();
        }
    }
}