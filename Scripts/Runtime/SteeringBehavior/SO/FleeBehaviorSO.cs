using UnityEngine;

namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Flee Behavior", menuName = "Wiking Raven/Behaviors/Flee")]
    public class FleeBehaviorSO : SteeringBehaviorSO
    {
        [Tooltip("Khoảng cách tối đa mà troop sẽ bắt đầu bỏ chạy")]
        public float panicDistance = 5f;
    
        public override ISteeringBehavior Create()
        {
            return new FleeBehavior(weight, panicDistance);
        }
    }
}