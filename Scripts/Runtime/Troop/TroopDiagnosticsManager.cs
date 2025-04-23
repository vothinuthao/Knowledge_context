using System.Collections.Generic;
using Core.Debug_OnGame;
using UnityEngine;

namespace Troop
{
    /// <summary>
    /// Tool to diagnose and fix issues with troops in the scene
    /// </summary>
    public class TroopDiagnosticsManager : MonoBehaviour
    {
        [Header("Global Settings")]
        [SerializeField] private bool enableDiagnostics = true;
        [SerializeField] private bool autoFixStuckTroops = true;
        [SerializeField] private float stuckDetectionTime = 3f;
        [SerializeField] private float minMovementThreshold = 0.1f;
    
        [Header("Visualization")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool showSquadPositions = true;
        [SerializeField] private bool showTroopDestinations = true;
    
        [Header("Debug Components")]
        [SerializeField] private bool addDebugComponentToTroops = true;
    
        // Keep track of troop movement to detect stuck troops
        private Dictionary<TroopController, TroopMovementData> _troopMovementData = 
            new Dictionary<TroopController, TroopMovementData>();
    
        private class TroopMovementData
        {
            public Vector3 lastPosition;
            public float stuckTime = 0f;
            public bool isStuck = false;
            public float timeSinceLastFix = 0f;
        }
    
        private void Start()
        {
            if (enableDiagnostics)
            {
                // Find all troops in the scene
                var troops = FindObjectsOfType<TroopController>();
            
                Debug.Log($"Found {troops.Length} troops in the scene");
            
                foreach (var troop in troops)
                {
                    InitializeTroopDiagnostics(troop);
                }
            }
        }
    
        private void Update()
        {
            if (!enableDiagnostics) return;
        
            // Check for stuck troops
            if (autoFixStuckTroops)
            {
                CheckForStuckTroops();
            }
        }
    
        private void InitializeTroopDiagnostics(TroopController troop)
        {
            if (troop == null) return;
        
            // Add debug component if needed
            if (addDebugComponentToTroops && !troop.GetComponent<EnhancedTroopDebugComponent>())
            {
                troop.gameObject.AddComponent<EnhancedTroopDebugComponent>();
            }
        
            // Initialize movement tracking
            _troopMovementData[troop] = new TroopMovementData
            {
                lastPosition = troop.GetPosition(),
                stuckTime = 0f,
                isStuck = false,
                timeSinceLastFix = 0f
            };
        
            // Validate troop configuration
            ValidateTroopSetup(troop);
        }
    
        private void ValidateTroopSetup(TroopController troop)
        {
            if (troop == null || troop.GetModel() == null) return;
        
            bool hasIssues = false;
            var model = troop.GetModel();
        
            // Check if required behaviors are enabled
            bool hasSeek = false;
            bool hasArrival = false;
        
            foreach (var behavior in model.SteeringBehavior.GetSteeringBehaviors())
            {
                if (behavior.GetName() == "Seek" && troop.IsBehaviorEnabled("Seek"))
                {
                    hasSeek = true;
                }
                else if (behavior.GetName() == "Arrival" && troop.IsBehaviorEnabled("Arrival"))
                {
                    hasArrival = true;
                }
            }
        
            if (!hasSeek)
            {
                Debug.LogWarning($"Troop {troop.name} does not have Seek behavior enabled", troop);
                troop.EnableBehavior("Seek", true);
                hasIssues = true;
            }
        
            if (!hasArrival)
            {
                Debug.LogWarning($"Troop {troop.name} does not have Arrival behavior enabled", troop);
                troop.EnableBehavior("Arrival", true);
                hasIssues = true;
            }
        
            // Check if troop has a valid target position
            if (troop.GetTargetPosition() == Vector3.zero)
            {
                Debug.LogWarning($"Troop {troop.name} has zero target position", troop);
            
                // Try to get a valid target from squad if applicable
                var squadExtensions = TroopControllerSquadExtensions.Instance;
                if (squadExtensions != null)
                {
                    var squad = squadExtensions.GetSquad(troop);
                    if (squad != null)
                    {
                        var squadPos = squadExtensions.GetSquadPosition(troop);
                        Vector3 squadTargetPos = squad.GetPositionForTroop(squad, squadPos.x, squadPos.y);
                        troop.SetTargetPosition(squadTargetPos);
                        Debug.Log($"Set troop {troop.name} target to squad position {squadTargetPos}", troop);
                    }
                }
            
                hasIssues = true;
            }
        
            // Log result
            if (hasIssues)
            {
                Debug.Log($"Fixed issues with troop {troop.name}", troop);
            }
        }
    
        private void CheckForStuckTroops()
        {
            List<TroopController> troopsToRemove = new List<TroopController>();
        
            foreach (var kvp in _troopMovementData)
            {
                var troop = kvp.Key;
                var data = kvp.Value;
            
                if (troop == null || !troop.IsAlive())
                {
                    troopsToRemove.Add(troop);
                    continue;
                }
            
                // Update timer for the last fix
                data.timeSinceLastFix += Time.deltaTime;
            
                // Check if troop has moved
                float distanceMoved = Vector3.Distance(troop.GetPosition(), data.lastPosition);
            
                // If troop is moving, reset stuck timer
                if (distanceMoved > minMovementThreshold)
                {
                    data.stuckTime = 0f;
                    data.isStuck = false;
                    data.lastPosition = troop.GetPosition();
                }
                else
                {
                    // Increment stuck timer
                    data.stuckTime += Time.deltaTime;
                
                    // Check if troop is stuck
                    if (data.stuckTime > stuckDetectionTime && !data.isStuck)
                    {
                        data.isStuck = true;
                    
                        // Only try to fix if enough time has passed since last fix
                        if (data.timeSinceLastFix > 5f)
                        {
                            TryFixStuckTroop(troop);
                            data.timeSinceLastFix = 0f;
                        }
                    }
                }
            }
        
            // Remove invalid troops
            foreach (var troop in troopsToRemove)
            {
                _troopMovementData.Remove(troop);
            }
        }
    
        private void TryFixStuckTroop(TroopController troop)
        {
            if (troop == null) return;
        
            Debug.LogWarning($"Attempting to fix stuck troop: {troop.name}", troop);
        
            // Strategy 1: Reset velocity and acceleration
            troop.GetModel().Velocity = Vector3.zero;
            troop.GetModel().Acceleration = Vector3.zero;
        
            // Strategy 2: Make sure seek and arrival are enabled
            troop.EnableBehavior("Seek", true);
            troop.EnableBehavior("Arrival", true);
        
            // Strategy 3: If in a squad, reassign the target position
            var squadExtensions = TroopControllerSquadExtensions.Instance;
            if (squadExtensions != null)
            {
                var squad = squadExtensions.GetSquad(troop);
                if (squad != null)
                {
                    var squadPos = squadExtensions.GetSquadPosition(troop);
                    Vector3 squadTargetPos = squad.GetPositionForTroop(squad, squadPos.x, squadPos.y);
                
                    // Add slight randomness to break any potential deadlocks
                    squadTargetPos += new Vector3(
                        Random.Range(-0.3f, 0.3f),
                        0,
                        Random.Range(-0.3f, 0.3f)
                    );
                
                    troop.SetTargetPosition(squadTargetPos);
                    Debug.Log($"Reset troop {troop.name} target to squad position {squadTargetPos}", troop);
                }
            }
        
            // Strategy 4: Force state change to moving
            if (troop.GetState() != TroopState.Moving)
            {
                troop.StateMachine.ChangeState<MovingState>();
                Debug.Log($"Forced troop {troop.name} to MovingState", troop);
            }
        }
    
        private void OnDrawGizmos()
        {
            if (!showGizmos || !enableDiagnostics || !Application.isPlaying) return;
        
            // Draw squad positions
            if (showSquadPositions)
            {
                var squads = FindObjectsOfType<SquadSystem>();
            
                foreach (var squad in squads)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(squad.transform.position, 0.5f);
                
                    // Draw grid for squad positions
                    for (int row = 0; row < squad.rows; row++)
                    {
                        for (int col = 0; col < squad.columns; col++)
                        {
                            Vector3 pos = squad.GetPositionForTroop(squad, row, col);
                            Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.5f);
                            Gizmos.DrawCube(pos, Vector3.one * 0.3f);
                        }
                    }
                }
            }
        
