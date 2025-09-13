using UnityEngine;
using RavenDeckbuilding.Core;

namespace RavenDeckbuilding.Systems.Cards
{
    /// <summary>
    /// Context for card execution - used by Command pattern
    /// </summary>
    public struct CardExecutionContext
    {
        public Player caster;
        public Player target;
        public Vector3 targetPosition;
        public Vector3 castDirection;
        public CardInstance cardInstance;
        public float executionTime;
        
        public static CardExecutionContext Create(Player caster, Vector3 targetPos, CardInstance card = null)
        {
            return new CardExecutionContext
            {
                caster = caster,
                targetPosition = targetPos,
                cardInstance = card,
                executionTime = Time.time
            };
        }
        
        public bool IsValid => caster != null;
    }
}