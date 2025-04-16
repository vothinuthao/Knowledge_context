// Scripts/Squad/Factory/SquadFactory.cs

using System.Collections.Generic;
using Core;
using Core.Behaviors;
using Core.Patterns;
using Troops.Base;
using Troops.Config;
using UnityEngine;

/// <summary>
/// Factory for creating squads - uses Factory Pattern
/// </summary>
public class SquadFactory : MonoBehaviourSingleton<SquadFactory>
{
    [SerializeField] private GameObject squadPrefab;
    [SerializeField] private GameObject formationPositionPrefab;
        
    [Header("Troop Prefabs")]
    [SerializeField] private GameObject warriorPrefab;
    [SerializeField] private GameObject archerPrefab;
    [SerializeField] private GameObject pikerPrefab;
        
    [Header("Config")]
    [SerializeField] private SquadBase warriorSquadConfig;
    [SerializeField] private SquadBase archerSquadConfig;
    [SerializeField] private SquadBase pikerSquadConfig;
        
    /// <summary>
    /// Create a squad with specified type and size
    /// </summary>
    public SquadController CreateSquad(GameDefineData.SquadType squadType, int troopCount, Vector3 position)
    {
        // Get the appropriate config
        SquadBase config = GetSquadConfig(squadType);
            
        // Create the squad gameobject
        GameObject squadObject = Instantiate(squadPrefab, position, Quaternion.identity);
        squadObject.name = $"{squadType}Squad";
        squadObject.tag = GameDefineData.Tags.Squad;
            
        // Set up the squad controller
        SquadController squadController = squadObject.GetComponent<SquadController>();
        if (squadController == null)
            squadController = squadObject.AddComponent<SquadController>();
            
        // Initialize the squad
        squadController.Initialize(config);
            
        // Create formation positions
        CreateFormationPositions(squadController, config, troopCount);
            
        // Create and add troops
        CreateTroops(squadController, config, troopCount);
            
        return squadController;
    }
        
    /// <summary>
    /// Get appropriate squad config based on type
    /// </summary>
    private SquadBase GetSquadConfig(GameDefineData.SquadType squadType)
    {
        switch (squadType)
        {
            case GameDefineData.SquadType.Warrior:
                return warriorSquadConfig;
            case GameDefineData.SquadType.Archer:
                return archerSquadConfig;
            case GameDefineData.SquadType.Piker:
                return pikerSquadConfig;
            default:
                Debug.LogError($"No config found for squad type: {squadType}");
                return warriorSquadConfig; // Default fallback
        }
    }
        
    /// <summary>
    /// Create formation positions for the squad
    /// </summary>
    private void CreateFormationPositions(SquadController squadController, SquadBase config, int troopCount)
    {
        // Ensure we don't exceed max troops
        troopCount = Mathf.Min(troopCount, config.maxTroops);
            
        // Get formation positions based on formation type
        Vector3[] formationOffsets = GetFormationOffsets(config.formationType, troopCount);
            
        // Create a container for formation positions
        GameObject formationContainer = new GameObject("FormationPositions");
        formationContainer.transform.parent = squadController.transform;
        formationContainer.transform.localPosition = Vector3.zero;
            
        // Create each formation position
        List<Transform> formationPositions = new List<Transform>();
        for (int i = 0; i < troopCount; i++)
        {
            GameObject posObj = Instantiate(formationPositionPrefab, 
                squadController.transform.position + formationOffsets[i] * config.troopSpacing, 
                Quaternion.identity,
                formationContainer.transform);
                
            posObj.name = $"FormationPos_{i}";
            posObj.tag = GameDefineData.Tags.FormationPosition;
                
            formationPositions.Add(posObj.transform);
        }
            
        // Set the formation positions in the squad controller
        squadController.SetFormationPositions(formationPositions);
    }
        
    /// <summary>
    /// Get appropriate formation offsets based on formation type and count
    /// </summary>
    private Vector3[] GetFormationOffsets(GameDefineData.Formation.FormationType formationType, int count)
    {
        // Use formation presets from GameDefineData
        Vector3[] baseFormation = GameDefineData.Formation.SquareFormationPositions;
            
        // If we need fewer positions than the base formation, take only what we need
        if (count < baseFormation.Length)
        {
            Vector3[] result = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = baseFormation[i];
            }
            return result;
        }
            
        return baseFormation;
    }
        
    /// <summary>
    /// Create troops for the squad
    /// </summary>
    private void CreateTroops(SquadController squadController, SquadConfigSO config, int troopCount)
    {
        // Get appropriate troop prefab
        GameObject troopPrefab = GetTroopPrefab(config.squadType);
            
        // Get formation positions
        List<Transform> formationPositions = squadController.GetFormationPositions();
            
        // Ensure we don't exceed available positions
        troopCount = Mathf.Min(troopCount, formationPositions.Count);
            
        // Create each troop
        for (int i = 0; i < troopCount; i++)
        {
            // Instantiate troop at formation position
            GameObject troopObject = Instantiate(troopPrefab, 
                formationPositions[i].position, 
                Quaternion.identity,
                squadController.transform);
                
            troopObject.name = $"{config.squadType}_{i}";
            troopObject.tag = GameDefineData.Tags.Troop;
            troopObject.layer = GameDefineData.Layers.Troops;
                
            // Set up TroopBase component
            TroopBase troopBase = troopObject.GetComponent<TroopBase>();
            if (troopBase != null)
            {
                // Initialize with config and assign formation position
                troopBase.Initialize(config.troopConfig);
                troopBase.SetFormationPositionTarget(formationPositions[i]);
                    
                // Use ContextSteeringManager if available
                ContextSteeringManager steeringManager = troopObject.GetComponent<ContextSteeringManager>();
                if (steeringManager != null)
                {
                    steeringManager.SetTarget(formationPositions[i]);
                }
                    
                // Add to squad
                squadController.AddTroop(troopBase);
            }
        }
    }
        
    /// <summary>
    /// Get appropriate troop prefab based on squad type
    /// </summary>
    private GameObject GetTroopPrefab(GameDefineData.SquadType squadType)
    {
        switch (squadType)
        {
            case GameDefineData.SquadType.Warrior:
                return warriorPrefab;
            case GameDefineData.SquadType.Archer:
                return archerPrefab;
            case GameDefineData.SquadType.Piker:
                return pikerPrefab;
            default:
                Debug.LogError($"No prefab found for squad type: {squadType}");
                return warriorPrefab; // Default fallback
        }
    }
}