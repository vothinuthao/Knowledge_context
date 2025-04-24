using UnityEngine;

namespace Debug_Tool
{
    /// <summary>
    /// Helper component to enable debug visualization on all troops
    /// </summary>
    public class DebugEnablerComponent : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugging = true;
        [SerializeField] private KeyCode toggleDebugKey = KeyCode.F1;
        [SerializeField] private KeyCode refreshSquadsKey = KeyCode.F2;
    
        [Header("References")]
        [SerializeField] private WorldManager worldManager;
        [SerializeField] private GridManager gridManager;
    
        private void Start()
        {
            // Find managers if not assigned
            if (worldManager == null) worldManager = FindObjectOfType<WorldManager>();
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        
            if (enableDebugging)
            {
                EnableDebugVisualizersOnAllTroops();
            }
        }
    
        private void Update()
        {
            // Toggle debug visualization with F1 key
            if (Input.GetKeyDown(toggleDebugKey))
            {
                enableDebugging = !enableDebugging;
            
                if (enableDebugging)
                {
                    EnableDebugVisualizersOnAllTroops();
                    Debug.Log("Debug visualization enabled");
                }
                else
                {
                    DisableDebugVisualizersOnAllTroops();
                    Debug.Log("Debug visualization disabled");
                }
            }
        
            // Refresh squad positions with F2 key
            if (Input.GetKeyDown(refreshSquadsKey) && gridManager != null)
            {
                gridManager.ClearAllOccupiedCells();
                RefreshSquadsOccupancy();
                Debug.Log("Refreshed squad positions on grid");
            }
        }
    
        /// <summary>
        /// Enables debug visualizers on all troops
        /// </summary>
        private void EnableDebugVisualizersOnAllTroops()
        {
            var troopVisualizers = FindObjectsOfType<TroopDebugVisualizer>();
        
            foreach (var visualizer in troopVisualizers)
            {
                visualizer.EnableDebugVisualization();
                visualizer.enabled = true;
            }
        
            Debug.Log($"Enabled debug visualization on {troopVisualizers.Length} troops");
        }
    
        /// <summary>
        /// Disables debug visualizers on all troops
        /// </summary>
        private void DisableDebugVisualizersOnAllTroops()
        {
            var troopVisualizers = FindObjectsOfType<TroopDebugVisualizer>();
        
            foreach (var visualizer in troopVisualizers)
            {
                visualizer.enabled = false;
            }
        
            Debug.Log($"Disabled debug visualization on {troopVisualizers.Length} troops");
        }
    
        /// <summary>
        /// Refreshes squad positions on the grid based on their actual positions
        /// </summary>
        private void RefreshSquadsOccupancy()
        {
            if (worldManager == null || gridManager == null)
                return;
            
            // Find all squad entities with position components
            var squadEntities = worldManager.GetWorld().GetEntitiesWith<Squad.SquadStateComponent, Movement.PositionComponent>();
        
            foreach (var squadEntity in squadEntities)
            {
                var position = squadEntity.GetComponent<Movement.PositionComponent>().Position;
                var gridCoords = gridManager.GetGridCoordinates(position);
            
                if (gridManager.IsWithinGrid(gridCoords))
                {
                    gridManager.SetCellOccupied(gridCoords, true);
                }
            }
        }
    }
}