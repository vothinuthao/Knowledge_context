using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Steering;

namespace VikingRaven.Units.Components
{
    public class SteeringComponent : BaseComponent
    {
        [SerializeField] private SteeringManager _steeringManager;
        
        public SteeringManager SteeringManager => _steeringManager;

        public override void Initialize()
        {
            if (_steeringManager == null)
            {
                // Create a new steering manager if one doesn't exist
                GameObject managerObject = new GameObject($"SteeringManager_{Entity.Id}");
                managerObject.transform.SetParent(transform);
                
                _steeringManager = managerObject.AddComponent<SteeringManager>();
                _steeringManager.SetEntity(Entity);
            }
        }

        public void AddBehavior(ISteeringBehavior behavior)
        {
            if (_steeringManager != null)
            {
                _steeringManager.RegisterBehavior(behavior);
            }
        }

        public void RemoveBehavior(ISteeringBehavior behavior)
        {
            if (_steeringManager != null)
            {
                _steeringManager.UnregisterBehavior(behavior);
            }
        }

        public override void Cleanup()
        {
            // Nothing specific to clean up
        }
    }
}