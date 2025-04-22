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
        
        [Tooltip("Template cho các behavior")]
        public BehaviorTemplateSet behaviorTemplateSet;
        
        [Tooltip("Danh sách các troop config có sẵn")]
        public List<TroopConfigSO> availableTroopConfigs;
        
        [Tooltip("Danh sách các troop template")]
        public List<TroopTemplate> availableTroopTemplates;
        
        // Dictionary các behavior cơ bản cho từng loại troop
        private Dictionary<TroopClassType, List<System.Type>> _defaultBehaviors = new Dictionary<TroopClassType, List<System.Type>>();
        
        private void Awake()
        {
            // Khởi tạo dictionary
            InitializeDefaultBehaviors();
        }
        
        // Khởi tạo các behavior mặc định cho từng loại troop
        private void InitializeDefaultBehaviors()
        {
            // Behavior cơ bản cho mọi troop
            List<System.Type> basicBehaviors = new List<System.Type>
            {
                typeof(SeekBehaviorSO),
                typeof(ArrivalBehaviorSO),
                typeof(SeparationBehaviorSO)
            };
            
            // Infantry - cơ bản thêm cohesion và alignment
            _defaultBehaviors[TroopClassType.Infantry] = new List<System.Type>(basicBehaviors);
            _defaultBehaviors[TroopClassType.Infantry].Add(typeof(CohesionBehaviorSO));
            _defaultBehaviors[TroopClassType.Infantry].Add(typeof(AlignmentBehaviorSO));
            
            // HeavyInfantry - thêm khả năng phòng thủ
            _defaultBehaviors[TroopClassType.HeavyInfantry] = new List<System.Type>(_defaultBehaviors[TroopClassType.Infantry]);
            _defaultBehaviors[TroopClassType.HeavyInfantry].Add(typeof(ProtectBehaviorSO));
            _defaultBehaviors[TroopClassType.HeavyInfantry].Add(typeof(PhalanxBehaviorSO));
            
            // Berserker - tập trung vào tấn công
            _defaultBehaviors[TroopClassType.Berserker] = new List<System.Type>(basicBehaviors);
            _defaultBehaviors[TroopClassType.Berserker].Add(typeof(ChargeBehaviorSO));
            
            // Archer - tấn công từ xa
            _defaultBehaviors[TroopClassType.Archer] = new List<System.Type>(basicBehaviors);
            _defaultBehaviors[TroopClassType.Archer].Add(typeof(CohesionBehaviorSO));
            _defaultBehaviors[TroopClassType.Archer].Add(typeof(FleeBehaviorSO));
            
            // Scout - di chuyển nhanh
            _defaultBehaviors[TroopClassType.Scout] = new List<System.Type>(basicBehaviors);
            _defaultBehaviors[TroopClassType.Scout].Add(typeof(AmbushMoveBehaviorSO));
            
            // Commander - hỗ trợ đồng minh
            _defaultBehaviors[TroopClassType.Commander] = new List<System.Type>(basicBehaviors);
            _defaultBehaviors[TroopClassType.Commander].Add(typeof(CohesionBehaviorSO));
            _defaultBehaviors[TroopClassType.Commander].Add(typeof(ProtectBehaviorSO));
            
            // Defender - chuyên bảo vệ
            _defaultBehaviors[TroopClassType.Defender] = new List<System.Type>(basicBehaviors);
            _defaultBehaviors[TroopClassType.Defender].Add(typeof(ProtectBehaviorSO));
            _defaultBehaviors[TroopClassType.Defender].Add(typeof(TestudoBehaviorSO));
            
            // Assassin - tấn công nhanh
            _defaultBehaviors[TroopClassType.Assassin] = new List<System.Type>(basicBehaviors);
            _defaultBehaviors[TroopClassType.Assassin].Add(typeof(JumpAttackBehaviorSO));
            
            // Custom - chỉ có behavior cơ bản
            _defaultBehaviors[TroopClassType.Custom] = new List<System.Type>(basicBehaviors);
        }
    
        // public TroopController CreateTroop(TroopConfigSO config, Vector3 position, Quaternion rotation, Transform parent = null)
        // {
        //     // Kiểm tra config
        //     if (config == null)
        //     {
        //         Debug.LogError("TroopFactory: Không thể tạo troop vì config là null");
        //         return null;
        //     }
        //
        //     // Tạo game object
        //     GameObject troopObject;
        //     if (config.troopPrefab != null)
        //     {
        //         troopObject = (GameObject)Instantiate((Object)config.troopPrefab, position, rotation, parent);
        //     }
        //     else if (troopPrefab != null)
        //     {
        //         troopObject = Instantiate(troopPrefab, position, rotation, parent);
        //     }
        //     else
        //     {
        //         Debug.LogError("TroopFactory: Không thể tạo troop vì không có prefab");
        //         return null;
        //     }
        //
        //     // Thiết lập tên
        //     troopObject.name = config.troopName;
        //
        //     // Lấy hoặc thêm các component cần thiết
        //     TroopController controller = troopObject.GetComponent<TroopController>();
        //     if (controller == null)
        //     {
        //         controller = troopObject.AddComponent<TroopController>();
        //     }
        //
        //     TroopView view = troopObject.GetComponent<TroopView>();
        //     if (view == null)
        //     {
        //         view = troopObject.AddComponent<TroopView>();
        //     }
        //
        //     // Khởi tạo controller với config
        //     controller.Initialize(config);
        //
        //     // Thêm PathComponent nếu có Path Following behavior
        //     if (config.behaviors.Find(b => b is PathFollowingBehaviorSO) != null)
        //     {
        //         troopObject.AddComponent<PathComponent>();
        //     }
        //
        //     return controller;
        // }
        // Trong TroopFactory.cs, thêm debug log để kiểm tra:
        public TroopController CreateTroop(TroopConfigSO config, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (!config)
                return null;
    
            GameObject troopObject;
            if (config.troopPrefab)
            {
                troopObject = Instantiate(config.troopPrefab, position, rotation, parent);
            }
            // else if (troopPrefab)
            // {
            //     troopObject = Instantiate(troopPrefab, position, rotation, parent);
            // }
            else
                return null;
            troopObject.name = config.troopName;
            TroopController controller = troopObject.GetComponent<TroopController>();
            controller.Initialize(config);
    
            return controller;
        }
        
        // Tạo một troop theo loại và team
        public TroopController CreateTroopByType(TroopClassType troopType, string teamTag, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            // Tạo config mới dựa trên loại troop
            TroopConfigSO config = CreateConfigFromType(troopType, teamTag);
            
            if (config == null)
            {
                Debug.LogError($"TroopFactory: Không thể tạo config cho troop loại {troopType}");
                return null;
            }
            
            return CreateTroop(config, position, rotation, parent);
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
        
        // Tạo một troop từ template có sẵn
        public TroopController CreateTroopFromTemplate(string templateName, string troopName, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            // Tìm template
            TroopTemplate template = availableTroopTemplates.Find(t => t.templateName == templateName);
            
            if (template == null)
            {
                Debug.LogError($"TroopFactory: Không tìm thấy template '{templateName}'");
                return null;
            }
            
            // Tạo config từ template
            TroopConfigSO config = template.CreateTroopConfig(troopName);
            
            return CreateTroop(config, position, rotation, parent);
        }
        
        // Tạo config mới dựa trên loại troop
        private TroopConfigSO CreateConfigFromType(TroopClassType troopType, string teamTag)
        {
            TroopConfigSO config = ScriptableObject.CreateInstance<TroopConfigSO>();
            
            // Set tên và thuộc tính cơ bản
            config.troopName = $"{teamTag}_{troopType}_{System.Guid.NewGuid().ToString().Substring(0, 4)}";
            
            // Set các thuộc tính cơ bản dựa trên loại troop
            SetBasicStats(config, troopType);
            
            // Thêm các behavior mặc định
            config.behaviors = new List<SteeringBehaviorSO>();
            
            // Thêm behavior dựa trên loại troop
            if (_defaultBehaviors.ContainsKey(troopType))
            {
                foreach (var behaviorType in _defaultBehaviors[troopType])
                {
                    // Tìm behavior tương ứng trong danh sách availableBehaviors
                    SteeringBehaviorSO behavior = FindBehaviorByType(behaviorType);
                    
                    if (behavior != null)
                    {
                        config.behaviors.Add(behavior);
                    }
                }
            }
            
            // Nếu là custom, thêm behavior từ template set
            if (troopType == TroopClassType.Custom && behaviorTemplateSet != null)
            {
                config.behaviors.AddRange(behaviorTemplateSet.GetDefaultBehaviors());
            }
            
            return config;
        }
        
        // Set thuộc tính cơ bản dựa trên loại troop
        private void SetBasicStats(TroopConfigSO config, TroopClassType troopType)
        {
            switch (troopType)
            {
                case TroopClassType.Infantry:
                    config.health = 100f;
                    config.attackPower = 10f;
                    config.moveSpeed = 3f;
                    config.attackRange = 1.5f;
                    config.attackSpeed = 1f;
                    break;
                    
                case TroopClassType.HeavyInfantry:
                    config.health = 150f;
                    config.attackPower = 12f;
                    config.moveSpeed = 2.5f;
                    config.attackRange = 1.5f;
                    config.attackSpeed = 0.8f;
                    break;
                    
                case TroopClassType.Berserker:
                    config.health = 80f;
                    config.attackPower = 18f;
                    config.moveSpeed = 3.5f;
                    config.attackRange = 1.5f;
                    config.attackSpeed = 1.2f;
                    break;
                    
                case TroopClassType.Archer:
                    config.health = 70f;
                    config.attackPower = 8f;
                    config.moveSpeed = 3f;
                    config.attackRange = 8f;
                    config.attackSpeed = 0.8f;
                    break;
                    
                case TroopClassType.Scout:
                    config.health = 80f;
                    config.attackPower = 7f;
                    config.moveSpeed = 4.5f;
                    config.attackRange = 1.5f;
                    config.attackSpeed = 1.1f;
                    break;
                    
                case TroopClassType.Commander:
                    config.health = 120f;
                    config.attackPower = 9f;
                    config.moveSpeed = 3.2f;
                    config.attackRange = 1.5f;
                    config.attackSpeed = 1f;
                    break;
                    
                case TroopClassType.Defender:
                    config.health = 140f;
                    config.attackPower = 8f;
                    config.moveSpeed = 2.5f;
                    config.attackRange = 1.5f;
                    config.attackSpeed = 0.8f;
                    break;
                    
                case TroopClassType.Assassin:
                    config.health = 70f;
                    config.attackPower = 15f;
                    config.moveSpeed = 4f;
                    config.attackRange = 1.5f;
                    config.attackSpeed = 1.5f;
                    break;
                    
                case TroopClassType.Custom:
                default:
                    config.health = 100f;
                    config.attackPower = 10f;
                    config.moveSpeed = 3f;
                    config.attackRange = 1.5f;
                    config.attackSpeed = 1f;
                    break;
            }
        }
        
        // Tìm behavior theo loại
        private SteeringBehaviorSO FindBehaviorByType(System.Type behaviorType)
        {
            // Tìm trong available troop configs trước
            foreach (var troopConfig in availableTroopConfigs)
            {
                if (troopConfig.behaviors == null) continue;
                
                foreach (var behavior in troopConfig.behaviors)
                {
                    if (behavior != null && behavior.GetType() == behaviorType)
                    {
                        return behavior;
                    }
                }
            }
            
            // Nếu không tìm thấy, tìm trong project assets
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:" + behaviorType.Name);
            
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<SteeringBehaviorSO>(path);
            }
            
            Debug.LogWarning($"TroopFactory: Không tìm thấy behavior loại {behaviorType.Name}");
            return null;
        }
    }
    
}