using UnityEngine;

namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Phalanx Behavior", menuName = "Wiking Raven/Behaviors/Formation/Phalanx")]
    public class PhalanxBehaviorSO : SteeringBehaviorSO
    {
        [Tooltip("Khoảng cách giữa các units trong đội hình")]
        public float formationSpacing = 1.5f;

        [Tooltip("Hệ số tốc độ di chuyển khi ở đội hình phalanx")]
        [Range(0.1f, 1f)]
        public float movementSpeedMultiplier = 0.7f;

        [Tooltip("Số hàng tối đa trong đội hình")]
        [Range(1, 5)]
        public int maxRowsInFormation = 3;

        [HideInInspector]
        public int priority = 2;

        public override ISteeringBehavior Create()
        {
            return new PhalanxBehavior(weight, formationSpacing, movementSpeedMultiplier, maxRowsInFormation);
        }
    }
}