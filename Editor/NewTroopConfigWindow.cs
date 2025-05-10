using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using VikingRaven.Units.Components;
using VikingRaven.Core.Utils;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Behavior;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;

namespace VikingRaven.Editor
{
    /// <summary>
    /// Cửa sổ tạo Troop mới - cho phép tạo và cấu hình đơn vị mới từ đầu
    /// </summary>
    public class NewTroopConfigWindow : OdinEditorWindow
    {
        [MenuItem("VikingRaven/Công cụ/Tạo Troop mới")]
        private static void OpenWindow()
        {
            GetWindow<NewTroopConfigWindow>().Show();
        }

        [TitleGroup("Thông tin cơ bản")]
        [HorizontalGroup("Thông tin cơ bản/Split")]
        [VerticalGroup("Thông tin cơ bản/Split/Left")]
        [LabelText("Tên đơn vị")]
        public string troopName = "New_Troop";

        [HorizontalGroup("Thông tin cơ bản/Split")]
        [VerticalGroup("Thông tin cơ bản/Split/Left")]
        [EnumToggleButtons]
        [LabelText("Loại đơn vị")]
        public UnitType unitType = UnitType.Infantry;

        [HorizontalGroup("Thông tin cơ bản/Split")]
        [VerticalGroup("Thông tin cơ bản/Split/Right")]
        [LabelText("Mesh đơn vị")]
        [PreviewField(50)]
        public Mesh troopMesh;

        [HorizontalGroup("Thông tin cơ bản/Split")]
        [VerticalGroup("Thông tin cơ bản/Split/Right")]
        [ColorUsage(true, true)]
        [LabelText("Màu đơn vị")]
        public Color troopColor = Color.white;

        [HorizontalGroup("Thông tin cơ bản/Split")]
        [VerticalGroup("Thông tin cơ bản/Split/Right")]
        [LabelText("Prefab hiện có")]
        [InfoBox("Tùy chọn: Tải cấu hình từ prefab hiện có", InfoMessageType.None)]
        public GameObject existingPrefab;

        [HorizontalGroup("Thông tin cơ bản/Split")]
        [VerticalGroup("Thông tin cơ bản/Split/Right")]
        [Button("Tải cấu hình từ prefab", ButtonSizes.Small)]
        [GUIColor(0, 0.8f, 0.2f)]
        [EnableIf("existingPrefab")]
        private void LoadFromPrefab()
        {
            if (existingPrefab == null) return;

            var unitTypeComponent = existingPrefab.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                unitType = unitTypeComponent.UnitType;
            }

            var renderer = existingPrefab.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                troopColor = renderer.sharedMaterial.color;
            }

