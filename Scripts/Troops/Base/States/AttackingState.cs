using UnityEngine;

namespace Troops.Base
{
    /// <summary>
    /// Attacking state - troop is attacking an enemy
    /// </summary>
    public class AttackingState : ATroopStateBase
    {
        private float _attackCooldown = 0f;
        private float _attackRate = 1.0f;
        
        public AttackingState() : base(TroopState.Attacking) { }
        
        public override void Enter(TroopBase troop)
        {
            base.Enter(troop);
            _attackCooldown = 0f;
            _attackRate = troop.AttackRate;
            troop.PlayAnimation("AttackReady");
        }
        
        public override void Update(TroopBase troop)
        {
            if (!troop.HasEnemyInRange)
            {
                if (troop.IsSquadMoving)
                {
                    troop.ChangeState(new MovingState());
                }
                else
                {
                    troop.ChangeState(new IdleState());
                }
                return;
            }
            
            if (troop.CurrentHealthPercentage < 0.3f && Random.value < 0.1f)
            {
                troop.ChangeState(new FleeingState());
                return;
            }
            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown <= 0f && troop.IsInAttackRange)
            {
                troop.Attack();
                _attackCooldown = 1f / _attackRate;
                troop.PlayAnimation("Attack");
            }
            else
            {
                troop.MoveToAttackTarget();
            }
        }
    }
}