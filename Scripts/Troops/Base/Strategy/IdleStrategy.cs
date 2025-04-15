using Core.Behaviors;
using Core.Patterns;

namespace Troops.Base
{
    /// <summary>
    /// Idle behavior - troops stay in formation but don't move
    /// </summary>
    public class IdleStrategy : ATroopBehaviorStrategyBase
    {
        public IdleStrategy() : base("Idle", 0) { }
        public override bool ShouldExecute(TroopBase troop)
        {
            return true;
        }
        
        public override void Execute(TroopBase troop)
        {
            troop.SteeringManager.ClearBehaviors();
            var arrival = new ContextArrivalBehavior();
            troop.SteeringManager.AddBehavior(arrival);
            troop.SteeringManager.SetTarget(troop.FormationPositionTarget);
        }
    }
}