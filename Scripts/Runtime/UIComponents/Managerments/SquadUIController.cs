using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Troop;

/// <summary>
/// Controls the UI for squad selection and commands
/// </summary>
public class SquadUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TroopCommandSystem commandSystem;
    [SerializeField] private Camera mainCamera;
    
    [Header("Squad Selection")]
    [SerializeField] private LayerMask squadLayerMask;
    [SerializeField] private GameObject selectionIndicatorPrefab;
    [SerializeField] private float selectionIndicatorHeight = 0.5f;
    
    [Header("Move Command")]
    [SerializeField] private GameObject moveMarkerPrefab;
    [SerializeField] private float moveMarkerHeight = 0.2f;
    [SerializeField] private LayerMask groundLayerMask;
    
    [Header("UI Elements")]
    [SerializeField] private Button moveButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button defendButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private GameObject commandPanel;
    
    // State tracking
    private SquadSystem selectedSquad;
    private GameObject selectionIndicator;
    private GameObject moveMarker;
    private bool isPlacingMoveMarker = false;
    private Dictionary<SquadSystem, GameObject> squadIndicators = new Dictionary<SquadSystem, GameObject>();
    
    private void Awake()
    {
        // Find references if not assigned
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        if (commandSystem == null)
            commandSystem = FindObjectOfType<TroopCommandSystem>();
            
        // Setup UI buttons
        if (moveButton != null)
            moveButton.onClick.AddListener(OnMoveButtonClicked);
            
        if (attackButton != null)
            attackButton.onClick.AddListener(OnAttackButtonClicked);
            
        if (defendButton != null)
            defendButton.onClick.AddListener(OnDefendButtonClicked);
            
        if (stopButton != null)
            stopButton.onClick.AddListener(OnStopButtonClicked);
            
        // Initialize UI state
        UpdateUIState();
    }
    
    private void Update()
    {
        // Handle squad selection on left click
        if (Input.GetMouseButtonDown(0) && !isPlacingMoveMarker)
        {
            HandleSquadSelection();
        }
        
        // Handle move marker placement
        if (isPlacingMoveMarker)
        {
            UpdateMoveMarkerPosition();
            
            // Complete placement on left click
            if (Input.GetMouseButtonDown(0))
            {
                CompleteMoveCommand();
            }
            
            // Cancel placement on right click
            if (Input.GetMouseButtonDown(1))
            {
                CancelMoveCommand();
            }
        }
    }
    
    private void HandleSquadSelection()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 100f, squadLayerMask))
        {
            // Try to get squad from the hit object
            SquadSystem squad = hit.collider.GetComponentInParent<SquadSystem>();
            if (squad != null)
            {
                SelectSquad(squad);
            }
        }
        else
        {
            // Deselect if clicking elsewhere
            DeselectSquad();
        }
    }
    
    private void SelectSquad(SquadSystem squad)
    {
        // Deselect previous squad
        if (selectedSquad != null && selectedSquad != squad)
        {
            DeselectSquad();
        }
        
        selectedSquad = squad;
        
        // Create selection indicator if it doesn't exist
        if (!squadIndicators.TryGetValue(squad, out selectionIndicator))
        {
            selectionIndicator = Instantiate(selectionIndicatorPrefab, 
                squad.transform.position + Vector3.up * selectionIndicatorHeight, 
                Quaternion.identity);
                
            squadIndicators[squad] = selectionIndicator;
        }
        else
        {
            selectionIndicator.SetActive(true);
        }
        
        // Update UI
        UpdateUIState();
    }
    
    private void DeselectSquad()
    {
        if (selectedSquad != null)
        {
            if (squadIndicators.TryGetValue(selectedSquad, out var indicator))
            {
                indicator.SetActive(false);
            }
            
            selectedSquad = null;
            UpdateUIState();
        }
    }
    
    private void UpdateMoveMarkerPosition()
    {
        if (moveMarker == null) return;
        
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 100f, groundLayerMask))
        {
            moveMarker.transform.position = hit.point + Vector3.up * moveMarkerHeight;
        }
    }
    
    private void CompleteMoveCommand()
    {
        if (selectedSquad != null && moveMarker != null)
        {
            Vector3 targetPosition = moveMarker.transform.position;
            
            // Send move command to the selected squad
            commandSystem.CommandSquadMove(selectedSquad, targetPosition);
            
            // Leave the move marker in place to show destination
            isPlacingMoveMarker = false;
            
            // Update UI
            UpdateUIState();
        }
    }
    
    private void CancelMoveCommand()
    {
        if (moveMarker != null)
        {
            Destroy(moveMarker);
            moveMarker = null;
        }
        
        isPlacingMoveMarker = false;
        UpdateUIState();
    }
    
    private void UpdateUIState()
    {
        // Enable/disable command panel based on selection
        if (commandPanel != null)
        {
            commandPanel.SetActive(selectedSquad != null);
        }
        
        // Enable/disable buttons based on state
        if (moveButton != null)
            moveButton.interactable = selectedSquad != null && !isPlacingMoveMarker;
            
        if (attackButton != null)
            attackButton.interactable = selectedSquad != null && !isPlacingMoveMarker;
            
        if (defendButton != null)
            defendButton.interactable = selectedSquad != null && !isPlacingMoveMarker;
            
        if (stopButton != null)
            stopButton.interactable = selectedSquad != null && !isPlacingMoveMarker;
    }
    
    // Button click handlers
    
    private void OnMoveButtonClicked()
    {
        if (selectedSquad == null) return;
        
        isPlacingMoveMarker = true;
        
        // Create move marker if it doesn't exist
        if (moveMarker == null)
        {
            moveMarker = Instantiate(moveMarkerPrefab, 
                selectedSquad.transform.position, 
                Quaternion.identity);
        }
        
        // Update UI state
        UpdateUIState();
    }
    
    private void OnAttackButtonClicked()
    {
        if (selectedSquad == null) return;
        
        // For attack command, we would typically need to select an enemy
        // For simplicity, we'll auto-target the nearest enemy for now
        if (selectedSquad.GetTroops().Count > 0)
        {
            var firstTroop = selectedSquad.GetTroops()[0];
            if (firstTroop != null && firstTroop.SteeringContext.NearbyEnemies?.Length > 0)
            {
                var nearestEnemy = firstTroop.SteeringContext.NearbyEnemies[0];
                if (nearestEnemy != null)
                {
                    commandSystem.CommandSquadAttack(selectedSquad, nearestEnemy);
                }
            }
        }
    }
    
    private void OnDefendButtonClicked()
    {
        if (selectedSquad == null) return;
        
        commandSystem.CommandSquadDefend(selectedSquad);
    }
    
    private void OnStopButtonClicked()
    {
        if (selectedSquad == null) return;
        
        commandSystem.CommandSquadStop(selectedSquad);
        
        // Clean up any move markers
        if (moveMarker != null)
        {
            Destroy(moveMarker);
            moveMarker = null;
        }
    }
    
    private void OnDestroy()
    {
        // Clean up listeners
        if (moveButton != null)
            moveButton.onClick.RemoveListener(OnMoveButtonClicked);
            
        if (attackButton != null)
            attackButton.onClick.RemoveListener(OnAttackButtonClicked);
            
        if (defendButton != null)
            defendButton.onClick.RemoveListener(OnDefendButtonClicked);
            
        if (stopButton != null)
            stopButton.onClick.RemoveListener(OnStopButtonClicked);
    }
}