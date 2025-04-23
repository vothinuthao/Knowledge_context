using UnityEngine;

namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Cohesion Behavior", menuName = "Wiking Raven/Behaviors/Cohesion")]
    public class CohesionBehaviorSO : SteeringBehaviorSO
    {
        [Tooltip("Khoảng cách tối đa mà troop sẽ xem xét khi tính cohesion")]
        public float cohesionRadius = 10f;
    
        public override ISteeringBehavior Create()
        {
            return new CohesionBehavior(weight, cohesionRadius);
        }
    }
}