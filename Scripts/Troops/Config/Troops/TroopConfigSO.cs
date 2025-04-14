using System.Collections.Generic;
using Core.Behaviors;
using UnityEngine;

namespace Troops.Config
{

    /// <summary>
    /// Scriptable Object to store troop configuration
    /// </summary>
    [CreateAssetMenu(fileName = "TroopConfig", menuName = "WikingRaven/TroopConfig")]
    public class TroopConfigSO : ScriptableObject
    {
        [Header("Basic Settings")]
        public string troopName;
        public TroopType troopType;
        public GameObject prefab;
        
        [Header("Stats")]
        public int health = 100;
        public float moveSpeed = 5f;
        public float attackDamage = 10f;
        public float attackRange = 1.5f;
        public float attackRate = 1.0f;
        public float detectionRange = 10f;
        
        [Header("Behavior Settings")]
        public List<BehaviorConfigSO> behaviors = new List<BehaviorConfigSO>();
        
        [Header("Formation Settings")]
        public float formationSpacing = 1.0f;
        public bool maintainFormation = true;
        
        [Header("Special Settings")]
        public float fleeProbability = 0.1f;
        public float hesitationProbability = 0.05f;
        
        /// <summary>
        /// Create a list of steering behaviors from the configuration
        /// </summary>
        public List<ISteeringComponent> CreateBehaviors()
        {
            List<ISteeringComponent> steeringBehaviors = new List<ISteeringComponent>();
            
            foreach (var behaviorConfig in behaviors)
            {
                ISteeringComponent behavior = behaviorConfig.CreateBehavior();
                if (behavior != null)
                {
                    steeringBehaviors.Add(behavior);
                }
            }
            
            return steeringBehaviors;
        }
        
        /// <summary>
        /// Apply steering context settings from this configuration
        /// </summary>
        public void ApplyToContext(SteeringContext context)
        {
            context.MaxSpeed = moveSpeed;
            foreach (var behaviorConfig in behaviors)
            {
                behaviorConfig.ApplyToContext(context);
            }
        }
    }
}