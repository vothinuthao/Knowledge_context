using UnityEngine;

namespace RavenDeckbuilding.Core
{
    /// <summary>
    /// Interface for all card components
    /// </summary>
    public interface ICardComponent
    {
        int ExecutionPriority { get; }
        ComponentCategory Category { get; }
        bool IsActiveThisFrame { get; }
        
        void Execute(in GameContext context);
        void Initialize(CardEntity owner);
        void Cleanup();
    }
    
    public enum ComponentCategory : byte
    {
        Targeting = 0,
        Effect = 1,
        Visual = 2,
        Audio = 3
    }
}