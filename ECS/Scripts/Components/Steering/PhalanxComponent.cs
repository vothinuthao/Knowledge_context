using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for phalanx formation behavior
    /// </summary>
    public class PhalanxComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 2.0f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Spacing between units in formation
        public float FormationSpacing { get; set; } = 1.5f;
        
        // Movement speed multiplier when in formation
        public float MovementSpeedMultiplier { get; set; } = 0.7f;
        
        // Maximum rows in formation
        public int MaxRowsInFormation { get; set; } = 3;
        
        public PhalanxComponent(float weight = 2.0f, float formationSpacing = 1.5f, 
            float movementSpeedMultiplier = 0.7f, int maxRowsInFormation = 3)
        {
            Weight = weight;
            FormationSpacing = formationSpacing;
            MovementSpeedMultiplier = movementSpeedMultiplier;
            MaxRowsInFormation = maxRowsInFormation;
        }
    }
}