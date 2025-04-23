using Troop;
using UnityEngine;

public class AttackingState : TroopStateBase
    {
        private TroopController _currentTarget = null;
        private float _strafeTimer = 0f;
        private float _strafeInterval = 1.5f;
        private float _attackCooldown = 0f;
        private float _targetUpdateInterval = 0.5f;
        private float _targetUpdateTimer = 0f;
        private float _chaseDistance = 12f; // Khoảng cách tối đa mà troop sẽ đuổi theo kẻ địch
        private float _attackMovementSpeed = 1.2f; // Hệ số tốc độ di chuyển khi tấn công

        public AttackingState()
        {
            stateEnum = TroopState.Attacking;
            
            // Quy định các state có thể chuyển từ Attacking
            allowedTransitions.Add(typeof(IdleState));
            allowedTransitions.Add(typeof(MovingState));
            allowedTransitions.Add(typeof(DefendingState));
            allowedTransitions.Add(typeof(FleeingState));
            allowedTransitions.Add(typeof(StunnedState));
            allowedTransitions.Add(typeof(KnockbackState));
            allowedTransitions.Add(typeof(DeadState));
        }

        public override void Enter(TroopController troop)
        {
            // Tìm target gần nhất
            _currentTarget = FindBestTarget(troop);
            
            // Kích hoạt các behavior liên quan đến tấn công
            troop.EnableBehavior("Seek", true);
            troop.EnableBehavior("Arrival", true);
            troop.EnableBehavior("Separation", true);
            
            // Vô hiệu hóa các behavior làm chậm tốc độ tấn công
            troop.EnableBehavior("Cohesion", false);
            troop.EnableBehavior("Alignment", false);
            
            // Kích hoạt các behavior tấn công đặc biệt
            if (troop.IsBehaviorEnabled("Charge"))
                troop.EnableBehavior("Charge", true);
            
            if (troop.IsBehaviorEnabled("Jump Attack"))
                troop.EnableBehavior("Jump Attack", true);
            
            // Reset timers
            _strafeTimer = UnityEngine.Random.Range(0, _strafeInterval);
            _attackCooldown = 0f;
            _targetUpdateTimer = 0f;
            
            // Set target position
            if (_currentTarget != null)
            {
                troop.SetTargetPosition(_currentTarget.GetPosition());
            }
            
            // Tăng tốc độ di chuyển khi tấn công
            troop.GetModel().TemporarySpeedMultiplier = _attackMovementSpeed;
        }

        public override void Update(TroopController troop)
        {
            // Cập nhật timers
            _strafeTimer -= Time.deltaTime;
            _attackCooldown -= Time.deltaTime;
            _targetUpdateTimer -= Time.deltaTime;
            
            // Cập nhật target định kỳ
            if (_targetUpdateTimer <= 0f)
            {
                _targetUpdateTimer = _targetUpdateInterval;
                
                // Nếu không có target hoặc target đã chết
                if (_currentTarget == null || !_currentTarget.IsAlive())
                {
                    _currentTarget = FindBestTarget(troop);
                    
                    // Nếu không tìm thấy target nào, quay về idle
                    if (_currentTarget == null)
                    {
                        troop.StateMachine.ChangeState<IdleState>();
                        return;
                    }
                }
                
                // Cập nhật position của target
                troop.SetTargetPosition(_currentTarget.GetPosition());
            }
            
            // Nếu có target, xử lý tấn công
            if (_currentTarget != null && _currentTarget.IsAlive())
            {
                float distanceToTarget = Vector3.Distance(troop.GetPosition(), _currentTarget.GetPosition());
                
                // Nếu target ở ngoài chase distance và không phải đang trong squad, quay về idle
                if (distanceToTarget > _chaseDistance && TroopControllerSquadExtensions.Instance?.GetSquad(troop) == null)
                {
                    troop.StateMachine.ChangeState<IdleState>();
                    return;
                }
                
                // Nếu đủ gần để tấn công
                if (distanceToTarget <= troop.GetModel().AttackRange)
                {
                    // Xử lý tấn công
                    if (_attackCooldown <= 0f)
                    {
                        bool attackSuccess = troop.Attack(_currentTarget);
                        
                        if (attackSuccess)
                        {
                            // Reset attack cooldown dựa trên attack speed
                            _attackCooldown = 1f / troop.GetModel().AttackSpeed;
                        }
                    }
                    
                    // Xử lý strafe (di chuyển né tránh) khi đang cooldown
                    if (_attackCooldown > 0f && _strafeTimer <= 0f)
                    {
                        // Tính hướng strafe ngẫu nhiên
                        Vector3 toTarget = (_currentTarget.GetPosition() - troop.GetPosition()).normalized;
                        Vector3 strafeDir = new Vector3(-toTarget.z, 0, toTarget.x); // Vuông góc với hướng tới target
                        
                        // Đổi hướng strafe ngẫu nhiên
                        if (UnityEngine.Random.value < 0.5f)
                            strafeDir = -strafeDir;
                        
                        // Tính vị trí strafe
                        Vector3 strafePos = troop.GetPosition() + strafeDir * 2f;
                        
                        // Set target position tạm thời để strafe
                        troop.SetTargetPosition(strafePos);
                        
                        // Reset strafe timer
                        _strafeTimer = _strafeInterval;
                    }
                    else if (_strafeTimer <= 0f)
                    {
                        // Cập nhật position của target nếu không trong strafe
                        troop.SetTargetPosition(_currentTarget.GetPosition());
                    }
                }
                else
                {
                    // Ngoài tầm tấn công, di chuyển tới target
                    troop.SetTargetPosition(_currentTarget.GetPosition());
                }
            }
            
            // Kiểm tra health để quyết định có nên flee không
            float healthRatio = troop.GetModel().CurrentHealth / troop.GetModel().MaxHealth;
            
            if (healthRatio < 0.3f && Random.value < 0.01f)
            {
                troop.StateMachine.ChangeState<FleeingState>();
                return;
            }
        }

        public override void Exit(TroopController troop)
        {
            troop.EnableBehavior("Seek", true);
            troop.EnableBehavior("Arrival", true);
            troop.EnableBehavior("Separation", true);
            troop.EnableBehavior("Cohesion", true);
            troop.EnableBehavior("Alignment", true);
            
            if (troop.IsBehaviorEnabled("Charge"))
                troop.EnableBehavior("Charge", false);
            
            if (troop.IsBehaviorEnabled("Jump Attack"))
                troop.EnableBehavior("Jump Attack", false);
            
            troop.GetModel().TemporarySpeedMultiplier = 1.0f;
            
            _currentTarget = null;
        }
        
        private TroopController FindBestTarget(TroopController troop)
        {
            var enemies = troop.SteeringContext.NearbyEnemies;
            if (enemies == null || enemies.Length == 0) return null;
            
            TroopController bestTarget = null;
            float bestScore = float.MinValue;
            
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive()) continue;
                float dist = Vector3.Distance(troop.GetPosition(), enemy.GetPosition());
                if (dist > _chaseDistance) continue;
                
                float healthRatio = enemy.GetModel().CurrentHealth / enemy.GetModel().MaxHealth;
                float distScore = 1.0f - (dist / _chaseDistance);
                float score = distScore * 0.7f + (1.0f - healthRatio) * 0.3f;
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }
            return bestTarget;
        }
    }
