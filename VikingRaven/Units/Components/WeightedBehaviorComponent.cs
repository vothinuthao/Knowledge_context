using UnityEngine;
using VikingRaven.Core.Behavior;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class WeightedBehaviorComponent : BaseComponent
    {
        [SerializeField] private WeightedBehaviorManager _behaviorManager;
        
        public WeightedBehaviorManager BehaviorManager => _behaviorManager;

        public override void Initialize()
        {
            if (_behaviorManager == null)
            {
                // Create a new behavior manager if one doesn't exist
                GameObject managerObject = new GameObject($"BehaviorManager_{Entity.Id}");
                managerObject.transform.SetParent(transform);
                
                _behaviorManager = managerObject.AddComponent<WeightedBehaviorManager>();
                _behaviorManager.SetEntity(Entity);
            }
        }

        public void AddBehavior(IBehavior behavior)
        {
            if (_behaviorManager != null)
            {
                _behaviorManager.RegisterBehavior(behavior);
            }
        }

        public void RemoveBehavior(IBehavior behavior)
        {
            if (_behaviorManager != null)
            {
                _behaviorManager.UnregisterBehavior(behavior);
            }
        }

        public override void Cleanup()
        {
            if (_behaviorManager != null)
            {
                _behaviorManager.CleanupAllBehaviors();
            }
        }
    }
}