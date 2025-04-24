using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for jump attack behavior (leap at enemies)
    /// </summary>
    public class JumpAttackComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 2.0f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Maximum jump range
        public float JumpRange { get; set; } = 6.0f;
        
        // Jump speed
        public float JumpSpeed { get; set; } = 10.0f;
        
        // Jump height
        public float JumpHeight { get; set; } = 2.0f;
        
        // Damage multiplier when jump attacking
        public float DamageMultiplier { get; set; } = 1.5f;
        
        // Cooldown between jumps
        public float Cooldown { get; set; } = 5.0f;
        
        // Current cooldown timer
        public float CooldownTimer { get; set; } = 0f;
        
        // Whether currently jumping
        public bool IsJumping { get; set; } = false;
        
        // Jump target position
        public Vector3 JumpTarget { get; set; } = Vector3.zero;
        
        // Jump progress (0-1)
        public float JumpProgress { get; set; } = 0f;
        
        public JumpAttackComponent(float weight = 2.0f, float jumpRange = 6.0f, float jumpSpeed = 10.0f, 
            float jumpHeight = 2.0f, float damageMultiplier = 1.5f, float cooldown = 5.0f)
        {
            Weight = weight;
            JumpRange = jumpRange;
            JumpSpeed = jumpSpeed;
            JumpHeight = jumpHeight;
            DamageMultiplier = damageMultiplier;
            Cooldown = cooldown;
        }
    }
}