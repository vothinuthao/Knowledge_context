using System.Collections.Generic;
using Core.ECS;
using UnityEngine;

namespace Management
{
    /// <summary>
    /// Component gắn vào GameObject đại diện cho một Squad
    /// </summary>
    public class SquadVisualController : MonoBehaviour
    {
        private Entity squadEntity;
        private List<Transform> troopVisuals = new List<Transform>();
        private SquadSelectionManager selectionManager;
    
        [SerializeField] private float squadCenterHeight = 0.5f;
        [SerializeField] private GameObject squadFlagPrefab;
    
        private GameObject squadFlag;
    
        /// <summary>
        /// Initialize with an entity reference
        /// </summary>
        public void Initialize(Entity entity)
        {
            squadEntity = entity;
            selectionManager = FindObjectOfType<SquadSelectionManager>();
        
            if (selectionManager != null)
            {
                selectionManager.RegisterSquadGameObject(entity.Id, this.gameObject);
            }
        
            // Create squad flag if prefab is assigned
            if (squadFlagPrefab != null)
            {
                squadFlag = Instantiate(squadFlagPrefab, transform.position, Quaternion.identity);
                squadFlag.transform.parent = transform;
                UpdateFlagPosition();
            }
        }
    
        /// <summary>
        /// Register a troop visual to this squad
        /// </summary>
        public void RegisterTroopVisual(Transform troopTransform)
        {
            if (!troopVisuals.Contains(troopTransform))
            {
                troopVisuals.Add(troopTransform);
            }
        }
    
        /// <summary>
        /// Update the flag position to the squad's center
        /// </summary>
        private void UpdateFlagPosition()
        {
            if (squadFlag == null) return;
        
            // Calculate center of the troop visuals
            Vector3 center = CalculateSquadCenter();
        
            // Update flag position
            squadFlag.transform.position = center + Vector3.up * squadCenterHeight;
        }
    
        /// <summary>
        /// Calculate the center of the squad based on troop positions
        /// </summary>
        private Vector3 CalculateSquadCenter()
        {
            if (troopVisuals.Count == 0)
            {
                return transform.position;
            }
        
            Vector3 center = Vector3.zero;
            int count = 0;
        
            foreach (var troopTransform in troopVisuals)
            {
                if (troopTransform != null)
                {
                    center += troopTransform.position;
                    count++;
                }
            }
        
            if (count > 0)
            {
                center /= count;
            }
        
            return center;
        }
    
        private void Update()
        {
            UpdateFlagPosition();
        }
    }
}