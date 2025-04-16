using System.Collections.Generic;
using Core;
using Squad;
using Troops.Base;
using UnityEngine;

/// <summary>
/// Controller for Squad - handles game logic and coordinates Model and View
/// </summary>
public class SquadController : MonoBehaviour
{
    [SerializeField] private float arrivalThreshold = 0.5f;
        
    private SquadBase _squadModel;
    private SquadView _squadView;
    private List<Transform> _formationPositions = new List<Transform>();
        
    private Transform _targetTile;
    private bool _isMoving = false;
    private bool _isSelected = false;
        
    /// <summary>
    /// Initialize with a squad model
    /// </summary>
    public void Initialize(SquadBase squadModel)
    {
        _squadModel = squadModel;
            
        // Set game object name
        gameObject.name = squadModel.SquadName;
            
        // Get or create the view component
        _squadView = GetComponent<SquadView>();
        if (_squadView == null)
        {
            _squadView = gameObject.AddComponent<SquadView>();
        }
            
        // Initialize the view
        _squadView.Initialize(squadModel);
    }
        
    /// <summary>
    /// Set the formation positions for troops
    /// </summary>
    public void SetFormationPositions(List<Transform> positions)
    {
        _formationPositions = positions;
    }
        
    /// <summary>
    /// Get the current formation positions
    /// </summary>
    public List<Transform> GetFormationPositions()
    {
        return _formationPositions;
    }
        
    /// <summary>
    /// Add a troop to the squad
    /// </summary>
    public void AddTroop(TroopBase troop)
    {
        if (_squadModel.TroopCount >= _squadModel.MaxTroops)
        {
            Debug.LogWarning($"Cannot add troop to squad {_squadModel.SquadName}: squad is at max capacity");
            return;
        }
            
        // Add to model
        _squadModel.AddTroop(troop);
            
        // Notify view
        _squadView.OnTroopAdded(troop);
    }
        
    /// <summary>
    /// Remove a troop from the squad
    /// </summary>
    public void RemoveTroop(TroopBase troop)
    {
        // Remove from model
        _squadModel.RemoveTroop(troop);
            
        // Notify view
        _squadView.OnTroopRemoved(troop);
    }
        
    /// <summary>
    /// Command the squad to move to a target tile
    /// </summary>
    public void MoveToTile(Transform tileTrans)
    {
        if (tileTrans == null)
            return;
            
        // Set target and state
        _targetTile = tileTrans;
        _isMoving = true;
            
        // Move formation positions to target
        MoveFormationPositions(tileTrans.position);
            
        // Update troop targets
        List<TroopBase> troops = _squadModel.GetTroops();
        foreach (var troop in troops)
        {
            troop.SetSquadMoveTarget(troop.FormationPositionTarget);
        }
            
        // Notify view of movement
        _squadView.OnSquadMoving();
            
        // Trigger event for other systems to react
        EventManager.Instance.TriggerEvent(EventTypeInGame.SquadMoved, this);
    }
        
    /// <summary>
    /// Stop the squad's movement
    /// </summary>
    public void StopMoving()
    {
        _isMoving = false;
        _targetTile = null;
            
        // Update troop targets
        List<TroopBase> troops = _squadModel.GetTroops();
        foreach (var troop in troops)
        {
            troop.ClearSquadMoveTarget();
        }
            
        // Notify view
        _squadView.OnSquadStopped();
    }
        
    /// <summary>
    /// Set the selected state of the squad
    /// </summary>
    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        _squadView.SetHighlighted(selected);
    }
        
    /// <summary>
    /// Move formation positions to a new center position
    /// </summary>
    private void MoveFormationPositions(Vector3 newCenter)
    {
        // Get formation parent
        if (_formationPositions.Count == 0)
            return;
            
        Transform formationParent = _formationPositions[0].parent;
            
        // Move parent to new center position
        formationParent.position = newCenter;
    }
        
    /// <summary>
    /// Get an available formation position for a new troop
    /// </summary>
    public Transform GetAvailableFormationPosition()
    {
        // Collect all formation positions that are not already assigned
        List<Transform> availablePositions = new List<Transform>();
            
        foreach (var position in _formationPositions)
        {
            bool isAssigned = false;
                
            // Check if any troop already uses this position
            foreach (var troop in _squadModel.GetTroops())
            {
                if (troop.FormationPositionTarget == position)
                {
                    isAssigned = true;
                    break;
                }
            }
                
            if (!isAssigned)
            {
                availablePositions.Add(position);
            }
        }
            
        // Return the first available, or null if none
        return availablePositions.Count > 0 ? availablePositions[0] : null;
    }
        
    /// <summary>
    /// Update method - check arrival status and handle other state updates
    /// </summary>
    private void Update()
    {
        if (_isMoving)
        {
            // Check if troops have arrived
            bool allTroopsArrived = true;
            List<TroopBase> troops = _squadModel.GetTroops();
                
            foreach (var troop in troops)
            {
                // Skip dead troops
                if (troop.CurrentState == TroopState.Dead)
                    continue;
                    
                // Check if troop is close to its formation position
                float distToPosition = Vector3.Distance(
                    troop.transform.position, 
                    troop.FormationPositionTarget.position);
                    
                if (distToPosition > arrivalThreshold)
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
        
    /// <summary>
    /// Access to the squad model
    /// </summary>
    public SquadBase SquadModel => _squadModel;
        
    /// <summary>
    /// Check if the squad is currently moving
    /// </summary>
    public bool IsMoving => _isMoving;
        
    /// <summary>
    /// Check if the squad is currently selected
    /// </summary>
    public bool IsSelected => _isSelected;
}