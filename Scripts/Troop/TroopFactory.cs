using System.Collections.Generic;
using Core.Patterns.Singleton;
using SteeringBehavior;
using UnityEngine;
using Utils;

namespace Troop
{
    public class TroopFactory : ManualSingletonMono<TroopFactory>
    {
        [Tooltip("Prefab troop cơ bản")]
        public GameObject troopPrefab;
    
        [Tooltip("Danh sách các troop config có sẵn")]
        public List<TroopConfigSO> availableTroopConfigs;
    
        // Tạo một troop từ config
        public TroopController CreateTroop(TroopConfigSO config, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            // Kiểm tra config
            if (config == null)
            {
                Debug.LogError("TroopFactory: Không thể tạo troop vì config là null");
                return null;
            }
        
            // Tạo game object
            GameObject troopObject;
            if (config.troopPrefab != null)
            {
                troopObject = (GameObject)Instantiate((Object)config.troopPrefab, position, rotation, parent);
            }
            else if (troopPrefab != null)
            {
                troopObject = Instantiate(troopPrefab, position, rotation, parent);
            }
            else
            {
                Debug.LogError("TroopFactory: Không thể tạo troop vì không có prefab");
                return null;
            }
        
            // Thiết lập tên
            troopObject.name = config.troopName;
        
            // Lấy hoặc thêm các component cần thiết
            TroopController controller = troopObject.GetComponent<TroopController>();
            if (controller == null)
            {
                controller = troopObject.AddComponent<TroopController>();
            }
        
            TroopView view = troopObject.GetComponent<TroopView>();
            if (view == null)
            {
                view = troopObject.AddComponent<TroopView>();
            }
        
            // Khởi tạo controller với config
            controller.Initialize(config);
        
            if (config.behaviors.Find(b => b is PathFollowingBehaviorSO) != null)
            {
                troopObject.AddComponent<PathComponent>();
            }
        
            return controller;
        }
    
        // Tạo một troop theo tên
        public TroopController CreateTroopByName(string troopName, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            // Tìm config theo tên
            TroopConfigSO config = availableTroopConfigs.Find(c => c.troopName == troopName);
            if (config == null)
            {
                Debug.LogError($"TroopFactory: Không tìm thấy config cho troop '{troopName}'");
                return null;
            }
        
            return CreateTroop(config, position, rotation, parent);
        }
    }
}