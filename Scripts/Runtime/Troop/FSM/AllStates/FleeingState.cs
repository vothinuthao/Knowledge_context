using Troop;
using UnityEngine;

public class FleeingState : TroopStateBase
    {
        private TroopController _currentThreat = null;
        private float _fleeTimer = 0f;
        private float _fleeDuration = 5f; // Thời gian bỏ chạy
        private float _threatUpdateInterval = 0.5f;
        private float _threatUpdateTimer = 0f;
        private float _safeDistance = 15f; // Khoảng cách an toàn
        private float _fleeSpeedMultiplier = 1.3f; // Hệ số tốc độ khi bỏ chạy

        public FleeingState()
        {
            stateEnum = TroopState.Fleeing;
            
            // Quy định các state có thể chuyển từ Fleeing
            allowedTransitions.Add(typeof(IdleState));
            allowedTransitions.Add(typeof(MovingState));
            allowedTransitions.Add(typeof(AttackingState));
            allowedTransitions.Add(typeof(StunnedState));
            allowedTransitions.Add(typeof(KnockbackState));
            allowedTransitions.Add(typeof(DeadState));
        }

        public override void Enter(TroopController troop)
        {
            // Tìm mối đe dọa lớn nhất
            _currentThreat = FindBiggestThreat(troop);
            
            // Kích hoạt behavior Flee
            troop.EnableBehavior("Flee", true);
            troop.EnableBehavior("Seek", false);
            troop.EnableBehavior("Arrival", false);
            troop.EnableBehavior("Cohesion", false);
            troop.EnableBehavior("Alignment", false);
            
            // Giữ Separation để tránh các troop khác
            troop.EnableBehavior("Separation", true);
            
            // Vô hiệu hóa tất cả combat behaviors
            if (troop.IsBehaviorEnabled("Charge"))
                troop.EnableBehavior("Charge", false);
            
            if (troop.IsBehaviorEnabled("Jump Attack"))
                troop.EnableBehavior("Jump Attack", false);
            
            // Thiết lập vị trí cần tránh xa
            if (_currentThreat != null)
            {
                troop.SteeringContext.AvoidPosition = _currentThreat.GetPosition();
                troop.SteeringContext.IsInDanger = true;
            }
            
            // Reset timer
            _fleeTimer = _fleeDuration;
            _threatUpdateTimer = 0f;
            
            // Tăng tốc độ di chuyển
            troop.GetModel().TemporarySpeedMultiplier = _fleeSpeedMultiplier;
        }

        public override void Update(TroopController troop)
        {
            // Cập nhật timers
            _fleeTimer -= Time.deltaTime;
            _threatUpdateTimer -= Time.deltaTime;
            
            // Cập nhật mối đe dọa
            if (_threatUpdateTimer <= 0f)
            {
                _threatUpdateTimer = _threatUpdateInterval;
                TroopController newThreat = FindBiggestThreat(troop);
                
                // Cập nhật mối đe dọa nếu có
                if (newThreat != null)
                {
                    _currentThreat = newThreat;
                    troop.SteeringContext.AvoidPosition = _currentThreat.GetPosition();
                }
            }
            
            // Kiểm tra xem đã an toàn chưa
            bool isSafe = true;
            
            if (_currentThreat && _currentThreat.IsAlive())
            {
                float distanceToThreat = Vector3.Distance(troop.GetPosition(), _currentThreat.GetPosition());
                
                if (distanceToThreat < _safeDistance)
                {
                    isSafe = false;
                }
                
                // Cập nhật avoid position
                troop.SteeringContext.AvoidPosition = _currentThreat.GetPosition();
            }
            
            // Nếu đã an toàn hoặc hết thời gian flee
            if (isSafe || _fleeTimer <= 0f)
            {
                // Quay về idle
                troop.StateMachine.ChangeState<IdleState>();
                return;
            }
            
            var squadSystem = TroopControllerSquadExtensions.Instance?.GetSquad(troop);
            if (squadSystem)
            {
                // Tìm vị trí đến của squad
                Vector2Int squadPos = TroopControllerSquadExtensions.Instance.GetSquadPosition(troop);
                Vector3 squadTargetPos = squadSystem.GetPositionForTroop(squadSystem,squadPos.x, squadPos.y);
                
                // Tính vector từ mối đe dọa đến vị trí squad
                Vector3 threatToSquadDir = (squadTargetPos - _currentThreat.GetPosition()).normalized;
                
                // Tạo vị trí an toàn theo hướng đó
                Vector3 safePos = _currentThreat.GetPosition() + threatToSquadDir * (_safeDistance * 1.5f);
                
                // Thiết lập vị trí target (kết hợp giữa vị trí an toàn và vị trí squad)
                Vector3 targetPos = Vector3.Lerp(safePos, squadTargetPos, 0.3f);
                
                // Set target position để flee theo hướng đó
                troop.SetTargetPosition(targetPos);
            }
            else
            {
                // Nếu không có squad, flee theo hướng ngược với mối đe dọa
                Vector3 fleeDir = (troop.GetPosition() - _currentThreat.GetPosition()).normalized;
                Vector3 fleePos = troop.GetPosition() + fleeDir * _safeDistance;
                
                troop.SetTargetPosition(fleePos);
            }
        }

        public override void Exit(TroopController troop)
        {
            // Reset behaviors về mặc định
            troop.EnableBehavior("Flee", false);
            troop.EnableBehavior("Seek", true);
            troop.EnableBehavior("Arrival", true);
            troop.EnableBehavior("Cohesion", true);
            troop.EnableBehavior("Alignment", true);
            
            // Reset danger flag
            troop.SteeringContext.IsInDanger = false;
            
            // Reset speed multiplier
            troop.GetModel().TemporarySpeedMultiplier = 1.0f;
            
            // Clear threat
            _currentThreat = null;
        }
        
        private TroopController FindBiggestThreat(TroopController troop)
        {
            var enemies = troop.SteeringContext.NearbyEnemies;
            if (enemies == null || enemies.Length == 0) return null;
            
            TroopController biggestThreat = null;
            float highestThreatLevel = 0f;
            
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive()) continue;
                
                float distance = Vector3.Distance(troop.GetPosition(), enemy.GetPosition());
                float power = enemy.GetModel().AttackPower;
                
                // Threat level tỷ lệ thuận với sức mạnh, tỷ lệ nghịch với khoảng cách
                float threatLevel = power / (distance + 1f);
                
                // Ưu tiên enemies đang tấn công
                if (enemy.GetState() == TroopState.Attacking)
                {
                    threatLevel *= 1.5f;
                }
                
                if (threatLevel > highestThreatLevel)
                {
                    highestThreatLevel = threatLevel;
                    biggestThreat = enemy;
                }
            }
            
            return biggestThreat;
        }
    }