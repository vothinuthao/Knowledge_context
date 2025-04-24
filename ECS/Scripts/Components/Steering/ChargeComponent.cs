using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for charge behavior (rush at enemies with high speed and damage)
    /// </summary>
    public class ChargeComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 3.0f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Maximum charge distance
        public float ChargeDistance { get; set; } = 10.0f;
        
        // Speed multiplier when charging
        public float ChargeSpeedMultiplier { get; set; } = 2.0f;
        
        // Damage multiplier when charge hits
        public float ChargeDamageMultiplier { get; set; } = 2.0f;
        
        // Time to prepare before charging
        public float ChargePreparationTime { get; set; } = 1.0f;
        
        // Cooldown between charges
        public float ChargeCooldown { get; set; } = 8.0f;
        
        // Current cooldown timer
        public float CooldownTimer { get; set; } = 0f;
        
        // Current preparation timer
        public float PreparationTimer { get; set; } = 0f;
        
        // Whether currently charging
        public bool IsCharging { get; set; } = false;
        
        // Whether currently preparing to charge
        public bool IsPreparing { get; set; } = false;
        
        // Target position for charge
        public Vector3 ChargeTarget { get; set; } = Vector3.zero;
        
        // Direction of charge
        public Vector3 ChargeDirection { get; set; } = Vector3.zero;
        
        public ChargeComponent(float weight = 3.0f, float chargeDistance = 10.0f, float chargeSpeedMultiplier = 2.0f,
            float chargeDamageMultiplier = 2.0f, float chargePreparationTime = 1.0f, float chargeCooldown = 8.0f)
        {
            Weight = weight;
            ChargeDistance = chargeDistance;
            ChargeSpeedMultiplier = chargeSpeedMultiplier;
            ChargeDamageMultiplier = chargeDamageMultiplier;
            ChargePreparationTime = chargePreparationTime;
            ChargeCooldown = chargeCooldown;
        }
    }
}