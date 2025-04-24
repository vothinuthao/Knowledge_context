using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for testudo formation behavior (turtle shell formation)
    /// </summary>
    public class TestudoComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 3.0f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Spacing between units in formation
        public float FormationSpacing { get; set; } = 1.0f;
        
        // Movement speed multiplier when in formation (very slow)
        public float MovementSpeedMultiplier { get; set; } = 0.5f;
        
        // Bonus to knockback resistance
        public float KnockbackResistanceBonus { get; set; } = 0.8f;
        
        // Bonus to ranged defense
        public float RangedDefenseBonus { get; set; } = 0.7f;
        
        public TestudoComponent(float weight = 3.0f, float formationSpacing = 1.0f, 
            float movementSpeedMultiplier = 0.5f, float knockbackResistanceBonus = 0.8f,
            float rangedDefenseBonus = 0.7f)
        {
            Weight = weight;
            FormationSpacing = formationSpacing;
            MovementSpeedMultiplier = movementSpeedMultiplier;
            KnockbackResistanceBonus = knockbackResistanceBonus;
            RangedDefenseBonus = rangedDefenseBonus;
        }
    }
}