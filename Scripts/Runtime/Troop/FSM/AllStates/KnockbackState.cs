using Troop;
using UnityEngine;

public class KnockbackState : TroopStateBase
    {
        private float _knockbackDuration = 0f;
        private TroopState _previousState;
        private float _recoveryTime = 0.5f; // Thời gian hồi phục sau knockback
        private float _recoveryTimer = 0f;

        public KnockbackState()
        {
            stateEnum = TroopState.Knockback;
            
            // Có thể chuyển đến mọi state sau khi hết knockback
            allowedTransitions.Add(typeof(IdleState));
            allowedTransitions.Add(typeof(MovingState));
            allowedTransitions.Add(typeof(AttackingState));
            allowedTransitions.Add(typeof(DefendingState));
            allowedTransitions.Add(typeof(FleeingState));
            allowedTransitions.Add(typeof(StunnedState));
            allowedTransitions.Add(typeof(DeadState));
        }

        public override void Enter(TroopController troop)
        {
            // Lưu lại state trước đó để có thể quay lại
            _previousState = troop.GetModel().PreviousState;
            
            // Vô hiệu hóa tất cả behaviors ngoại trừ knockback
            DisableAllBehaviors(troop);
            
            // Lấy thời gian knockback từ troop
            _knockbackDuration = troop.KnockbackTimer;
            _recoveryTimer = _recoveryTime;
            
            // Kích hoạt animation knockback
            troop.TroopView.TriggerAnimation("Knockback");
        }

        public override void Update(TroopController troop)
        {
            _knockbackDuration = troop.KnockbackTimer;
            
            // Nếu hết thời gian knockback, vào giai đoạn hồi phục
            if (_knockbackDuration <= 0)
            {
                // Giảm dần velocity trong giai đoạn hồi phục
                troop.GetModel().Velocity *= 0.9f;
                
                _recoveryTimer -= Time.deltaTime;
                
                // Nếu đã hồi phục xong, quay về state trước đó hoặc idle
                if (_recoveryTimer <= 0)
                {
                    // Kiểm tra health để quyết định có nên flee không
                    float healthRatio = troop.GetModel().CurrentHealth / troop.GetModel().MaxHealth;
                    
                    // Nếu health quá thấp, chuyển sang Fleeing
                    if (healthRatio < 0.3f && UnityEngine.Random.value < 0.3f)
                    {
                        troop.StateMachine.ChangeState<FleeingState>();
                        return;
                    }
                    
                    // Quay về state trước đó hoặc idle
                    switch (_previousState)
                    {
                        case TroopState.Moving:
                            troop.StateMachine.ChangeState<MovingState>();
                            break;
                        case TroopState.Attacking:
                            troop.StateMachine.ChangeState<AttackingState>();
                            break;
                        case TroopState.Defending:
                            troop.StateMachine.ChangeState<DefendingState>();
                            break;
                        case TroopState.Fleeing:
                            troop.StateMachine.ChangeState<FleeingState>();
                            break;
                        default:
                            troop.StateMachine.ChangeState<IdleState>();
                            break;
                    }
                }
            }
            else
            {
                // Trong knockback, velocity sẽ được xử lý trực tiếp bởi ApplyKnockback
                // và giảm dần theo thời gian
                troop.GetModel().Velocity *= 0.95f;
            }
        }

        public override void Exit(TroopController troop)
        {
            // Kích hoạt lại các behavior mặc định
            EnableDefaultBehaviors(troop);
            
            // Reset velocity
            troop.GetModel().Velocity = Vector3.zero;
        }
        
        private void DisableAllBehaviors(TroopController troop)
        {
            foreach (var behavior in troop.GetModel().SteeringBehavior.GetSteeringBehaviors())
            {
                troop.EnableBehavior(behavior.GetName(), false);
            }
        }
        
        private void EnableDefaultBehaviors(TroopController troop)
        {
            troop.EnableBehavior("Seek", true);
            troop.EnableBehavior("Arrival", true);
            troop.EnableBehavior("Separation", true);
            troop.EnableBehavior("Cohesion", true);
            troop.EnableBehavior("Alignment", true);
        }
    }
