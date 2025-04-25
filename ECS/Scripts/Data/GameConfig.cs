using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Viking Raven/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Grid Settings")]
        public int GridWidth = 20;
        public int GridHeight = 20;
        public float CellSize = 3.0f;
        
        [Header("Performance")]
        public int TargetFPS = 60;
        public bool EnableObjectPooling = true;
        public bool EnableBehaviorCulling = true;
        public float CullingDistance = 50.0f;
        
        [Header("Debug")]
        public bool EnableDebugMode = false;
        public bool ShowPerformanceOverlay = false;
        public bool ShowGridDebug = false;
        public bool ShowPathfindingDebug = false;
        
        [Header("Gameplay")]
        public float DefaultMoraleValue = 1.0f;
        public float MoraleDecayRate = 0.01f;
        public float CombatMoraleImpact = 0.2f;
        
        [Header("AI Settings")]
        public float AIUpdateInterval = 0.5f;
        public float AIDecisionDelay = 0.2f;
        public bool EnableAdvancedAI = false;
    }
}