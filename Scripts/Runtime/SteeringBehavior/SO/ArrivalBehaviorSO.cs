using UnityEngine;
namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Arrival Behavior", menuName = "Wiking Raven/Behaviors/Arrival")]
    public class ArrivalBehaviorSO : SteeringBehaviorSO
    {
        [Tooltip("Khoảng cách mà troop bắt đầu giảm tốc")]
        public float slowingDistance = 3f;
    
        public override ISteeringBehavior Create()
        {
            return new ArrivalBehavior(weight, slowingDistance);
        }
    }
}