            // Highlight stuck troops
            foreach (var kvp in _troopMovementData)
            {
                var troop = kvp.Key;
                var data = kvp.Value;
            
                if (troop != null && data.isStuck)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(troop.GetPosition() + Vector3.up, 0.5f);
                
                    // Draw a line to where it's supposed to go
                    if (showTroopDestinations)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(troop.GetPosition() + Vector3.up, troop.GetTargetPosition() + Vector3.up);
                        Gizmos.DrawCube(troop.GetTargetPosition() + Vector3.up, Vector3.one * 0.3f);
                    }
                }
            }
        }
    
        [ContextMenu("Add Debug Components To All Troops")]
        private void AddDebugComponentsToAllTroops()
        {
            var troops = FindObjectsOfType<TroopController>();
            int added = 0;
        
            foreach (var troop in troops)
            {
                if (!troop.GetComponent<EnhancedTroopDebugComponent>())
                {
                    troop.gameObject.AddComponent<EnhancedTroopDebugComponent>();
                    added++;
                }
            }
        
            Debug.Log($"Added debug components to {added} troops");
        }
    
        [ContextMenu("Fix All Stuck Troops")]
        private void FixAllStuckTroops()
        {
            var troops = FindObjectsOfType<TroopController>();
            int fixed_bug = 0;
        
            foreach (var troop in troops)
            {
                if (_troopMovementData.TryGetValue(troop, out var data) && data.isStuck)
                {
                    TryFixStuckTroop(troop);
                    data.isStuck = false;
                    data.stuckTime = 0f;
                    fixed_bug++;
                }
            }
        
            Debug.Log($"Attempted to fix {fixed_bug} stuck troops");
        }
    }
}