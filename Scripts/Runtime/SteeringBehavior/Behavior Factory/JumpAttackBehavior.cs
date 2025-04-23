using Troop;
using UnityEngine;

namespace SteeringBehavior
{
    public class JumpAttackBehavior : SteeringBehaviorBase
    {
        private float _jumpRange;
        private float _jumpSpeed;
        private float _jumpHeight;
        private float _damageMultiplier;
        private float _cooldown;
        
        private float _cooldownTimer = 0f;
        private bool _isJumping = false;
        private Vector3 _jumpTarget;
        private float _jumpProgress = 0f;
        
        public JumpAttackBehavior(float weight, float jumpRange, float jumpSpeed, float jumpHeight, float damageMultiplier, float cooldown) 
            : base(weight, "Jump Attack", 2) // Ưu tiên cao hơn (2)
        {
            _jumpRange = jumpRange;
            _jumpSpeed = jumpSpeed;
            _jumpHeight = jumpHeight;
            _damageMultiplier = damageMultiplier;
            _cooldown = cooldown;
        }
        
        public override Vector3 Execute(SteeringContext context)
        {
            if (context.TroopModel == null) return Vector3.zero;
            
            // Cập nhật cooldown
            if (_cooldownTimer > 0)
            {
                _cooldownTimer -= context.DeltaTime;
                return Vector3.zero;
            }
            
            // Nếu đang trong quá trình nhảy
            if (_isJumping)
            {
                return ExecuteJump(context);
            }
            
            // Kiểm tra xem có mục tiêu nào trong tầm jump không
            if (context.NearbyEnemies != null && context.NearbyEnemies.Length > 0)
            {
                foreach (var enemy in context.NearbyEnemies)
                {
                    if (enemy == null) continue;
                    
                    float distance = Vector3.Distance(context.TroopModel.Position, enemy.GetPosition());
                    
                    if (distance <= _jumpRange && distance > context.TroopModel.AttackRange)
                    {
                        // Bắt đầu jump attack
                        _isJumping = true;
                        _jumpTarget = enemy.GetPosition();
                        _jumpProgress = 0f;
                        
                        // Trả về lực đầu tiên của jump
                        return StartJump(context);
                    }
                }
            }
            
            return Vector3.zero;
        }
        
        private Vector3 StartJump(SteeringContext context)
        {
            // Lấy hướng nhảy
            Vector3 jumpDirection = (_jumpTarget - context.TroopModel.Position).normalized;
            
            // Tính toán lực nhảy ban đầu
            Vector3 jumpForce = jumpDirection * _jumpSpeed * 2f; // Lực ban đầu mạnh hơn
            jumpForce.y = _jumpHeight; // Thêm thành phần theo chiều cao
            
            return jumpForce;
        }
        
        private Vector3 ExecuteJump(SteeringContext context)
        {
            // Cập nhật tiến trình nhảy
            _jumpProgress += context.DeltaTime * _jumpSpeed;
            
            // Nếu nhảy hoàn thành
            if (_jumpProgress >= 1.0f)
            {
                // Kết thúc nhảy
                _isJumping = false;
                _cooldownTimer = _cooldown;
                
                // Gây sát thương cho mục tiêu nếu trong tầm
                foreach (var enemy in context.NearbyEnemies)
                {
                    if (enemy == null) continue;
                    
                    float distance = Vector3.Distance(context.TroopModel.Position, enemy.GetPosition());
                    if (distance <= context.TroopModel.AttackRange * 1.5f)
                    {
                        // Gọi phương thức attack với damage tăng lên
                        TroopController troopController = context.TroopModel.GetComponent<TroopController>();
                        if (troopController != null)
                        {
                            // Gọi Attack với damage tăng lên theo _damageMultiplier
                            Vector3 direction = (enemy.GetPosition() - context.TroopModel.Position).normalized;
                            enemy.TakeDamage(
                                context.TroopModel.AttackPower * _damageMultiplier,
                                direction,
                                context.TroopModel.AttackPower * 1.5f,
                                0.5f); // Cơ hội choáng cao hơn
                        }
                    }
                }
                
                return Vector3.zero;
            }
            
            // Tính toán vị trí theo đường cong
            Vector3 start = context.TroopModel.Position;
            Vector3 target = _jumpTarget;
            float height = _jumpHeight * Mathf.Sin(_jumpProgress * Mathf.PI); // Đường cong sin cho chiều cao
            
            // Nội suy tuyến tính giữa start và target
            Vector3 horizontalPos = Vector3.Lerp(start, target, _jumpProgress);
            
            // Thêm thành phần chiều cao
            Vector3 nextPos = new Vector3(horizontalPos.x, horizontalPos.y + height, horizontalPos.z);
            
            // Tính vận tốc cần thiết để đến nextPos
            Vector3 velocity = (nextPos - context.TroopModel.Position) / context.DeltaTime;
            
            // Trả về lực lái để đạt được vận tốc đó
            return velocity - context.TroopModel.Velocity;
        }
    }
}