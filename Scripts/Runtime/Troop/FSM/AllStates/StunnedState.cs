using Troop;
using UnityEngine;

public class StunnedState : TroopStateBase
    {
        private float _stunDuration = 0f;
        private TroopState _previousState;

        public StunnedState()
        {
            stateEnum = TroopState.Stunned;
            
            // Có thể chuyển đến mọi state sau khi hết choáng
            allowedTransitions.Add(typeof(IdleState));
            allowedTransitions.Add(typeof(MovingState));
            allowedTransitions.Add(typeof(AttackingState));
            allowedTransitions.Add(typeof(DefendingState));
            allowedTransitions.Add(typeof(FleeingState));
            allowedTransitions.Add(typeof(KnockbackState));
            allowedTransitions.Add(typeof(DeadState));
        }

        public override void Enter(TroopController troop)
        {
            // Lưu lại state trước đó để có thể quay lại
            _previousState = troop.GetModel().PreviousState;
            
            // Vô hiệu hóa tất cả behaviors
            DisableAllBehaviors(troop);
            
            // Set velocity về 0
            troop.GetModel().Velocity = Vector3.zero;
            troop.GetModel().Acceleration = Vector3.zero;
            
            // Lấy thời gian stun từ troop
            _stunDuration = troop.StunTimer;
            
            // Kích hoạt animation stunned
            troop.TroopView.TriggerAnimation("Stunned");
        }

        public override void Update(TroopController troop)
        {
            // Luôn cập nhật thời gian stun từ troop controller
            _stunDuration = troop.StunTimer;
            
            // Nếu hết thời gian stun, quay về state trước đó hoặc idle
            if (_stunDuration <= 0)
            {
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
                return;
            }
        }

        public override void Exit(TroopController troop)
        {
            // Kích hoạt lại các behavior mặc định
            EnableDefaultBehaviors(troop);
        }
        
        private void DisableAllBehaviors(TroopController troop)
        {
            foreach (var behavior in troop.GetModel().SteeringBehavior.GetStrategies())
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