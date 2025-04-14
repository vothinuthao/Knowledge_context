using Core;
using Core.Behaviors;
using Core.Patterns;

namespace Troops.Base
{
    /// <summary>
    /// Flee behavior - troops flee from stronger enemies
    /// </summary>
    public class FleeStrategy : ATroopBehaviorStrategyBase
    {
        public FleeStrategy() : base("Flee", 30) { }
        
        public override bool ShouldExecute(TroopBase troop)
        {
            return troop.CurrentHealthPercentage < 0.3f && troop.HasEnemyInRange;
        }
        
        public override void Execute(TroopBase troop)
        {
            troop.SteeringManager.ClearBehaviors();
            var flee = new FleeBehavior();
            troop.SteeringManager.AddBehavior(flee);
            troop.SteeringManager.SetTarget(troop.NearestEnemyTransform);
            EventManager.Instance.TriggerEvent(EventTypeInGame.TroopFleeing, troop);
        }
    }
}