            var meshFilter = existingPrefab.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                troopMesh = meshFilter.sharedMesh;
            }

            troopName = existingPrefab.name;

            // Tải cấu hình component
            LoadComponentConfig();
        }

        [TabGroup("Thông số đơn vị")]
        [LabelText("Thông số Combat")]
        [Indent]
        [FoldoutGroup("Thông số đơn vị/Thông số Combat")]
        public CombatParameters combatParams = new CombatParameters();

        [TabGroup("Thông số đơn vị")]
        [LabelText("Thông số Health")]
        [Indent]
        [FoldoutGroup("Thông số đơn vị/Thông số Health")]
        public HealthParameters healthParams = new HealthParameters();

        [TabGroup("Thông số đơn vị")]
        [LabelText("Thông số Movement")]
        [Indent]
        [FoldoutGroup("Thông số đơn vị/Thông số Movement")]
        public MovementParameters movementParams = new MovementParameters();

        [TabGroup("Thông số đơn vị")]
        [LabelText("Thông số Stealth")]
        [Indent]
        [FoldoutGroup("Thông số đơn vị/Thông số Stealth")]
        public StealthParameters stealthParams = new StealthParameters();

        [TabGroup("Thông số đơn vị")]
        [LabelText("Thông số Aggro")]
        [Indent]
        [FoldoutGroup("Thông số đơn vị/Thông số Aggro")]
        public AggroParameters aggroParams = new AggroParameters();

        [TabGroup("Thành phần (Components)")]
        [LabelText("Component cơ bản")]
        [ListDrawerSettings(ShowIndexLabels = false, ShowPaging = false)]
        [TableList]
        public List<ComponentSelection> basicComponents = new List<ComponentSelection>
        {
            new ComponentSelection { Name = "TransformComponent", IsSelected = true, IsRequired = true },
            new ComponentSelection { Name = "HealthComponent", IsSelected = true, IsRequired = true },
            new ComponentSelection { Name = "UnitTypeComponent", IsSelected = true, IsRequired = true },
            new ComponentSelection { Name = "CombatComponent", IsSelected = true, IsRequired = true },
            new ComponentSelection { Name = "StateComponent", IsSelected = true, IsRequired = true },
            new ComponentSelection { Name = "AggroDetectionComponent", IsSelected = true, IsRequired = false },
        };

        [TabGroup("Thành phần (Components)")]
        [LabelText("Component di chuyển")]
        [ListDrawerSettings(ShowIndexLabels = false, ShowPaging = false)]
        [TableList]
        public List<ComponentSelection> movementComponents = new List<ComponentSelection>
        {
            new ComponentSelection { Name = "NavigationComponent", IsSelected = true, IsRequired = false },
            new ComponentSelection { Name = "FormationComponent", IsSelected = true, IsRequired = false },
            new ComponentSelection { Name = "SteeringComponent", IsSelected = true, IsRequired = false },
        };

        [TabGroup("Thành phần (Components)")]
        [LabelText("Component hành vi")]
        [ListDrawerSettings(ShowIndexLabels = false, ShowPaging = false)]
        [TableList]
        public List<ComponentSelection> behaviorComponents = new List<ComponentSelection>
        {
            new ComponentSelection { Name = "WeightedBehaviorComponent", IsSelected = true, IsRequired = false },
            new ComponentSelection { Name = "StealthComponent", IsSelected = false, IsRequired = false },
            new ComponentSelection { Name = "AnimationComponent", IsSelected = false, IsRequired = false },
        };

        [TabGroup("Behavior và Steering")]
        [LabelText("Behaviors")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [TableList]
        public List<BehaviorSelection> behaviors = new List<BehaviorSelection>
        {
            new BehaviorSelection { Name = "Attack", BehaviorType = "AttackBehavior", IsSelected = true },
            new BehaviorSelection { Name = "Move", BehaviorType = "MoveBehavior", IsSelected = true },
            new BehaviorSelection { Name = "Strafe", BehaviorType = "StrafeBehavior", IsSelected = true },
            new BehaviorSelection { Name = "Surround", BehaviorType = "SurroundBehavior", IsSelected = false },
            new BehaviorSelection { Name = "Phalanx", BehaviorType = "PhalanxBehavior", IsSelected = false },
            new BehaviorSelection { Name = "Testudo", BehaviorType = "TestudoBehavior", IsSelected = false },
            new BehaviorSelection { Name = "Charge", BehaviorType = "ChargeBehavior", IsSelected = false },
            new BehaviorSelection { Name = "Protect", BehaviorType = "ProtectBehavior", IsSelected = false },
            new BehaviorSelection { Name = "Cover", BehaviorType = "CoverBehavior", IsSelected = false },
            new BehaviorSelection { Name = "Ambush", BehaviorType = "AmbushMoveBehavior", IsSelected = false },
        };

        [TabGroup("Behavior và Steering")]
        [LabelText("Steering Behaviors")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [TableList]
        public List<SteeringSelection> steeringBehaviors = new List<SteeringSelection>
        {
            new SteeringSelection { Name = "Seek", BehaviorType = "SeekBehavior", IsSelected = true },
            new SteeringSelection { Name = "Flee", BehaviorType = "FleeBehavior", IsSelected = true },
            new SteeringSelection { Name = "Separation", BehaviorType = "SeparationBehavior", IsSelected = true },
            new SteeringSelection { Name = "Cohesion", BehaviorType = "CohesionBehavior", IsSelected = false },
            new SteeringSelection { Name = "FormationFollowing", BehaviorType = "FormationFollowingBehavior", IsSelected = true },
            new SteeringSelection { Name = "ObstacleAvoidance", BehaviorType = "ObstacleAvoidanceBehavior", IsSelected = true },
        };

        [TabGroup("Cấu hình Advanced")]
        [LabelText("Thêm NavMeshAgent")]
        public bool addNavMeshAgent = true;

        [TabGroup("Cấu hình Advanced")]
        [LabelText("Thêm Rigidbody")]
        public bool addRigidbody = true;

        [TabGroup("Cấu hình Advanced")]
        [LabelText("Thêm Capsule Collider")]
        public bool addCapsuleCollider = true;

        [TabGroup("Cấu hình Advanced")]
        [LabelText("Kích thước Collider")]
        [ShowIf("addCapsuleCollider")]
        public Vector3 colliderSize = new Vector3(1f, 2f, 1f);

        [TabGroup("Lưu/Tải")]
        [LabelText("Vị trí lưu Prefab")]
        [FolderPath]
        public string savePath = "Assets/Prefabs/Units";

        [SerializeField]
        public class CombatParameters
        {
            [LabelText("Sát thương"), Range(5f, 30f)]
            public float attackDamage = 10f;

            [LabelText("Tầm đánh"), Range(1f, 10f)]
            public float attackRange = 2f;

            [LabelText("Hồi chiêu (giây)"), Range(0.5f, 5f)]
            public float attackCooldown = 1.5f;

            [LabelText("Tốc độ di chuyển"), Range(1f, 10f)]
            public float moveSpeed = 3.0f;

            [LabelText("Lực đẩy lùi"), Range(1f, 10f)]
            public float knockbackForce = 5f;
        }

        [SerializeField]
        public class HealthParameters
        {
            [LabelText("Máu tối đa"), Range(50f, 300f)]
            public float maxHealth = 100f;

            [LabelText("Tốc độ hồi máu/giây"), Range(0f, 10f)]
            public float regenerationRate = 0f;
        }

        [SerializeField]
        public class MovementParameters
        {
            [LabelText("Tốc độ quay"), Range(1f, 10f)]
            public float rotationSpeed = 5.0f;

            [LabelText("Gia tốc tối đa"), Range(5f, 20f)]
            public float maxAcceleration = 10.0f;

            [LabelText("Tốc độ tối đa"), Range(1f, 10f)]
            public float maxSpeed = 3.0f;
        }

        [SerializeField]
        public class StealthParameters
        {
            [LabelText("Hệ số tốc độ Stealth"), Range(0.2f, 1f)]
            public float stealthMovementSpeedFactor = 0.5f;

            [LabelText("Bán kính phát hiện"), Range(1f, 10f)]
            public float detectionRadius = 5f;

            [LabelText("Thời gian khi mất Stealth"), Range(0.1f, 3f)]
            public float breakStealthDuration = 0.5f;
        }

        [SerializeField]
        public class AggroParameters
        {
            [LabelText("Tầm phát hiện kẻ địch"), Range(5f, 20f)]
            public float aggroRange = 10f;
        }

        [Serializable]
        public class ComponentSelection
        {
            [TableColumnWidth(200)]
            public string Name;

            [TableColumnWidth(80)]
            public bool IsSelected;

            [TableColumnWidth(80)]
            [ReadOnly]
            public bool IsRequired;
        }

        [Serializable]
        public class BehaviorSelection
        {
            [TableColumnWidth(150)]
            public string Name;

            [TableColumnWidth(200)]
            [ReadOnly]
            public string BehaviorType;

            [TableColumnWidth(80)]
            public bool IsSelected;
        }

        [Serializable]
        public class SteeringSelection
        {
            [TableColumnWidth(150)]
            public string Name;

            [TableColumnWidth(200)]
            [ReadOnly]
            public string BehaviorType;

            [TableColumnWidth(80)]
            public bool IsSelected;
        }

        [Button("Tạo đơn vị mới", ButtonSizes.Large)]
        [GUIColor(0, 0.8f, 0)]
        private void CreateNewTroop()
        {
            if (string.IsNullOrEmpty(troopName))
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng nhập tên cho đơn vị mới.", "OK");
                return;
            }

            if (troopMesh == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn mesh cho đơn vị mới.", "OK");
                return;
            }

            // Tạo GameObject mới
            GameObject troopObject = new GameObject(troopName);

            // Thêm MeshFilter và MeshRenderer
            MeshFilter meshFilter = troopObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = troopMesh;

            MeshRenderer meshRenderer = troopObject.AddComponent<MeshRenderer>();
            Material material = new Material(Shader.Find("Standard"));
            material.color = troopColor;
            meshRenderer.sharedMaterial = material;

            // Thêm Rigidbody nếu được chọn
            if (addRigidbody)
            {
                Rigidbody rb = troopObject.AddComponent<Rigidbody>();
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.mass = 1.0f;
                rb.linearDamping = 0.5f;
                rb.angularDamping = 0.5f;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }

            // Thêm Capsule Collider nếu được chọn
            if (addCapsuleCollider)
            {
                CapsuleCollider collider = troopObject.AddComponent<CapsuleCollider>();
                collider.height = colliderSize.y;
                collider.radius = colliderSize.x / 2;
                collider.center = new Vector3(0, colliderSize.y / 2, 0);
            }

            // Thêm NavMeshAgent nếu được chọn
            if (addNavMeshAgent)
            {
                UnityEngine.AI.NavMeshAgent agent = troopObject.AddComponent<UnityEngine.AI.NavMeshAgent>();
                agent.speed = movementParams.maxSpeed;
                agent.angularSpeed = movementParams.rotationSpeed * 100f;
                agent.acceleration = movementParams.maxAcceleration;
                agent.stoppingDistance = 0.1f;
                agent.radius = colliderSize.x / 2;
                agent.height = colliderSize.y;
            }

            // Thêm các components cơ bản
            BaseEntity entity = troopObject.AddComponent<BaseEntity>();
            
            // Dựa vào danh sách component đã chọn
            AddSelectedComponents(troopObject, basicComponents);
            AddSelectedComponents(troopObject, movementComponents);
            AddSelectedComponents(troopObject, behaviorComponents);

            // Cấu hình UnitTypeComponent
            UnitTypeComponent unitTypeComponent = troopObject.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                unitTypeComponent.SetUnitType(unitType);
            }

            // Cấu hình HealthComponent
            HealthComponent healthComponent = troopObject.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                SetFieldValue(healthComponent, "_maxHealth", healthParams.maxHealth);
                SetFieldValue(healthComponent, "_regenerationRate", healthParams.regenerationRate);
            }

            // Cấu hình CombatComponent
            CombatComponent combatComponent = troopObject.GetComponent<CombatComponent>();
            if (combatComponent != null)
            {
                combatComponent.SetAttackRange(combatParams.attackRange);
                combatComponent.SetAttackDamage(combatParams.attackDamage);
                combatComponent.SetAttackCooldown(combatParams.attackCooldown);
                SetFieldValue(combatComponent, "_moveSpeed", combatParams.moveSpeed);
                SetFieldValue(combatComponent, "_knockbackForce", combatParams.knockbackForce);
            }

            // Cấu hình StealthComponent
            StealthComponent stealthComponent = troopObject.GetComponent<StealthComponent>();
            if (stealthComponent != null)
            {
                SetFieldValue(stealthComponent, "_stealthMovementSpeedFactor", stealthParams.stealthMovementSpeedFactor);
                SetFieldValue(stealthComponent, "_detectionRadius", stealthParams.detectionRadius);
                SetFieldValue(stealthComponent, "_breakStealthDuration", stealthParams.breakStealthDuration);
            }

            // Cấu hình AggroDetectionComponent
            AggroDetectionComponent aggroComponent = troopObject.GetComponent<AggroDetectionComponent>();
            if (aggroComponent != null)
            {
                SetFieldValue(aggroComponent, "_aggroRange", aggroParams.aggroRange);
            }

            // Thiết lập WeightedBehaviorComponent và Behaviors
            WeightedBehaviorComponent behaviorComponent = troopObject.GetComponent<WeightedBehaviorComponent>();
            if (behaviorComponent != null)
            {
                behaviorComponent.Initialize();
                var selectedBehaviors = behaviors.Where(b => b.IsSelected).ToList();
                
                // Lưu thông tin để áp dụng sau khi prefab được tạo
                EditorPrefs.SetString("VikingRaven_LastCreatedTroop_Behaviors", 
                    JsonConvert.SerializeObject(selectedBehaviors));
            }

            // Thiết lập SteeringComponent và Steering Behaviors
            SteeringComponent steeringComponent = troopObject.GetComponent<SteeringComponent>();
            if (steeringComponent != null)
            {
                steeringComponent.Initialize();
                var selectedSteering = steeringBehaviors.Where(s => s.IsSelected).ToList();
                
                // Lưu thông tin để áp dụng sau khi prefab được tạo
                EditorPrefs.SetString("VikingRaven_LastCreatedTroop_Steering", 
                    JsonConvert.SerializeObject(selectedSteering));
            }

            // Tạo thư mục lưu nếu chưa tồn tại
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            // Lưu prefab
            string fullPath = $"{savePath}/{troopName}.prefab";
            
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(troopObject, fullPath);
            DestroyImmediate(troopObject);
            
            // Mở prefab để tiếp tục chỉnh sửa
            GameObject prefabInstance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            
            // Áp dụng behaviors
            ApplyBehaviorsAndSteering(prefabInstance);
            
            // Áp dụng prefab
            PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.AutomatedAction);
            
            // Hủy instance
            DestroyImmediate(prefabInstance);

            // Hiển thị thông báo
            EditorUtility.DisplayDialog("Hoàn tất", $"Đã tạo đơn vị mới {troopName} tại {fullPath}", "OK");
            
            // Chọn prefab trong Project window
            Selection.activeObject = prefabAsset;
        }

        private void AddSelectedComponents(GameObject troopObject, List<ComponentSelection> componentSelections)
        {
            foreach (var componentSelection in componentSelections.Where(c => c.IsSelected))
            {
                Type componentType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == componentSelection.Name);

                if (componentType != null && !troopObject.GetComponent(componentType))
                {
                    troopObject.AddComponent(componentType);
                }
            }
        }

        private void ApplyBehaviorsAndSteering(GameObject instance)
        {
            // Áp dụng behaviors
            string behaviorsJson = EditorPrefs.GetString("VikingRaven_LastCreatedTroop_Behaviors", "");
            if (!string.IsNullOrEmpty(behaviorsJson))
            {
                var selectedBehaviors = JsonConvert.DeserializeObject<List<BehaviorSelection>>(behaviorsJson);
                WeightedBehaviorComponent behaviorComponent = instance.GetComponent<WeightedBehaviorComponent>();
                
                if (behaviorComponent != null && behaviorComponent.BehaviorManager != null)
                {
                    foreach (var behavior in selectedBehaviors)
                    {
                        // Tìm kiếm type để tạo behavior
                        Type behaviorType = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == behavior.BehaviorType);

                        if (behaviorType != null)
                        {
                            try
                            {
                                // Tạo instance của behavior
                                // (Giả định rằng tất cả behaviors đều có constructor nhận entity làm tham số)
                                var entity = instance.GetComponent<BaseEntity>();
                                var newBehavior = Activator.CreateInstance(behaviorType, entity) as BaseBehavior;
                                
                                if (newBehavior != null)
                                {
                                    behaviorComponent.AddBehavior(newBehavior);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Lỗi khi tạo behavior {behaviorType.Name}: {ex.Message}");
                            }
                        }
                    }
                }

                // Xóa thông tin tạm thời
                EditorPrefs.DeleteKey("VikingRaven_LastCreatedTroop_Behaviors");
            }

            // Áp dụng steering behaviors
            string steeringJson = EditorPrefs.GetString("VikingRaven_LastCreatedTroop_Steering", "");
            if (!string.IsNullOrEmpty(steeringJson))
            {
                var selectedSteering = JsonConvert.DeserializeObject<List<SteeringSelection>>(steeringJson);
                SteeringComponent steeringComponent = instance.GetComponent<SteeringComponent>();
                
                if (steeringComponent != null && steeringComponent.SteeringManager != null)
                {
                    foreach (var steering in selectedSteering)
                    {
                        // Tìm kiếm type để tạo steering behavior
                        Type steeringType = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == steering.BehaviorType);

                        if (steeringType != null)
                        {
                            try
                            {
                                // Tạo instance của steering behavior
                                var newSteering = Activator.CreateInstance(steeringType) as Core.Steering.ISteeringBehavior;
                                
                                if (newSteering != null)
                                {
                                    steeringComponent.AddBehavior(newSteering);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Lỗi khi tạo steering behavior {steeringType.Name}: {ex.Message}");
                            }
                        }
                    }
                }

                // Xóa thông tin tạm thời
                EditorPrefs.DeleteKey("VikingRaven_LastCreatedTroop_Steering");
            }
        }

        private void LoadComponentConfig()
        {
            if (existingPrefab == null) return;

            // Load Combat Parameters
            CombatComponent combatComponent = existingPrefab.GetComponent<CombatComponent>();
            if (combatComponent != null)
            {
                combatParams.attackDamage = combatComponent.AttackDamage;
                combatParams.attackRange = combatComponent.AttackRange;
                combatParams.moveSpeed = combatComponent.MoveSpeed;
                
                // Sử dụng reflection để lấy các trường private
                combatParams.attackCooldown = GetFieldValue<float>(combatComponent, "_attackCooldown");
                combatParams.knockbackForce = GetFieldValue<float>(combatComponent, "_knockbackForce");
            }

            // Load Health Parameters
            HealthComponent healthComponent = existingPrefab.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthParams.maxHealth = healthComponent.MaxHealth;
                healthParams.regenerationRate = GetFieldValue<float>(healthComponent, "_regenerationRate");
            }

            // Load Aggro Parameters
            AggroDetectionComponent aggroComponent = existingPrefab.GetComponent<AggroDetectionComponent>();
            if (aggroComponent != null)
            {
                aggroParams.aggroRange = aggroComponent.AggroRange;
            }

            // Load Stealth Parameters
            StealthComponent stealthComponent = existingPrefab.GetComponent<StealthComponent>();
            if (stealthComponent != null)
            {
                stealthParams.stealthMovementSpeedFactor = GetFieldValue<float>(stealthComponent, "_stealthMovementSpeedFactor");
                stealthParams.detectionRadius = GetFieldValue<float>(stealthComponent, "_detectionRadius");
                stealthParams.breakStealthDuration = GetFieldValue<float>(stealthComponent, "_breakStealthDuration");
            }

            // Movement Parameters - lấy từ NavigationComponent và TransformComponent
            TransformComponent transformComponent = existingPrefab.GetComponent<TransformComponent>();
            if (transformComponent != null)
            {
                movementParams.rotationSpeed = GetFieldValue<float>(transformComponent, "_rotationSpeed");
            }

            // Cập nhật trạng thái của các component
            UpdateComponentSelections();
        }

        private void UpdateComponentSelections()
        {
            if (existingPrefab == null) return;

            // Cập nhật basicComponents
            foreach (var selection in basicComponents)
            {
                Type componentType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == selection.Name);
                
                if (componentType != null)
                {
                    selection.IsSelected = existingPrefab.GetComponent(componentType) != null || selection.IsRequired;
                }
            }

            // Cập nhật movementComponents
            foreach (var selection in movementComponents)
            {
                Type componentType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == selection.Name);
                
                if (componentType != null)
                {
                    selection.IsSelected = existingPrefab.GetComponent(componentType) != null || selection.IsRequired;
                }
            }

            // Cập nhật behaviorComponents
            foreach (var selection in behaviorComponents)
            {
                Type componentType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == selection.Name);
                
                if (componentType != null)
                {
                    selection.IsSelected = existingPrefab.GetComponent(componentType) != null || selection.IsRequired;
                }
            }

            // Cập nhật trạng thái NavMeshAgent và Colliders
            addNavMeshAgent = existingPrefab.GetComponent<UnityEngine.AI.NavMeshAgent>() != null;
            addRigidbody = existingPrefab.GetComponent<Rigidbody>() != null;
            
            CapsuleCollider capsuleCollider = existingPrefab.GetComponent<CapsuleCollider>();
            addCapsuleCollider = capsuleCollider != null;
            
            if (capsuleCollider != null)
            {
                colliderSize = new Vector3(capsuleCollider.radius * 2, capsuleCollider.height, capsuleCollider.radius * 2);
            }
        }

        private T GetFieldValue<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic);
            
            if (field != null)
            {
                return (T)field.GetValue(obj);
            }
            
            return default(T);
        }

        private void SetFieldValue(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic);
            
            if (field != null)
            {
                try
                {
                    field.SetValue(target, value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Không thể đặt giá trị cho {fieldName}: {e.Message}");
                }
            }
        }

        [TabGroup("Lưu/Tải")]
        [FolderPath]
        [LabelText("File cấu hình")]
        public string configFilePath;

        [TabGroup("Lưu/Tải")]
        [Button("Lưu cấu hình")]
        [GUIColor(0, 0.5f, 1)]
        private void SaveConfiguration()
        {
            if (string.IsNullOrEmpty(configFilePath))
            {
                configFilePath = EditorUtility.SaveFilePanel("Lưu cấu hình đơn vị", "", $"TroopConfig_{troopName}", "json");
                if (string.IsNullOrEmpty(configFilePath)) return;
            }

            var config = new
            {
                troopName,
                unitType,
                troopColor,
                combatParams,
                healthParams,
                movementParams,
                stealthParams,
                aggroParams,
                basicComponents,
                movementComponents,
                behaviorComponents,
                behaviors,
                steeringBehaviors,
                addNavMeshAgent,
                addRigidbody,
                addCapsuleCollider,
                colliderSize
            };

            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
            EditorUtility.DisplayDialog("Lưu cấu hình", "Cấu hình đơn vị đã được lưu thành công.", "OK");
        }

        [TabGroup("Lưu/Tải")]
        [Button("Tải cấu hình")]
        [GUIColor(1, 0.5f, 0)]
        private void LoadConfiguration()
        {
            if (string.IsNullOrEmpty(configFilePath) || !File.Exists(configFilePath))
            {
                configFilePath = EditorUtility.OpenFilePanel("Tải cấu hình đơn vị", "", "json");
                if (string.IsNullOrEmpty(configFilePath) || !File.Exists(configFilePath)) return;
            }

            string json = File.ReadAllText(configFilePath);
            dynamic config = JsonConvert.DeserializeObject(json);

            troopName = config.troopName;
            unitType = (UnitType)Enum.Parse(typeof(UnitType), config.unitType.ToString());
            troopColor = new Color(
                (float)config.troopColor.r,
                (float)config.troopColor.g,
                (float)config.troopColor.b,
                (float)config.troopColor.a
            );

            // Tải các thông số
            combatParams = JsonConvert.DeserializeObject<CombatParameters>(config.combatParams.ToString());
            healthParams = JsonConvert.DeserializeObject<HealthParameters>(config.healthParams.ToString());
            movementParams = JsonConvert.DeserializeObject<MovementParameters>(config.movementParams.ToString());
            stealthParams = JsonConvert.DeserializeObject<StealthParameters>(config.stealthParams.ToString());
            aggroParams = JsonConvert.DeserializeObject<AggroParameters>(config.aggroParams.ToString());

            // Tải danh sách component
            basicComponents = JsonConvert.DeserializeObject<List<ComponentSelection>>(config.basicComponents.ToString());
            movementComponents = JsonConvert.DeserializeObject<List<ComponentSelection>>(config.movementComponents.ToString());
            behaviorComponents = JsonConvert.DeserializeObject<List<ComponentSelection>>(config.behaviorComponents.ToString());

            // Tải danh sách behavior
            behaviors = JsonConvert.DeserializeObject<List<BehaviorSelection>>(config.behaviors.ToString());
            steeringBehaviors = JsonConvert.DeserializeObject<List<SteeringSelection>>(config.steeringBehaviors.ToString());

            // Tải cài đặt khác
            addNavMeshAgent = config.addNavMeshAgent;
            addRigidbody = config.addRigidbody;
            addCapsuleCollider = config.addCapsuleCollider;

            colliderSize = new Vector3(
                (float)config.colliderSize.x,
                (float)config.colliderSize.y,
                (float)config.colliderSize.z
            );

            EditorUtility.DisplayDialog("Tải cấu hình", "Cấu hình đơn vị đã được tải thành công.", "OK");
        }
    }
}