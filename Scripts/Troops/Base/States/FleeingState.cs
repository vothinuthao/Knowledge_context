using Core;
using UnityEngine;
using EventType = Core.EventType;

namespace Troops.Base
{
    /// <summary>
    /// Fleeing state - troop is running away from danger
    /// </summary>
    public class FleeingState : ATroopStateBase
    {
        private float _fleeingDuration = 0f;
        private float _maxFleeingDuration = 3.0f;
        
        public FleeingState() : base(TroopState.Fleeing) { }
        
        public override void Enter(TroopBase troop)
        {
            base.Enter(troop);
            _fleeingDuration = 0f;
            troop.PlayAnimation("Flee");
            
            // Notify others
            EventManager.Instance.TriggerEvent(EventType.TroopFleeing, troop);
        }
        
        public override void Update(TroopBase troop)
        {
            _fleeingDuration += Time.deltaTime;
            if (_fleeingDuration >= _maxFleeingDuration)
            {
                troop.ChangeState(new IdleState());
                return;
            }
            troop.FleeFromThreats();
        }
    }
}