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
                // Small chance to flee when health is low
                troop.ChangeState(new FleeingState());
                return;
            }
            
            // Update attack cooldown
            _attackCooldown -= Time.deltaTime;
            
            // Attack if cooldown expired and in range
            if (_attackCooldown <= 0f && troop.IsInAttackRange)
            {
                // Perform attack
                troop.Attack();
                
                // Reset cooldown
                _attackCooldown = 1f / _attackRate;
                
                // Play attack animation
                troop.PlayAnimation("Attack");
            }
            else
            {
                // Move closer to target if needed
                troop.MoveToAttackTarget();
            }
        }
    }
}