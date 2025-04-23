using Troop;
using UnityEngine;

public class MovingState : TroopStateBase
{
    private float _stuckTimer = 0f;
        private Vector3 _lastPosition;
        private float _stuckThreshold = 2f; // Thời gian tối đa trước khi coi là stuck
        private float _minMoveDistance = 0.2f; // Khoảng cách tối thiểu phải di chuyển để không bị coi là stuck

        public MovingState()
        {
            stateEnum = TroopState.Moving;
            
            // Quy định các state có thể chuyển từ Moving
            allowedTransitions.Add(typeof(IdleState));
            allowedTransitions.Add(typeof(AttackingState));
            allowedTransitions.Add(typeof(DefendingState));
            allowedTransitions.Add(typeof(FleeingState));
            allowedTransitions.Add(typeof(StunnedState));
            allowedTransitions.Add(typeof(KnockbackState));
            allowedTransitions.Add(typeof(DeadState));
        }

        public override void Enter(TroopController troop)
        {
            // Enable movement related behaviors
            troop.EnableBehavior("Seek", true);
            troop.EnableBehavior("Arrival", true);
            troop.EnableBehavior("Separation", true);
            troop.EnableBehavior("Cohesion", true);
            troop.EnableBehavior("Alignment", true);
            
            // Disable combat behaviors
            troop.EnableBehavior("Flee", false);
            if (troop.IsBehaviorEnabled("Charge"))
                troop.EnableBehavior("Charge", false);
            if (troop.IsBehaviorEnabled("Jump Attack"))
                troop.EnableBehavior("Jump Attack", false);
            
            // Kích hoạt ambush move nếu có behavior đó
            if (troop.IsBehaviorEnabled("Ambush Move"))
                troop.EnableBehavior("Ambush Move", true);
            
            // Reset stuck detection
            _stuckTimer = 0f;
            _lastPosition = troop.GetPosition();
        }

        public override void Update(TroopController troop)
        {
            // Kiểm tra xem đã đến đích chưa
            Vector3 targetPos = troop.GetTargetPosition();
            float distanceToTarget = Vector3.Distance(troop.GetPosition(), targetPos);
            
            if (distanceToTarget < 0.5f)
            {
                // Đã đến đích, chuyển sang idle
                troop.StateMachine.ChangeState<IdleState>();
                return;
            }
            
            // Kiểm tra xem có kẻ địch gần đó
            TroopController nearestEnemy = FindNearestEnemy(troop);
            if (nearestEnemy != null)
            {
                float distToEnemy = Vector3.Distance(troop.GetPosition(), nearestEnemy.GetPosition());
                
                // Nếu kẻ địch trong tầm attack, chuyển sang attacking
                if (distToEnemy < 2f * troop.GetModel().AttackRange)
                {
                    // Có 50% cơ hội chuyển sang tấn công
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        troop.SetTargetPosition(nearestEnemy.GetPosition());
                        troop.StateMachine.ChangeState<AttackingState>();
                        return;
                    }
                }
            }
            
            // Kiểm tra stuck detection
            float movedDistance = Vector3.Distance(troop.GetPosition(), _lastPosition);
            if (movedDistance < _minMoveDistance)
            {
                _stuckTimer += Time.deltaTime;
                
                // Nếu stuck quá lâu, thử tìm đường khác
                if (_stuckTimer > _stuckThreshold)
                {
                    // Thử tìm một vị trí gần đó để di chuyển
                    Vector3 randomOffset = new Vector3(
                        UnityEngine.Random.Range(-2f, 2f),
                        0,
                        UnityEngine.Random.Range(-2f, 2f)
                    );
                    
                    Vector3 alternatePoint = troop.GetPosition() + randomOffset;
                    troop.SetTargetPosition(alternatePoint);
                    
                    // Reset stuck timer
                    _stuckTimer = 0f;
                }
            }
            else
            {
                // Reset stuck timer nếu đang di chuyển bình thường
                _stuckTimer = 0f;
            }
            
            // Cập nhật vị trí cuối cùng
            _lastPosition = troop.GetPosition();
        }

        public override void Exit(TroopController troop)
        {
            // Turn off ambush move if it was enabled
            if (troop.IsBehaviorEnabled("Ambush Move"))
                troop.EnableBehavior("Ambush Move", false);
        }
        
        private TroopController FindNearestEnemy(TroopController troop)
        {
            var enemies = troop.SteeringContext.NearbyEnemies;
            if (enemies == null || enemies.Length == 0) return null;
            
            TroopController nearest = null;
            float minDist = float.MaxValue;
            
            foreach (var enemy in enemies)
            {
                if (!enemy || !enemy.IsAlive()) continue;
                
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