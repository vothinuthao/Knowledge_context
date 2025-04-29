using Core.ECS;
using UnityEngine;

namespace Components
{
    /// <summary>
    /// Component marking an entity as selectable by the player
    /// </summary>
    public class SelectableComponent : IComponent
    {
        // Whether this entity is currently selected
        public bool IsSelected { get; set; } = false;
        
        // The material to use when selected
        public Material SelectedMaterial { get; set; }
        
        // The original material to revert to when deselected
        public Material OriginalMaterial { get; set; }
        
        // Optional highlight color for visual feedback
        public Color HighlightColor { get; set; } = new Color(0.3f, 0.7f, 0.3f, 1f);
        
        public SelectableComponent(Material originalMaterial = null, Material selectedMaterial = null)
        {
            OriginalMaterial = originalMaterial;
            SelectedMaterial = selectedMaterial;
        }
    }
}