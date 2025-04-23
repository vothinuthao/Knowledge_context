using UnityEngine;

namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Cover Behavior", menuName = "Wiking Raven/Behaviors/Formation/Cover")]
    public class CoverBehaviorSO : SteeringBehaviorSO
    {
        [Tooltip("Khoảng cách giữ với protector khi cover")]
        public float coverDistance = 2f;

        [Tooltip("Tốc độ di chuyển khi vào vị trí cover")]
        public float positioningSpeed = 3.5f;

        [HideInInspector]
        public int priority = 1;

        public override ISteeringBehavior Create()
        {
            return new CoverBehavior(weight, coverDistance, positioningSpeed);
        }
    }
}
