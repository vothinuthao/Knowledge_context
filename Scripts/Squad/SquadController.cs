// Scripts/Squad/SquadController.cs

using System.Collections.Generic;
using Troops.Base;
using UnityEngine;

/// <summary>
/// Controller for a squad of troops
/// </summary>
public class SquadController : MonoBehaviour
{
    [SerializeField] private SquadBase config;
    [SerializeField] private List<TroopBase> troops = new List<TroopBase>();
    [SerializeField] private List<Transform> formationPositions = new List<Transform>();
        
    private Transform targetTile;
    private bool isMoving = false;
        
    /// <summary>
    /// Initialize squad with config
    /// </summary>
    public void Initialize(SquadBase config)
    {
        this.config = config;
            
        // Set name
        gameObject.name = config.SquadName;
            
        // Add banner if available
        if (config.SquadBannerPrefab != null)
        {
            GameObject banner = Instantiate(config.SquadBannerPrefab, transform);
            banner.transform.localPosition = new Vector3(0, 2, 0);
        }
    }
        
    /// <summary>
    /// Add a troop to the squad
    /// </summary>
    public void AddTroop(TroopBase troop)
    {
        if (!troops.Contains(troop))
        {
            troops.Add(troop);
        }
    }
        
    /// <summary>
    /// Remove a troop from the squad
    /// </summary>
    public void RemoveTroop(TroopBase troop)
    {
        if (troops.Contains(troop))
        {
            troops.Remove(troop);
        }
    }
        
    /// <summary>
    /// Set formation positions for the squad
    /// </summary>
    public void SetFormationPositions(List<Transform> positions)
    {
        formationPositions = positions;
    }
        
    /// <summary>
    /// Get formation positions
    /// </summary>
    public List<Transform> GetFormationPositions()
    {
        return formationPositions;
    }
        
    /// <summary>
    /// Move squad to target tile
    /// </summary>
    public void MoveToTile(Transform tileTrans)
    {
        if (tileTrans == null)
            return;
                
        // Set target
        targetTile = tileTrans;
        isMoving = true;
            
        // Move formation positions to target
        MoveFormationPositions(tileTrans.position);
            
        // Tell troops to move
        foreach (var troop in troops)
        {
            troop.SetSquadMoveTarget(troop.FormationPositionTarget);
        }
    }
        
    /// <summary>
    /// Stop moving
    /// </summary>
    public void StopMoving()
    {
        isMoving = false;
        targetTile = null;
            
        foreach (var troop in troops)
        {
            troop.ClearSquadMoveTarget();
        }
    }
        
    /// <summary>
    /// Move formation positions to new center
    /// </summary>
    private void MoveFormationPositions(Vector3 newCenter)
    {
        // Get formation parent
        Transform formationParent = formationPositions.Count > 0 ? 
            formationPositions[0].parent : transform;
                
        // Move parent to new center position
        formationParent.position = newCenter;
    }
        
    /// <summary>
    /// Update method for checking arrival at destination
    /// </summary>
    private void Update()
    {
        if (isMoving)
        {
            // Check if troops have arrived
            bool allTroopsArrived = true;
            foreach (var troop in troops)
            {
                // Check if troop is close to its formation position
                float distToPosition = Vector3.Distance(
                    troop.transform.position, 
                    troop.FormationPositionTarget.position);
                    
                if (distToPosition > 0.5f)
                {
                    allTroopsArrived = false;
                    break;
                }
            }
                
            // If all troops have arrived, stop moving
            if (allTroopsArrived)
            {
                StopMoving();
            }
        }
    }
}