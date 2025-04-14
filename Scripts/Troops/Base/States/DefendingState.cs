namespace Troops.Base
{
    /// <summary>
    /// Defending state - troop is defending a position
    /// </summary>
    public class DefendingState : ATroopStateBase
    {
        public DefendingState() : base(TroopState.Defending) { }
        
        public override void Enter(TroopBase troop)
        {
            base.Enter(troop);
            troop.PlayAnimation("Defend");
        }
        
        public override void Update(TroopBase troop)
        {
            if (troop.HasEnemyInRange && troop.IsInAttackRange)
            {
                troop.ChangeState(new AttackingState());
                return;
            }
            
            if (troop.IsSquadMoving)
            {
                troop.ChangeState(new MovingState());
                return;
            }
            troop.MaintainFormation();
        }
    }
}