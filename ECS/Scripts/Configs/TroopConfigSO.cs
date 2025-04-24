using Steering.Config;
using UnityEngine;

namespace Configs
{
    /// <summary>
    /// ScriptableObject for configuring troop properties and behaviors
    /// </summary>
    [CreateAssetMenu(fileName = "TroopConfig", menuName = "Wiking Raven/Troop Config")]
    public class TroopConfigSO : ScriptableObject
    {
        // Basic troop properties
        [Header("Basic Properties")]
        public string TroopName = "Viking Warrior";
        public float MaxHealth = 100.0f;
        public float MoveSpeed = 3.5f;
        public float AttackPower = 10.0f;
        public float AttackRange = 2.0f;
        public float AttackCooldown = 1.0f;
        
        // Defense properties
        [Header("Defense Properties")]
        public float Armor = 5.0f;
        public float KnockbackResistance = 0.2f;
        public float StunResistance = 0.1f;
        
        // Steering behavior configuration
        [Header("Steering Behavior")]
        public SteeringBehaviorConfig SteeringConfig;
        
        // AI behavior configuration
        [Header("AI Behavior")]
        public float AggroRange = 8.0f;
        public float AwarenessRange = 15.0f;
        public bool CanFlee = true;
        public bool CanPatrol = true;
        
        // Visual and audio configuration
        [Header("Visual & Audio")]
        public GameObject ModelPrefab;
        public AudioClip[] AttackSounds;
        public AudioClip[] HurtSounds;
        public AudioClip[] DeathSounds;
        public ParticleSystem[] AttackEffects;
    }
}