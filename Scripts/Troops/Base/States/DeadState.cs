using Core;

namespace Troops.Base
{
    /// <summary>
    /// Dead state - troop is dead
    /// </summary>
    public class DeadState : ATroopStateBase
    {
        public DeadState() : base(TroopState.Dead) { }
        
        public override void Enter(TroopBase troop)
        {
            base.Enter(troop);
            troop.PlayAnimation("Death");
            troop.DisableCollisions();
            EventManager.Instance.TriggerEvent(EventTypeInGame.TroopDeath, troop);
        }
        
        public override void Update(TroopBase troop)
        {
            
        }
    }
}