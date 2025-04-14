namespace Troops.Base
{
    /// <summary>
    /// Moving state - troop is moving to a target
    /// </summary>
    public class MovingState : ATroopStateBase
    {
        public MovingState() : base(TroopState.Moving) { }
        public override void Enter(TroopBase troop)
        {
            base.Enter(troop);
            troop.PlayAnimation("Move");
        }
        
        public override void Update(TroopBase troop)
        {
            if (!troop.IsSquadMoving)
            {
                troop.ChangeState(new IdleState());
                return;
            }
            
            if (troop.HasEnemyInRange && troop.IsInAttackRange)
            {
                troop.ChangeState(new AttackingState());
                return;
            }
            troop.MoveWithSquad();
        }
    }
}