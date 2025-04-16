using System.Collections.Generic;
using Core;
using Core.Behaviors;
using Core.Patterns;
using Squad;
using Troops.Base;
using Troops.Config;
using UnityEngine;

/// <summary>
/// Factory for creating squads - follows Factory Pattern
/// </summary>
public class SquadFactory : MonoBehaviourSingleton<SquadFactory>
{
    [SerializeField] private GameObject squadPrefab;
    [SerializeField] private GameObject formationPositionPrefab;
        
    [Header("Troop Prefabs")]
    [SerializeField] private GameObject warriorPrefab;
    [SerializeField] private GameObject archerPrefab;
    [SerializeField] private GameObject pikerPrefab;
        
    [Header("Squad Configs")]
    [SerializeField] private SquadConfigSO warriorSquadConfig;
    [SerializeField] private SquadConfigSO archerSquadConfig;
    [SerializeField] private SquadConfigSO pikerSquadConfig;
        
    /// <summary>
    /// Create a squad with specified type and size
    /// </summary>
    public SquadController CreateSquad(GameDefineData.SquadType squadType, int troopCount, Vector3 position)
    {
        SquadConfigSO config = GetSquadConfig(squadType);
        
        SquadBase squadModel = new SquadBase(config);
        GameObject squadObject = Instantiate(squadPrefab, position, Quaternion.identity);
        squadObject.name = $"{squadType}Squad";
        squadObject.tag = GameDefineData.Tags.Squad;
            
        SquadController squadController = squadObject.GetComponent<SquadController>();
        if (squadController == null)
            squadController = squadObject.AddComponent<SquadController>();
            
        if (squadObject.GetComponent<SquadView>() == null)
            squadObject.AddComponent<SquadView>();
        squadController.Initialize(squadModel);
        CreateFormationPositions(squadController, squadModel, troopCount);
        CreateTroops(squadController, squadModel, troopCount);
            
        return squadController;
    }
        
    /// <summary>
    /// Get appropriate squad config based on type
    /// </summary>
    private SquadConfigSO GetSquadConfig(GameDefineData.SquadType squadType)
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
    private void CreateFormationPositions(SquadController squadController, SquadBase squadModel, int troopCount)
    {
        // Ensure we don't exceed max troops
        troopCount = Mathf.Min(troopCount, squadModel.MaxTroops);
            
        // Get formation positions based on formation type
        Vector3[] formationOffsets = GetFormationOffsets(squadModel.FormationType, troopCount);
            
        // Create a container for formation positions
        GameObject formationContainer = new GameObject("FormationPositions");
        formationContainer.transform.parent = squadController.transform;
        formationContainer.transform.localPosition = Vector3.zero;
            
        List<Transform> formationPositions = new List<Transform>();
        for (int i = 0; i < troopCount; i++)
        {
            GameObject posObj = Instantiate(formationPositionPrefab, 
                squadController.transform.position + formationOffsets[i] * squadModel.TroopSpacing, 
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
        Vector3[] baseFormation;
            
        // Choose formation pattern based on type
        switch (formationType)
        {
            case GameDefineData.Formation.FormationType.Line:
                baseFormation = CreateLineFormation(count);
                break;
            case GameDefineData.Formation.FormationType.Column:
                baseFormation = CreateColumnFormation(count);
                break;
            case GameDefineData.Formation.FormationType.Vshape:
                baseFormation = CreateVShapeFormation(count);
                break;
            case GameDefineData.Formation.FormationType.Circle:
                baseFormation = CreateCircleFormation(count);
                break;
            case GameDefineData.Formation.FormationType.Square:
            default:
                baseFormation = GameDefineData.Formation.SquareFormationPositions;
                break;
        }
            
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
    /// Create a line formation (troops side by side)
    /// </summary>
    private Vector3[] CreateLineFormation(int count)
    {
        Vector3[] positions = new Vector3[count];
        float halfWidth = (count - 1) * 0.5f;
            
        for (int i = 0; i < count; i++)
        {
            positions[i] = new Vector3(i - halfWidth, 0, 0);
        }
            
        return positions;
    }
        
    /// <summary>
    /// Create a column formation (troops in a line front to back)
    /// </summary>
    private Vector3[] CreateColumnFormation(int count)
    {
        Vector3[] positions = new Vector3[count];
        float halfHeight = (count - 1) * 0.5f;
            
        for (int i = 0; i < count; i++)
        {
            positions[i] = new Vector3(0, 0, halfHeight - i);
        }
            
        return positions;
    }
        
    /// <summary>
    /// Create a V-shape formation
    /// </summary>
    private Vector3[] CreateVShapeFormation(int count)
    {
        Vector3[] positions = new Vector3[count];
            
        // Leader at the front
        positions[0] = new Vector3(0, 0, 1.5f);
            
        int leftIndex = 1;
        int rightIndex = 2;
            
        // Place remaining troops in the wings
        for (int i = 1; i < count; i++)
        {
            if (i % 2 == 1 && leftIndex < count)
            {
                float depth = 1.0f - (leftIndex / 2) * 0.5f;
                positions[leftIndex] = new Vector3(-leftIndex * 0.5f, 0, depth);
                leftIndex += 2;
            }
            else if (rightIndex < count)
            {
                float depth = 1.0f - ((rightIndex - 1) / 2) * 0.5f;
                positions[rightIndex] = new Vector3(rightIndex * 0.5f - 0.5f, 0, depth);
                rightIndex += 2;
            }
        }
            
        return positions;
    }
        
    /// <summary>
    /// Create a circle formation
    /// </summary>
    private Vector3[] CreateCircleFormation(int count)
    {
        Vector3[] positions = new Vector3[count];
            
        // Place troops in a circle
        float angleStep = 360f / count;
        float radius = 1.0f;
            
        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius;
            positions[i] = new Vector3(x, 0, z);
        }
            
        return positions;
    }
        
    /// <summary>
    /// Create troops for the squad
    /// </summary>
    private void CreateTroops(SquadController squadController, SquadBase squadModel, int troopCount)
    {
        // Get appropriate troop prefab
        GameObject troopPrefab = GetTroopPrefab(squadModel.SquadType);
            
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
                    
            troopObject.name = $"{squadModel.SquadType}_{i}";
            troopObject.tag = GameDefineData.Tags.Troop;
            troopObject.layer = GameDefineData.Layers.Troops;
                
            // Set up TroopBase component
            TroopBase troopBase = troopObject.GetComponent<TroopBase>();
            if (troopBase != null)
            {
                // Initialize with troop config from the squad model
                troopBase.Initialize(squadModel.TroopConfig);
                troopBase.SetFormationPositionTarget(formationPositions[i]);
                    
                // Use ContextSteeringManager if available for improved movement
                ContextSteeringManager contextSteeringManager = troopObject.GetComponent<ContextSteeringManager>();
                if (contextSteeringManager != null)
                {
                    contextSteeringManager.SetTarget(formationPositions[i]);
                }
                else
                {
                    // Fallback to standard steering manager
                    SteeringManager steeringManager = troopObject.GetComponent<SteeringManager>();
                    if (steeringManager != null)
                    {
                        steeringManager.SetTarget(formationPositions[i]);
                    }
                }
                    
                // Add to squad controller
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