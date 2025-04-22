using UnityEngine;
namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Obstacle Avoidance Behavior", menuName = "Wiking Raven/Behaviors/Obstacle Avoidance")]
    public class ObstacleAvoidanceBehaviorSO : SteeringBehaviorSO
    {
        [Tooltip("Khoảng cách tránh xa chướng ngại vật")]
        public float avoidDistance = 2f;
    
        [Tooltip("Khoảng cách nhìn trước")]
        public float lookAheadDistance = 5f;
    
        public override ISteeringBehavior Create()
        {
            return new ObstacleAvoidanceBehavior(weight, avoidDistance, lookAheadDistance);
        }
    }
}