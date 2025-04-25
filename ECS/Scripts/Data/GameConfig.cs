// File: Data/GameConfig.cs

using UnityEngine;

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
    public bool CreateTestSquads = true;
        
    [Header("Gameplay")]
    public float DefaultMoraleValue = 1.0f;
    public float MoraleDecayRate = 0.01f;
    public float CombatMoraleImpact = 0.2f;
    public bool AutoStartGame = false;
    public VictoryCondition VictoryCondition = VictoryCondition.DEFEAT_ALL_ENEMIES;
        
    [Header("AI Settings")]
    public float AIUpdateInterval = 0.5f;
    public float AIDecisionDelay = 0.2f;
    public bool EnableAdvancedAI = false;
        
    [Header("Squad Settings")]
    public int MaxSquadsPerPlayer = 10;
    public int MaxSquadsPerAI = 10;
    public float SquadSpawnCooldown = 5.0f;
        
    [Header("Resource Settings")]
    public int StartingGold = 1000;
    public int GoldPerSecond = 5;
    public int SquadCost = 100;
}
    
public enum VictoryCondition
{
    DEFEAT_ALL_ENEMIES,
    CAPTURE_OBJECTIVES,
    SURVIVE_TIME,
    SCORE_BASED
}