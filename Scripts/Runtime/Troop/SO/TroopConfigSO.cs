using System.Collections.Generic;
using SteeringBehavior;
using UnityEngine;

namespace Troop
{
    [CreateAssetMenu(fileName = "New Troop Config", menuName = "Wiking Raven/Troop Config")]
    public class TroopConfigSO : ScriptableObject
    {
        [Header("Basic Stats")]
        public string troopName;
        public float health;
        public float attackPower;
        public float moveSpeed;
        public float attackRange;
        public float attackSpeed;
    
        [Header("Visuals")]
        public GameObject troopPrefab;
        public RuntimeAnimatorController animatorController;
    
        [Header("Behaviors")]
        public List<SteeringBehaviorSO> behaviors = new List<SteeringBehaviorSO>();
    }
}