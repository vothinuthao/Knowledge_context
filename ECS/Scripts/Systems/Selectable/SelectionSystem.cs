using System;
using Components;
using Components.Squad;
using Core.ECS;
using Systems.Squad;
using UnityEngine;

namespace Systems
{
    /// <summary>
    /// System for handling entity selection by player
    /// </summary>
    public class SelectionSystem : ISystem
    {
        private World _world;
        private Entity _selectedEntity;
        private GridSystem _gridSystem;
        
        // Event for when an entity is selected
        public event Action<Entity> OnEntitySelected;
        public event Action OnEntityDeselected;
        
        // Layer masks for raycast selection
        [SerializeField] private LayerMask selectableLayer = -1; // Default to everything
        
        public int Priority => 200; // Very high priority to run before other systems
        
        public void Initialize(World world)
        {
            _world = world;
            
            // Try to get the grid system
            _gridSystem = _world.GetSystem<GridSystem>();
        }
        
        public void Update(float deltaTime)
        {
            // Handle selection input
            HandleSelectionInput();
        }
        
        /// <summary>
        /// Handle input for selecting entities
        /// </summary>
        private void HandleSelectionInput()
        {
            // Left mouse button for selection
            if (Input.GetMouseButtonDown(0))
            {
                // Cast ray from mouse position
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectableLayer))
                {
                    // Try to get EntityBehaviour from hit object
                    EntityBehaviour entityBehaviour = hit.collider.GetComponent<EntityBehaviour>();
                    if (entityBehaviour != null)
                    {
                        Entity entity = entityBehaviour.GetEntity();
                        if (entity != null)
                        {
                            SelectEntity(entity);
                        }
                    }
                }
                else
                {
                    // Click on empty space, deselect current entity
                    DeselectEntity();
                }
            }
            
            // Right mouse button for actions on selected entity
            if (Input.GetMouseButtonDown(1) && _selectedEntity != null)
            {
                HandleActionInput();
            }
            
            // Escape key to deselect
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                DeselectEntity();
            }
        }
        
        /// <summary>
        /// Handle right-click actions for selected entity
        /// </summary>
        private void HandleActionInput()
        {
            // Only proceed if we have a selected entity and grid system
            if (_selectedEntity == null || _gridSystem == null)
                return;
                
            // Cast ray to determine action target
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                // Check if hitting a tile
                Vector3 hitPosition = hit.point;
                Entity tileEntity = _gridSystem.GetTileAtPosition(hitPosition);
                
                if (tileEntity != null && tileEntity.HasComponent<GridTileComponent>())
                {
                    var tileComponent = tileEntity.GetComponent<GridTileComponent>();
                    
                    // If the tile is highlighted (valid move) and not occupied
                    if (tileComponent.IsHighlighted && !tileComponent.IsOccupied)
                    {
                        // Get the world position for movement
                        Vector3 targetPosition = _gridSystem.GridToWorld(new Vector2Int(tileComponent.X, tileComponent.Z));
                        
                        // If the entity is a squad, move it
                        if (_selectedEntity.HasComponent<SquadComponent>())
                        {
                            // Get the squad command system
                            SquadCommandSystem squadCommandSystem = _world.GetSystem<SquadCommandSystem>();
                            if (squadCommandSystem != null)
                            {
                                // Move the squad to the target position
                                squadCommandSystem.CommandMove(_selectedEntity, targetPosition);
                                
                                // Update tile occupancy
                                if (_selectedEntity.HasComponent<PositionComponent>())
                                {
                                    var positionComponent = _selectedEntity.GetComponent<PositionComponent>();
                                    Vector2Int oldGridPos = _gridSystem.WorldToGrid(positionComponent.Position);
                                    Vector2Int newGridPos = new Vector2Int(tileComponent.X, tileComponent.Z);
                                    
                                    // Update old tile
                                    Entity oldTileEntity = _gridSystem.GetTileAt(oldGridPos.x, oldGridPos.y);
                                    if (oldTileEntity != null && oldTileEntity.HasComponent<GridTileComponent>())
                                    {
                                        oldTileEntity.GetComponent<GridTileComponent>().ClearOccupied();
                                    }
                                    
                                    // Update new tile
                                    tileComponent.SetOccupied(_selectedEntity.Id);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Select an entity
        /// </summary>
        public void SelectEntity(Entity entity)
        {
            // Only proceed if the entity has SelectableComponent
            if (entity == null || !entity.HasComponent<SelectableComponent>())
                return;
                
            // Deselect current entity if different
            if (_selectedEntity != null && _selectedEntity != entity)
            {
                DeselectEntity();
            }
            
            // Set selected entity
            _selectedEntity = entity;
            
            // Update SelectableComponent
            var selectableComponent = entity.GetComponent<SelectableComponent>();
            selectableComponent.IsSelected = true;
            
            // Update visuals through EntityBehaviour
            UpdateEntitySelectionVisual(entity, true);
            
            // If this is a squad, show movement options
            if (entity.HasComponent<SquadComponent>() && _gridSystem != null)
            {
                _gridSystem.HighlightMovementOptions(entity);
            }
            
            // Trigger event
            OnEntitySelected?.Invoke(entity);
            
            Debug.Log($"Selected entity {entity.Id}");
        }
        
        /// <summary>
        /// Deselect the currently selected entity
        /// </summary>
        public void DeselectEntity()
        {
            if (_selectedEntity == null)
                return;
                
            // Update SelectableComponent
            if (_selectedEntity.HasComponent<SelectableComponent>())
            {
                var selectableComponent = _selectedEntity.GetComponent<SelectableComponent>();
                selectableComponent.IsSelected = false;
                
                // Update visuals
                UpdateEntitySelectionVisual(_selectedEntity, false);
            }
            
            // Clear movement highlights
            if (_gridSystem != null)
            {
                _gridSystem.ClearAllHighlights();
            }
            
            // Trigger event
            OnEntityDeselected?.Invoke();
            
            Debug.Log($"Deselected entity {_selectedEntity.Id}");
            
            _selectedEntity = null;
        }
        
        /// <summary>
        /// Update the visual representation of entity selection
        /// </summary>
        private void UpdateEntitySelectionVisual(Entity entity, bool isSelected)
        {
            // Find all EntityBehaviour instances
            EntityBehaviour[] entityBehaviours = GameObject.FindObjectsOfType<EntityBehaviour>();
            
            foreach (var behaviour in entityBehaviours)
            {
                if (behaviour.GetEntity() == entity)
                {
                    // Get renderers
                    Renderer[] renderers = behaviour.GetComponentsInChildren<Renderer>();
                    
                    // Get materials from SelectableComponent
                    if (entity.HasComponent<SelectableComponent>())
                    {
                        var selectableComponent = entity.GetComponent<SelectableComponent>();
                        
                        // Update materials based on selection state
                        if (isSelected && selectableComponent.SelectedMaterial != null)
                        {
                            foreach (var renderer in renderers)
                            {
                                renderer.material = selectableComponent.SelectedMaterial;
                            }
                        }
                        else if (!isSelected && selectableComponent.OriginalMaterial != null)
                        {
                            foreach (var renderer in renderers)
                            {
                                renderer.material = selectableComponent.OriginalMaterial;
                            }
                        }
                    }
                    
                    break;
                }
            }
        }
        
        /// <summary>
        /// Get the currently selected entity
        /// </summary>
        public Entity GetSelectedEntity()
        {
            return _selectedEntity;
        }
    }
}