namespace Troops.Base
{
    /// <summary>
    /// Idle state - troop is not moving or attacking
    /// </summary>
    public class IdleState : ATroopStateBase
    {
        public IdleState() : base(TroopState.Idle)
        {
        }

        public override void Enter(TroopBase troop)
        {
            base.Enter(troop);
            troop.SteeringManager.ClearBehaviors();
            troop.PlayAnimation("Idle");
        }

        public override void Update(TroopBase troop)
        {
            if (troop.IsSquadMoving)
            {
                troop.ChangeState(new MovingState());
                return;
            }

            if (troop.HasEnemyInRange)
            {
                troop.ChangeState(new AttackingState());
                return;
            }
            troop.MaintainFormation();
        }
    }
}