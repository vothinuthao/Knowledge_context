using Core.Patterns;

namespace Troops.Base
{
    /// <summary>
    /// Dead behavior - troops lie dead on the ground
    /// </summary>
    public class DeadStrategy : ATroopBehaviorStrategyBase
    {
        public DeadStrategy() : base("Dead", 100) { }
        
        public override bool ShouldExecute(TroopBase troop)
        {
            return troop.CurrentState == TroopState.Dead;
        }
        
        public override void Execute(TroopBase troop)
        {
            troop.SteeringManager.ClearBehaviors();
            if (!troop.IsPlayingDeathAnimation)
            {
                troop.PlayDeathAnimation();
            }
        }
    }
}