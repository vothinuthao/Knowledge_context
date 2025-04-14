using Core.Behaviors;
using Core.Patterns;

namespace Troops.Base
{
    /// <summary>
    /// Attack behavior - troops attack nearby enemies
    /// </summary>
    public class AttackStrategy : ATroopBehaviorStrategyBase
    {
        public AttackStrategy() : base("Attack", 20) { }
        
        public override bool ShouldExecute(TroopBase troop)
        {
            return troop.HasEnemyInRange && troop.CurrentState != TroopState.Dead;
        }
        
        public override void Execute(TroopBase troop)
        {
            troop.SteeringManager.ClearBehaviors();
            var seek = new SeekBehavior();
            troop.SteeringManager.AddBehavior(seek);
            var separation = new SeparationBehavior();
            troop.SteeringManager.AddBehavior(separation);
            troop.SteeringManager.SetTarget(troop.NearestEnemyTransform);
            if (troop.IsInAttackRange)
            {
                troop.Attack();
            }
        }
    }
}