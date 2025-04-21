using UnityEngine;

namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Protect Behavior", menuName = "Wiking Raven/Behaviors/Formation/Protect")]
    public class ProtectBehaviorSO : SteeringBehaviorSO
    {
        [Tooltip("Bán kính bảo vệ quanh đồng minh")]
        public float protectRadius = 3f;

        [Tooltip("Tốc độ di chuyển khi vào vị trí bảo vệ")]
        public float positioningSpeed = 4f;

        [Tooltip("Các tag cần ưu tiên bảo vệ (để trống nếu bảo vệ tất cả)")]
        public string[] protectedTags = new string[] { "Player" };

        [HideInInspector]
        public int priority = 2;

        public override ISteeringBehavior Create()
        {
            return new ProtectBehavior(weight, protectRadius, positioningSpeed, protectedTags);
        }
    }
}