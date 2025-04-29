using Components;
using Core.ECS;
using Movement;
using Systems;
using UnityEngine;

/// <summary>
/// MonoBehaviour that links a Unity GameObject to an ECS Entity
/// Extension with selection support
/// </summary>
public class EntityBehaviour : MonoBehaviour
{
    // Reference to the ECS entity
    private Entity _entity;
    
    // Reference to the World
    private World _world;
    
    // Selection properties
    [SerializeField] private bool isSelectable = true;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material selectedMaterial;
    
    /// <summary>
    /// Initialize this behaviour with an entity and world
    /// </summary>
    public void Initialize(Entity entity, World world)
    {
        _entity = entity;
        _world = world;
        if (isSelectable && entity != null)
        {
            if (!entity.HasComponent<SelectableComponent>())
            {
                entity.AddComponent(new SelectableComponent(defaultMaterial, selectedMaterial));
            }
            
            Collider collider = GetComponent<Collider>();
            if (!collider)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }
        }
    }
    
    /// <summary>
    /// Get the entity
    /// </summary>
    public Entity GetEntity()
    {
        return _entity;
    }
    
    public World GetWorld()
    {
        return _world;
    }
    
    private void Update()
    {
        // Only sync transform if entity has position and rotation components
        if (_entity == null)
        {
            return;
        }
        
        SyncTransformFromEntity();
    }
    
    /// <summary>
    /// Sync GameObject transform from entity components
    /// </summary>
    private void SyncTransformFromEntity()
    {
        if (_entity.HasComponent<PositionComponent>())
        {
            transform.position = _entity.GetComponent<PositionComponent>().Position;
        }
        
        if (_entity.HasComponent<RotationComponent>())
        {
            transform.rotation = _entity.GetComponent<RotationComponent>().Rotation;
        }
    }
    
    /// <summary>
    /// Sync entity components from GameObject transform
    /// </summary>
    public void SyncEntityFromTransform()
    {
        if (_entity.HasComponent<PositionComponent>())
        {
            _entity.GetComponent<PositionComponent>().Position = transform.position;
        }
        
        if (_entity.HasComponent<RotationComponent>())
        {
            _entity.GetComponent<RotationComponent>().Rotation = transform.rotation;
        }
    }
    
    /// <summary>
    /// Handle mouse click on this GameObject for selection
    /// </summary>
    private void OnMouseDown()
    {
        if (isSelectable && _entity != null)
        {
            // Get SelectionSystem from ServiceLocator
            SelectionSystem selectionSystem = _world?.GetSystem<SelectionSystem>();
            if (selectionSystem != null)
            {
                selectionSystem.SelectEntity(_entity);
            }
        }
    }
}