using UnityEngine;

namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Testudo Behavior", menuName = "Wiking Raven/Behaviors/Formation/Testudo")]
    public class TestudoBehaviorSO : SteeringBehaviorSO
    {
        [Tooltip("Khoảng cách giữa các units trong đội hình")]
        public float formationSpacing = 1.2f;

        [Tooltip("Hệ số tốc độ di chuyển khi ở đội hình testudo")]
        [Range(0.1f, 1f)]
        public float movementSpeedMultiplier = 0.5f;

        [Tooltip("Bonus kháng knockback khi ở đội hình testudo")]
        [Range(0f, 1f)]
        public float knockbackResistanceBonus = 0.5f;

        [Tooltip("Bonus phòng thủ tấn công từ xa")]
        [Range(0f, 1f)]
        public float rangedDefenseBonus = 0.7f;

        [HideInInspector]
        public int priority = 3;

        public override ISteeringBehavior Create()
        {
            return new TestudoBehavior(weight, formationSpacing, movementSpeedMultiplier, 
                knockbackResistanceBonus, rangedDefenseBonus);
        }
    }
}