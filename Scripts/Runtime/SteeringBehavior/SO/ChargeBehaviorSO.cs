using UnityEngine;

namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Charge Behavior", menuName = "Wiking Raven/Behaviors/Special/Charge")]
    public class ChargeBehaviorSO : SteeringBehaviorSO
    {
        [Tooltip("Khoảng cách tối đa để bắt đầu charge")]
        public float chargeDistance = 10f;
        
        [Tooltip("Hệ số tăng tốc độ khi charge")]
        public float chargeSpeedMultiplier = 2.5f;
        
        [Tooltip("Hệ số tăng sát thương khi charge")]
        public float chargeDamageMultiplier = 2f;
        
        [Tooltip("Thời gian chuẩn bị trước khi charge (giây)")]
        public float chargePreparationTime = 1.5f;
        
        [Tooltip("Thời gian hồi chiêu (giây)")]
        public float chargeCooldown = 10f;
        
        [HideInInspector]
        public int priority = 3;
        
        public override ISteeringBehavior Create()
        {
            return new ChargeBehavior(weight, chargeDistance, chargeSpeedMultiplier, chargeDamageMultiplier, chargePreparationTime, chargeCooldown);
        }
    }
}