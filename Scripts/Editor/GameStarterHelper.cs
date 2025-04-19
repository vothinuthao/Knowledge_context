#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using SteeringBehavior;
using Troop;
using UnityEngine;

public class GameStarterHelper
{
    [MenuItem("Wiking Raven/Create Sample Data")]
    public static void CreateSampleData()
    {
        // Tạo sample troop configs
        CreateSampleTroopConfigs();
        
        // Tạo sample behavior configs
        CreateSampleBehaviorConfigs();
        
        Debug.Log("GameStarterHelper: Đã tạo sample data");
    }
    
    private static void CreateSampleTroopConfigs()
    {
        // Tạo player troop config
        TroopConfigSO playerConfig = ScriptableObject.CreateInstance<TroopConfigSO>();
        playerConfig.troopName = "Viking Warrior";
        playerConfig.health = 100f;
        playerConfig.attackPower = 10f;
        playerConfig.moveSpeed = 3f;
        playerConfig.attackRange = 1.5f;
        playerConfig.attackSpeed = 1f;
        
        // Lưu asset
        AssetDatabase.CreateAsset(playerConfig, "Assets/Resources/Configs/VikingWarriorConfig.asset");
        
        // Tạo enemy troop config
        TroopConfigSO enemyConfig = ScriptableObject.CreateInstance<TroopConfigSO>();
        enemyConfig.troopName = "Enemy Raider";
        enemyConfig.health = 80f;
        enemyConfig.attackPower = 8f;
        enemyConfig.moveSpeed = 3.5f;
        enemyConfig.attackRange = 1.2f;
        enemyConfig.attackSpeed = 1.2f;
        
        // Lưu asset
        AssetDatabase.CreateAsset(enemyConfig, "Assets/Resources/Configs/EnemyRaiderConfig.asset");
        
        AssetDatabase.SaveAssets();
    }
    
    private static void CreateSampleBehaviorConfigs()
    {
        // Tạo Seek behavior
        SeekBehaviorSO seekBehavior = ScriptableObject.CreateInstance<SeekBehaviorSO>();
        seekBehavior.weight = 1.0f;
        seekBehavior.description = "Basic movement to target";
        
        // Lưu asset
        AssetDatabase.CreateAsset(seekBehavior, "Assets/Resources/Behaviors/SeekBehavior.asset");
        
        // Tạo Arrival behavior
        ArrivalBehaviorSO arrivalBehavior = ScriptableObject.CreateInstance<ArrivalBehaviorSO>();
        arrivalBehavior.weight = 2.0f;
        arrivalBehavior.slowingDistance = 3.0f;
        arrivalBehavior.description = "Slow down when approaching target";
        
        // Lưu asset
        AssetDatabase.CreateAsset(arrivalBehavior, "Assets/Resources/Behaviors/ArrivalBehavior.asset");
        
        // Tạo Separation behavior
        SeparationBehaviorSO separationBehavior = ScriptableObject.CreateInstance<SeparationBehaviorSO>();
        separationBehavior.weight = 1.5f;
        separationBehavior.separationRadius = 2.0f;
        separationBehavior.description = "Keep distance from allies";
        
        // Lưu asset
        AssetDatabase.CreateAsset(separationBehavior, "Assets/Resources/Behaviors/SeparationBehavior.asset");
        
        // Tạo Cohesion behavior
        CohesionBehaviorSO cohesionBehavior = ScriptableObject.CreateInstance<CohesionBehaviorSO>();
        cohesionBehavior.weight = 1.0f;
        cohesionBehavior.cohesionRadius = 8.0f;
        cohesionBehavior.description = "Stay close to allies";
        
        // Lưu asset
        AssetDatabase.CreateAsset(cohesionBehavior, "Assets/Resources/Behaviors/CohesionBehavior.asset");
        
        // Kết nối behaviors với troop configs
        TroopConfigSO playerConfig = AssetDatabase.LoadAssetAtPath<TroopConfigSO>("Assets/Resources/Configs/VikingWarriorConfig.asset");
        if (playerConfig != null)
        {
            playerConfig.behaviors = new List<SteeringBehaviorSO>
            {
                seekBehavior,
                arrivalBehavior,
                separationBehavior,
                cohesionBehavior
            };
            
            EditorUtility.SetDirty(playerConfig);
        }
        
        TroopConfigSO enemyConfig = AssetDatabase.LoadAssetAtPath<TroopConfigSO>("Assets/Resources/Configs/EnemyRaiderConfig.asset");
        if (enemyConfig != null)
        {
            enemyConfig.behaviors = new List<SteeringBehaviorSO>
            {
                seekBehavior,
                arrivalBehavior,
                separationBehavior
            };
            
            EditorUtility.SetDirty(enemyConfig);
        }
        
        AssetDatabase.SaveAssets();
    }
}
#endif