using UnityEngine;

namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Jump Attack Behavior", menuName = "Wiking Raven/Behaviors/Special/Jump Attack")]
    public class JumpAttackBehaviorSO : SteeringBehaviorSO
    {
        [Tooltip("Phạm vi tối đa mà troop sẽ thực hiện jump attack")]
        public float jumpRange = 5f;
        
        [Tooltip("Tốc độ của jump attack")]
        public float jumpSpeed = 2f;
        
        [Tooltip("Chiều cao tối đa của jump")]
        public float jumpHeight = 2f;
        
        [Tooltip("Hệ số nhân sát thương khi jump attack")]
        public float damageMultiplier = 1.5f;
        
        [Tooltip("Thời gian hồi (giây) giữa các lần jump")]
        public float cooldown = 5f;
        
        [HideInInspector]
        public int priority = 2;
        
        public override ISteeringBehavior Create()
        {
            return new JumpAttackBehavior(weight, jumpRange, jumpSpeed, jumpHeight, damageMultiplier, cooldown);
        }
    }
}
