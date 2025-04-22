using Troop;
using UnityEngine;

public class IdleState : TroopStateBase
{
    private float _idleTimer = 0f;
        private float _randomMoveChance = 0.01f;
        private float _maxIdleTime = 5f;

        public IdleState()
        {
            stateEnum = TroopState.Idle;
            
            // Quy định các state có thể chuyển từ Idle
            allowedTransitions.Add(typeof(MovingState));
            allowedTransitions.Add(typeof(AttackingState));
            allowedTransitions.Add(typeof(DefendingState));
            allowedTransitions.Add(typeof(FleeingState));
            allowedTransitions.Add(typeof(StunnedState));
            allowedTransitions.Add(typeof(KnockbackState));
            allowedTransitions.Add(typeof(DeadState));
        }

        public override void Enter(TroopController troop)
        {
            // Reset velocity
            troop.GetModel().Velocity = Vector3.zero;
            
            // Enable specific behaviors for idle
            troop.EnableBehavior("Seek", false);
            troop.EnableBehavior("Separation", true);
            troop.EnableBehavior("Cohesion", true);
            
            // Vô hiệu hóa các behavior không cần thiết
            troop.EnableBehavior("Flee", false);
            if (troop.IsBehaviorEnabled("Charge"))
                troop.EnableBehavior("Charge", false);
            if (troop.IsBehaviorEnabled("Jump Attack"))
                troop.EnableBehavior("Jump Attack", false);
            
            // Reset idle timer
            _idleTimer = UnityEngine.Random.Range(1f, _maxIdleTime);
        }

        public override void Update(TroopController troop)
        {
            _idleTimer -= Time.deltaTime;
            
            var squadSystem = TroopControllerSquadExtensions.Instance?.GetSquad(troop);
            if (squadSystem)
            {
                Vector2Int squadPos = TroopControllerSquadExtensions.Instance.GetSquadPosition(troop);
                Vector3 desiredPosition = squadSystem.GetPositionForTroop(squadSystem,squadPos.x, squadPos.y);
                
                float distToSquadPos = Vector3.Distance(troop.GetPosition(), desiredPosition);
                if (distToSquadPos > 1.0f)
                {
                    troop.SetTargetPosition(desiredPosition);
                    troop.StateMachine.ChangeState<MovingState>();
                    return;
                }
            }
            
            // Kiểm tra nếu có kẻ địch gần đó
            TroopController nearestEnemy = FindNearestEnemy(troop);
            if (nearestEnemy != null)
            {
                float distToEnemy = Vector3.Distance(troop.GetPosition(), nearestEnemy.GetPosition());
                
                if (distToEnemy < 8f)
                {
                    troop.SetTargetPosition(nearestEnemy.GetPosition());
                    troop.StateMachine.ChangeState<AttackingState>();
                    return;
                }
            }
            
            if (_idleTimer <= 0f && UnityEngine.Random.value < _randomMoveChance)
            {
                // Tìm một điểm ngẫu nhiên gần đó để di chuyển đến
                Vector3 randomOffset = new Vector3(
                    UnityEngine.Random.Range(-5f, 5f),
                    0,
                    UnityEngine.Random.Range(-5f, 5f)
                );
                
                Vector3 randomPoint = troop.GetPosition() + randomOffset;
                troop.SetTargetPosition(randomPoint);
                troop.StateMachine.ChangeState<MovingState>();
            }
        }

        public override void Exit(TroopController troop)
        {
            // Cleanup or reset things specific to idle state
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