using Core.Behaviors;
using Core.Patterns;

namespace Troops.Base
{
    /// <summary>
    /// Move behavior - troops move to target position
    /// </summary>
    public class MoveStrategy : ATroopBehaviorStrategyBase
    {
        public MoveStrategy() : base("Move", 10) { }
        
        public override bool ShouldExecute(TroopBase troop)
        {
            return troop.IsSquadMoving && troop.SquadMoveTarget != null;
        }
        
        public override void Execute(TroopBase troop)
        {
            troop.SteeringManager.ClearBehaviors();
            var cohesion = new ContextCohesionBehavior();
            troop.SteeringManager.AddBehavior(cohesion);
            var separation = new ContextSeparationBehavior();
            troop.SteeringManager.AddBehavior(separation);
            var arrival = new ContextArrivalBehavior();
            troop.SteeringManager.AddBehavior(arrival);
            var obstacleAvoidance = new ContextObstacleAvoidanceBehavior();
            troop.SteeringManager.AddBehavior(obstacleAvoidance);
            troop.SteeringManager.SetTarget(troop.FormationPositionTarget);
        }
    }
}