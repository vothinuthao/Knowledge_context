using Troop;
using UnityEngine;

namespace SteeringBehavior
{
    public class ChargeBehavior : SteeringBehaviorBase
    {
        private float _chargeDistance;
        private float _chargeSpeedMultiplier;
        private float _chargeDamageMultiplier;
        private float _chargePreparationTime;
        private float _chargeCooldown;
        
        private float _cooldownTimer = 0f;
        private float _preparationTimer = 0f;
        private bool _isCharging = false;
        private bool _isPreparing = false;
        private Vector3 _chargeTarget;
        private Vector3 _chargeDirection;
        
        public ChargeBehavior(float weight, float chargeDistance, float chargeSpeedMultiplier, float chargeDamageMultiplier, float chargePreparationTime, float chargeCooldown) 
            : base(weight, "Charge", 3) // Priority cao nhất
        {
            _chargeDistance = chargeDistance;
            _chargeSpeedMultiplier = chargeSpeedMultiplier;
            _chargeDamageMultiplier = chargeDamageMultiplier;
            _chargePreparationTime = chargePreparationTime;
            _chargeCooldown = chargeCooldown;
        }
        
        public override Vector3 Execute(SteeringContext context)
        {
            if (context.TroopModel == null) return Vector3.zero;
            
            // Cập nhật timers
            if (_cooldownTimer > 0)
            {
                _cooldownTimer -= context.DeltaTime;
                return Vector3.zero;
            }
            
            // Nếu đang trong giai đoạn chuẩn bị
            if (_isPreparing)
            {
                _preparationTimer -= context.DeltaTime;
                
                if (_preparationTimer <= 0)
                {
                    // Kết thúc chuẩn bị, bắt đầu charge
                    _isPreparing = false;
                    _isCharging = true;
                    
                    // Cập nhật hướng charge
                    _chargeDirection = (_chargeTarget - context.TroopModel.Position).normalized;
                    
                    // Trả về lực ban đầu của charge
                    return _chargeDirection * context.TroopModel.MoveSpeed * _chargeSpeedMultiplier;
                }
                
                // Trong khi chuẩn bị, giữ nguyên vị trí
                return -context.TroopModel.Velocity;
            }
            
            // Nếu đang charge
            if (_isCharging)
            {
                // Kiểm tra xem đã đi qua mục tiêu chưa
                float distanceToTarget = Vector3.Distance(context.TroopModel.Position, _chargeTarget);
                
                if (distanceToTarget < 0.5f)
                {
                    // Kết thúc charge
                    _isCharging = false;
                    _cooldownTimer = _chargeCooldown;
                    return Vector3.zero;
                }
                
                // Tiếp tục charge
                Vector3 chargeForce = _chargeDirection * context.TroopModel.MoveSpeed * _chargeSpeedMultiplier;
                
                // Kiểm tra va chạm với kẻ địch để gây sát thương
                CheckChargeCollisions(context);
                
                return chargeForce;
            }
            
            // Kiểm tra xem có kẻ địch nào trong tầm charge không
            if (context.NearbyEnemies != null && context.NearbyEnemies.Length > 0)
            {
                // Tìm kẻ địch trong tầm charge
                TroopController targetEnemy = null;
                float bestDistance = float.MaxValue;
                
                foreach (var enemy in context.NearbyEnemies)
                {
                    if (enemy == null) continue;
                    
                    float distance = Vector3.Distance(context.TroopModel.Position, enemy.GetPosition());
                    
                    if (distance <= _chargeDistance && distance < bestDistance)
                    {
                        bestDistance = distance;
                        targetEnemy = enemy;
                    }
                }
                
                if (targetEnemy != null)
                {
                    // Bắt đầu chuẩn bị charge
                    _isPreparing = true;
                    _preparationTimer = _chargePreparationTime;
                    _chargeTarget = targetEnemy.GetPosition();
                    
                    // Trả về lực để dừng lại và chuẩn bị
                    return -context.TroopModel.Velocity;
                }
            }
            
            return Vector3.zero;
        }
        
        private void CheckChargeCollisions(SteeringContext context)
        {
            if (context.NearbyEnemies == null) return;
            
            foreach (var enemy in context.NearbyEnemies)
            {
                if (enemy == null) continue;
                
                // Tính toán khoảng cách
                Vector3 toEnemy = enemy.GetPosition() - context.TroopModel.Position;
                float distance = toEnemy.magnitude;
                
                // Nếu gần và trong hướng charge
                if (distance < context.TroopModel.AttackRange * 1.5f && 
                    Vector3.Dot(toEnemy.normalized, _chargeDirection) > 0.7f)
                {
                    // Gây sát thương với damage tăng lên theo tốc độ
                    float speedFactor = context.TroopModel.Velocity.magnitude / context.TroopModel.MoveSpeed;
                    float bonusDamage = Mathf.Clamp01(speedFactor) * _chargeDamageMultiplier;
                    
                    enemy.TakeDamage(
                        context.TroopModel.AttackPower * (1f + bonusDamage),
                        _chargeDirection,
                        context.TroopModel.AttackPower * 2f, // Knockback mạnh
                        0.3f); // 30% cơ hội choáng
                }
            }
        }
    }
}