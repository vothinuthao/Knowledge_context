using UnityEngine;
namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Path Following Behavior", menuName = "Wiking Raven/Behaviors/Path Following")]
    public class PathFollowingBehaviorSO : SteeringBehaviorSO
    {
        [Tooltip("Khoảng cách mà troop bắt đầu giảm tốc khi đến gần waypoint")]
        public float pathRadius = 3f;
    
        [Tooltip("Khoảng cách mà troop được xem là đã đến waypoint")]
        public float arrivalDistance = 0.5f;
    
        public override ISteeringBehavior Create()
        {
            return new PathFollowingBehavior(weight, pathRadius, arrivalDistance);
        }
    }
}