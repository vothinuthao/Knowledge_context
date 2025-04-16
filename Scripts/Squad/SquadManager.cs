using System.Collections.Generic;
using Core;
using Core.Patterns;
using UnityEngine;

/// <summary>
/// Manager for all squads in the game
/// </summary>
public class SquadManager : MonoBehaviourSingleton<SquadManager>
{
    [SerializeField] private int maxSquadsAllowed = 10;
        
    private List<SquadController> _activeSquads = new List<SquadController>();
    private SquadController _selectedSquad;
        
    protected override void OnSingletonCreated()
    {
        base.OnSingletonCreated();
            
        EventManager.Instance.AddListener(EventTypeInGame.SquadCommandIssued, OnSquadCommandIssued);
    }
        
    /// <summary>
    /// Create a new squad
    /// </summary>
    public SquadController CreateSquad(GameDefineData.SquadType squadType, int troopCount, Vector3 position)
    {
        if (_activeSquads.Count >= maxSquadsAllowed)
        {
            Debug.LogWarning("Maximum number of squads reached!");
            return null;
        }
            
        // Use factory to create squad
        SquadController newSquad = SquadFactory.Instance.CreateSquad(squadType, troopCount, position);
            
        // Register squad
        RegisterSquad(newSquad);
            
        return newSquad;
    }
        
    /// <summary>
    /// Register an existing squad
    /// </summary>
    public void RegisterSquad(SquadController squad)
    {
        if (!_activeSquads.Contains(squad))
        {
            _activeSquads.Add(squad);
        }
    }
        
    /// <summary>
    /// Remove a squad
    /// </summary>
    public void RemoveSquad(SquadController squad)
    {
        if (_activeSquads.Contains(squad))
        {
            _activeSquads.Remove(squad);
                
            // Deselect if this was the selected squad
            if (_selectedSquad == squad)
            {
                SetSelectedSquad(null);
            }
                
            // Destroy the game object
            Destroy(squad.gameObject);
        }
    }
        
    /// <summary>
    /// Get all active squads
    /// </summary>
    public List<SquadController> GetAllSquads()
    {
        return new List<SquadController>(_activeSquads);
    }
        
    /// <summary>
    /// Select a squad
    /// </summary>
    public void SetSelectedSquad(SquadController squad)
    {
        // Deselect current squad if any
        if (_selectedSquad != null)
        {
            _selectedSquad.SetSelected(false);
        }
            
        // Set new selected squad
        _selectedSquad = squad;
            
        // Highlight new selected squad if any
        if (_selectedSquad != null)
        {
            _selectedSquad.SetSelected(true);
        }
    }
        
    /// <summary>
    /// Get the currently selected squad
    /// </summary>
    public SquadController GetSelectedSquad()
    {
        return _selectedSquad;
    }
        
    /// <summary>
    /// Issue move command to selected squad
    /// </summary>
    public void MoveSelectedSquadToTile(Transform tile)
    {
        if (_selectedSquad != null && tile != null)
        {
            _selectedSquad.MoveToTile(tile);
                
            // Trigger command event
            EventManager.Instance.TriggerEvent(EventTypeInGame.SquadCommandIssued, _selectedSquad);
        }
    }
        
    /// <summary>
    /// Handle squad commands
    /// </summary>
    private void OnSquadCommandIssued(object sender, System.EventArgs args)
    {
        // Handle any game-wide effects of squad commands
        // For example, play sound effects, UI updates, etc.
    }
        
    /// <summary>
    /// Find squads in an area
    /// </summary>
    public List<SquadController> GetSquadsInRadius(Vector3 center, float radius)
    {
        List<SquadController> squadsInRadius = new List<SquadController>();
            
        foreach (var squad in _activeSquads)
        {
            if (Vector3.Distance(squad.transform.position, center) <= radius)
            {
                squadsInRadius.Add(squad);
            }
        }
            
        return squadsInRadius;
    }
        
    /// <summary>
    /// Get squads by type
    /// </summary>
    public List<SquadController> GetSquadsByType(GameDefineData.SquadType squadType)
    {
        List<SquadController> squadsOfType = new List<SquadController>();
            
        foreach (var squad in _activeSquads)
        {
            if (squad.SquadModel.SquadType == squadType)
            {
                squadsOfType.Add(squad);
            }
        }
            
        return squadsOfType;
    }
}