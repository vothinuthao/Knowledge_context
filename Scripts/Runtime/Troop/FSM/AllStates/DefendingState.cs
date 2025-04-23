using Troop;
using UnityEngine;

public class DefendingState : TroopStateBase
    {
        private TroopController _currentThreat = null;
        private Vector3 _defendPosition;
        private float _defendRadius = 5f;
        private float _threatUpdateInterval = 0.5f;
        private float _threatUpdateTimer = 0f;
        private float _attackCooldown = 0f;
        private float _maxDefendTime = 15f; // Thời gian tối đa ở trạng thái phòng thủ
        private float _defendTimer = 0f;

        public DefendingState()
        {
            stateEnum = TroopState.Defending;
            
            // Quy định các state có thể chuyển từ Defending
            allowedTransitions.Add(typeof(IdleState));
            allowedTransitions.Add(typeof(AttackingState));
            allowedTransitions.Add(typeof(MovingState));
            allowedTransitions.Add(typeof(FleeingState));
            allowedTransitions.Add(typeof(StunnedState));
            allowedTransitions.Add(typeof(KnockbackState));
            allowedTransitions.Add(typeof(DeadState));
        }

        public override void Enter(TroopController troop)
        {
            // Ghi nhớ vị trí phòng thủ
            _defendPosition = troop.GetPosition();
            
            // Tìm kẻ địch gần nhất là mối đe dọa
            _currentThreat = FindNearestEnemy(troop);
            
            // Kích hoạt các behavior phòng thủ
            troop.EnableBehavior("Seek", false); // Không đuổi theo kẻ địch
            troop.EnableBehavior("Arrival", true);
            troop.EnableBehavior("Separation", true);
            troop.EnableBehavior("Cohesion", true);
            
            // Kích hoạt các behavior phòng thủ đặc biệt
            if (troop.IsBehaviorEnabled("Protect"))
                troop.EnableBehavior("Protect", true);
            
            if (troop.IsBehaviorEnabled("Phalanx"))
                troop.EnableBehavior("Phalanx", true);
            
            if (troop.IsBehaviorEnabled("Testudo"))
                troop.EnableBehavior("Testudo", true);
            
            // Vô hiệu hóa các behavior tấn công
            if (troop.IsBehaviorEnabled("Charge"))
                troop.EnableBehavior("Charge", false);
            
            if (troop.IsBehaviorEnabled("Jump Attack"))
                troop.EnableBehavior("Jump Attack", false);
            
            // Reset timers
            _threatUpdateTimer = 0f;
            _attackCooldown = 0f;
            _defendTimer = 0f;
            
            // Set defend position
            troop.SetTargetPosition(_defendPosition);
        }

        public override void Update(TroopController troop)
        {
            // Cập nhật timers
            _threatUpdateTimer -= Time.deltaTime;
            _attackCooldown -= Time.deltaTime;
            _defendTimer += Time.deltaTime;
            
            // Cập nhật mối đe dọa định kỳ
            if (_threatUpdateTimer <= 0f)
            {
                _threatUpdateTimer = _threatUpdateInterval;
                _currentThreat = FindNearestEnemy(troop);
            }
            
            // Luôn giữ ở gần vị trí phòng thủ
            float distanceToDefendPos = Vector3.Distance(troop.GetPosition(), _defendPosition);
            
            // Nếu đã đi quá xa vị trí phòng thủ, quay lại
            if (distanceToDefendPos > _defendRadius)
            {
                troop.SetTargetPosition(_defendPosition);
            }
            
            // Nếu có kẻ địch trong tầm tấn công, tấn công nhưng vẫn ở trạng thái phòng thủ
            if (_currentThreat != null && _currentThreat.IsAlive())
            {
                float distanceToThreat = Vector3.Distance(troop.GetPosition(), _currentThreat.GetPosition());
                
                if (distanceToThreat <= troop.GetModel().AttackRange && _attackCooldown <= 0f)
                {
                    // Thực hiện tấn công
                    bool attackSuccess = troop.Attack(_currentThreat);
                    
                    if (attackSuccess)
                    {
                        // Reset attack cooldown
                        _attackCooldown = 1f / troop.GetModel().AttackSpeed;
                    }
                }
                
                // Nếu kẻ địch vào quá gần, có thể chuyển sang tấn công
                if (distanceToThreat < _defendRadius / 2f && UnityEngine.Random.value < 0.05f)
                {
                    troop.StateMachine.ChangeState<AttackingState>();
                    return;
                }
            }
            
            // Nếu đã phòng thủ quá lâu mà không có mối đe dọa, quay về idle
            if (_defendTimer > _maxDefendTime && _currentThreat == null)
            {
                troop.StateMachine.ChangeState<IdleState>();
                return;
            }
            
            // Kiểm tra health để quyết định có nên flee không
            float healthRatio = troop.GetModel().CurrentHealth / troop.GetModel().MaxHealth;
            
            // Nếu health quá thấp, có cơ hội chuyển sang Fleeing
            if (healthRatio < 0.2f && UnityEngine.Random.value < 0.01f)
            {
                troop.StateMachine.ChangeState<FleeingState>();
                return;
            }
        }

        public override void Exit(TroopController troop)
        {
            // Reset các behavior về mặc định
            troop.EnableBehavior("Seek", true);
            troop.EnableBehavior("Arrival", true);
            troop.EnableBehavior("Separation", true);
            troop.EnableBehavior("Cohesion", true);
            
            // Vô hiệu hóa các behavior phòng thủ đặc biệt
            if (troop.IsBehaviorEnabled("Protect"))
                troop.EnableBehavior("Protect", false);
            
            if (troop.IsBehaviorEnabled("Phalanx"))
                troop.EnableBehavior("Phalanx", false);
            
            if (troop.IsBehaviorEnabled("Testudo"))
                troop.EnableBehavior("Testudo", false);
            
            // Clear threat
            _currentThreat = null;
        }
        
        private TroopController FindNearestEnemy(TroopController troop)
        {
            var enemies = troop.SteeringContext.NearbyEnemies;
            if (enemies == null || enemies.Length == 0) return null;
            
            TroopController nearest = null;
            float minDist = float.MaxValue;
            
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive()) continue;
                
                float dist = Vector3.Distance(troop.GetPosition(), enemy.GetPosition());
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = enemy;
                }
            }
            
            return nearest;
        }
    }