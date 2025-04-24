using System.Collections.Generic;
using Core.ECS;
using Debug_Tool;
using UnityEngine;

namespace Management
{
    /// <summary>
    /// Component attached to a GameObject representing a Squad
    /// </summary>
    public class SquadVisualController : MonoBehaviour
    {
        private Entity squadEntity;
        private List<Transform> troopVisuals = new List<Transform>();
        private SquadSelectionManager selectionManager;
    
        [SerializeField] private float squadCenterHeight = 0.5f;
        [SerializeField] private GameObject squadFlagPrefab;
    
        private GameObject squadFlag;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
    
        /// <summary>
        /// Initialize with an entity reference
        /// </summary>
        public void Initialize(Entity entity)
        {
            squadEntity = entity;
            
            // Tìm SquadSelectionManager
            selectionManager = FindObjectOfType<SquadSelectionManager>();
        
            if (selectionManager != null)
            {
                // Đăng ký GameObject này với SquadSelectionManager
                selectionManager.RegisterSquadGameObject(entity.Id, this.gameObject);
                if (debugMode) Debug.Log($"Squad {entity.Id} đã đăng ký với SquadSelectionManager");
            }
            else if (debugMode)
            {
                Debug.LogWarning("Không tìm thấy SquadSelectionManager!");
            }
        
            // Tạo squad flag nếu prefab được gán
            if (squadFlagPrefab != null)
            {
                squadFlag = Instantiate(squadFlagPrefab, transform.position, Quaternion.identity);
                squadFlag.transform.parent = transform;
                UpdateFlagPosition();
            }
            
            // Đảm bảo đối tượng này có đúng layer
            this.gameObject.layer = LayerMask.NameToLayer("Squad");
            
            // Đảm bảo đối tượng có collider
            EnsureHasCollider();
        }
        
        /// <summary>
        /// Đảm bảo Squad có collider để có thể được chọn
        /// </summary>
        private void EnsureHasCollider()
        {
            Collider collider = GetComponent<Collider>();
            if (collider == null)
            {
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(5f, 0.5f, 5f); // Kích thước bao quát các troop
                boxCollider.center = new Vector3(0, 0.25f, 0);
                boxCollider.isTrigger = true; // Đặt là trigger để không cản trở di chuyển
                
                if (debugMode) Debug.Log($"Đã thêm BoxCollider cho Squad {squadEntity.Id}");
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
                
                // Đảm bảo troop có đúng layer
                troopTransform.gameObject.layer = LayerMask.NameToLayer("Troop");
                
                // Đảm bảo troop có collider
                Collider troopCollider = troopTransform.GetComponent<Collider>();
                if (troopCollider == null)
                {
                    CapsuleCollider capsuleCollider = troopTransform.gameObject.AddComponent<CapsuleCollider>();
                    capsuleCollider.height = 2.0f;
                    capsuleCollider.radius = 0.5f;
                    capsuleCollider.center = new Vector3(0, 1.0f, 0);
                    
                    if (debugMode) Debug.Log($"Đã thêm CapsuleCollider cho Troop {troopTransform.name}");
                }
                
                if (debugMode) Debug.Log($"Đã đăng ký Troop {troopTransform.name} với Squad {squadEntity.Id}");
            }
        }
    
        /// <summary>
        /// Update the flag position to the squad's center
        /// </summary>
        private void UpdateFlagPosition()
        {
            if (squadFlag == null) return;
        
            // Tính toán vị trí trung tâm của troop visuals
            Vector3 center = CalculateSquadCenter();
        
            // Cập nhật vị trí flag
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
        
        /// <summary>
        /// Lấy Squad Entity
        /// </summary>
        public Entity GetSquadEntity()
        {
            return squadEntity;
        }
        
        /// <summary>
        /// Bật/tắt debug mode
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            debugMode = enabled;
            
            // Tìm và bật/tắt tất cả TroopDebugVisualizer trong các troop
            foreach (var troopTransform in troopVisuals)
            {
                if (troopTransform != null)
                {
                    var debugVisualizer = troopTransform.GetComponent<TroopDebugVisualizer>();
                    if (debugVisualizer != null)
                    {
                        debugVisualizer.enabled = enabled;
                    }
                }
            }
        }
    }
}