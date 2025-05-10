using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using VikingRaven.Units.Components;
using System.Reflection;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using VikingRaven.Core.ECS;

namespace VikingRaven.Editor
{
    /// <summary>
    /// Cửa sổ cấu hình Component cho Troop - cho phép chỉnh sửa các thành phần của các loại đơn vị
    /// </summary>
    public class TroopComponentConfigWindow : OdinEditorWindow
    {
        [MenuItem("VikingRaven/Công cụ/Cấu hình Troop Component")]
        private static void OpenWindow()
        {
            GetWindow<TroopComponentConfigWindow>().Show();
        }

        [Serializable]
        public class ComponentConfig
        {
            public string ComponentName;
            public bool IsActive;
            public Dictionary<string, object> Parameters = new Dictionary<string, object>();
        }

        [Serializable]
        public class TroopConfig
        {
            public UnitType UnitType;
            public List<ComponentConfig> Components = new List<ComponentConfig>();
        }

        [TitleGroup("Chọn loại đơn vị")]
        [HorizontalGroup("Chọn loại đơn vị/Split")]
        [VerticalGroup("Chọn loại đơn vị/Split/Left")]
        [EnumToggleButtons]
        [LabelText("Loại đơn vị")]
        public UnitType selectedUnitType;

        [HorizontalGroup("Chọn loại đơn vị/Split")]
        [VerticalGroup("Chọn loại đơn vị/Split/Right")]
        [InlineEditor]
        [PreviewField(50)]
        [LabelText("Prefab đơn vị")]
        public GameObject troopPrefab;

        [HorizontalGroup("Chọn loại đơn vị/Split")]
        [VerticalGroup("Chọn loại đơn vị/Split/Right")]
        [Button("Tải Component từ Prefab")]
        [GUIColor(0, 0.8f, 0.2f)]
        private void LoadPrefabComponents()
        {
            if (troopPrefab == null) 
            {
                EditorUtility.DisplayDialog("Không có Prefab", "Vui lòng chọn một prefab đơn vị trước.", "OK");
                return;
            }
            
            components.Clear();
            components.AddRange(troopPrefab.GetComponents<BaseComponent>());

            // Lấy loại đơn vị từ prefab
            var unitTypeComponent = troopPrefab.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                selectedUnitType = unitTypeComponent.UnitType;
            }
        }

        [TabGroup("Component")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "GetComponentLabel")]
        [InlineEditor]
        [LabelText("Danh sách Component")]
        public List<BaseComponent> components = new List<BaseComponent>();

        private string GetComponentLabel(BaseComponent component)
        {
            if (component == null) return "Null";
            return component.GetType().Name;
        }

        [TabGroup("Cấu hình tự động")]
        [Button("Tự động cấu hình Component", ButtonSizes.Large)]
        [GUIColor(0, 0.7f, 0)]
        private void AutoConfigureComponents()
        {
            if (troopPrefab == null || components.Count == 0)
            {
                EditorUtility.DisplayDialog("Không có Component", "Không có prefab đơn vị hoặc không tìm thấy component. Vui lòng tải prefab trước.", "OK");
                return;
            }

            // Cấu hình component dựa trên loại đơn vị
            foreach (var component in components)
            {
                if (component is HealthComponent healthComponent)
                {
                    ConfigureHealthComponent(healthComponent, selectedUnitType);
                }
                else if (component is CombatComponent combatComponent)
                {
                    ConfigureCombatComponent(combatComponent, selectedUnitType);
                }
                else if (component is SteeringComponent steeringComponent)
                {
                    ConfigureSteeringComponent(steeringComponent, selectedUnitType);
                }
                else if (component is AggroDetectionComponent aggroComponent)
                {
                    ConfigureAggroComponent(aggroComponent, selectedUnitType);
                }
                else if (component is StealthComponent stealthComponent)
                {
                    ConfigureStealthComponent(stealthComponent, selectedUnitType);
                }
                else if (component is FormationComponent formationComponent)
                {
                    ConfigureFormationComponent(formationComponent, selectedUnitType);
                }
                // Thêm các component khác nếu cần
            }

            EditorUtility.SetDirty(troopPrefab);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Cấu hình tự động hoàn tất", 
                $"Các component đã được cấu hình tự động cho đơn vị {selectedUnitType}.", "OK");
        }

        private void ConfigureHealthComponent(HealthComponent component, UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Infantry:
                    SetFieldValue(component, "_maxHealth", 150f);
                    SetFieldValue(component, "_regenerationRate", 0.5f);
                    break;
                case UnitType.Archer:
                    SetFieldValue(component, "_maxHealth", 100f);
                    SetFieldValue(component, "_regenerationRate", 0.3f);
                    break;
                case UnitType.Pike:
                    SetFieldValue(component, "_maxHealth", 130f);
                    SetFieldValue(component, "_regenerationRate", 0.4f);
                    break;
            }
        }

        private void ConfigureCombatComponent(CombatComponent component, UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Infantry:
                    component.SetAttackRange(2.0f);
                    component.SetAttackDamage(15.0f);
                    component.SetAttackCooldown(1.2f);
                    SetFieldValue(component, "_moveSpeed", 3.0f);
                    SetFieldValue(component, "_knockbackForce", 5.0f);
                    break;
                case UnitType.Archer:
                    component.SetAttackRange(8.0f);
                    component.SetAttackDamage(10.0f);
                    component.SetAttackCooldown(2.0f);
                    SetFieldValue(component, "_moveSpeed", 2.5f);
                    SetFieldValue(component, "_knockbackForce", 2.0f);
                    break;
                case UnitType.Pike:
                    component.SetAttackRange(3.0f);
                    component.SetAttackDamage(20.0f);
                    component.SetAttackCooldown(1.5f);
                    SetFieldValue(component, "_moveSpeed", 2.8f);
                    SetFieldValue(component, "_knockbackForce", 4.0f);
                    break;
            }
        }

        private void ConfigureSteeringComponent(SteeringComponent component, UnitType unitType)
        {
            // Các cấu hình cụ thể cho SteeringComponent
            var manager = component.SteeringManager;
            if (manager != null)
            {
                switch (unitType)
                {
                    case UnitType.Infantry:
                        SetFieldValue(manager, "MaxAcceleration", 10.0f);
                        SetFieldValue(manager, "MaxSpeed", 3.0f);
                        break;
                    case UnitType.Archer:
                        SetFieldValue(manager, "MaxAcceleration", 8.0f);
                        SetFieldValue(manager, "MaxSpeed", 2.5f);
                        break;
                    case UnitType.Pike:
                        SetFieldValue(manager, "MaxAcceleration", 7.0f);
                        SetFieldValue(manager, "MaxSpeed", 2.2f);
                        break;
                }
            }
        }

        private void ConfigureAggroComponent(AggroDetectionComponent component, UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Infantry:
                    SetFieldValue(component, "_aggroRange", 10f);
                    break;
                case UnitType.Archer:
                    SetFieldValue(component, "_aggroRange", 12f);
                    break;
                case UnitType.Pike:
                    SetFieldValue(component, "_aggroRange", 9f);
                    break;
            }
        }

        private void ConfigureStealthComponent(StealthComponent component, UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Infantry:
                    SetFieldValue(component, "_stealthMovementSpeedFactor", 0.5f);
                    SetFieldValue(component, "_detectionRadius", 4f);
                    break;
                case UnitType.Archer:
                    SetFieldValue(component, "_stealthMovementSpeedFactor", 0.6f);
                    SetFieldValue(component, "_detectionRadius", 5f);
                    break;
                case UnitType.Pike:
                    SetFieldValue(component, "_stealthMovementSpeedFactor", 0.4f);
                    SetFieldValue(component, "_detectionRadius", 3.5f);
                    break;
            }
        }

        private void ConfigureFormationComponent(FormationComponent component, UnitType unitType)
        {
            // Cấu hình formation chung cho mọi loại đơn vị
            // Cấu hình cụ thể hơn sẽ được thiết lập bởi FormationSystem
            SetFieldValue(component, "_currentFormationType", FormationType.Line);
        }

        private void SetFieldValue(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, 
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
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
        [LabelText("Đường dẫn file cấu hình")]
        public string configFilePath;

        [TabGroup("Lưu/Tải")]
        [Button("Lưu cấu hình")]
        [GUIColor(0, 0.5f, 1)]
        private void SaveConfiguration()
        {
            if (troopPrefab == null || components.Count == 0)
            {
                EditorUtility.DisplayDialog("Không có Component", "Không có prefab đơn vị hoặc không tìm thấy component. Vui lòng tải prefab trước.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(configFilePath))
            {
                configFilePath = EditorUtility.SaveFilePanel("Lưu cấu hình đơn vị", "", $"TroopConfig_{selectedUnitType}", "json");
                if (string.IsNullOrEmpty(configFilePath)) return;
            }

            TroopConfig configData = new TroopConfig { UnitType = selectedUnitType };

            foreach (var component in components)
            {
                ComponentConfig config = new ComponentConfig
                {
                    ComponentName = component.GetType().Name,
                    IsActive = component.IsActive
                };

                // Lấy các thuộc tính có thể serialize
                foreach (var field in component.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                    {
                        var value = field.GetValue(component);
                        if (value != null && IsSerializable(value.GetType()))
                        {
                            config.Parameters[field.Name] = value;
                        }
                    }
                }

                configData.Components.Add(config);
            }

            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(configData, Formatting.Indented));
            EditorUtility.DisplayDialog("Lưu cấu hình", "Cấu hình đơn vị đã được lưu thành công.", "OK");
        }

        [TabGroup("Lưu/Tải")]
        [Button("Tải cấu hình")]
        [GUIColor(1, 0.5f, 0)]
        private void LoadConfiguration()
        {
            if (troopPrefab == null)
            {
                EditorUtility.DisplayDialog("Không có Prefab", "Vui lòng chọn một prefab đơn vị trước.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(configFilePath) || !File.Exists(configFilePath))
            {
                configFilePath = EditorUtility.OpenFilePanel("Tải cấu hình đơn vị", "", "json");
                if (string.IsNullOrEmpty(configFilePath) || !File.Exists(configFilePath)) return;
            }

            string json = File.ReadAllText(configFilePath);
            TroopConfig configData = JsonConvert.DeserializeObject<TroopConfig>(json);

            selectedUnitType = configData.UnitType;

            // Tìm hoặc tạo mới UnitTypeComponent
            var unitTypeComponent = troopPrefab.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent == null)
            {
                unitTypeComponent = troopPrefab.AddComponent<UnitTypeComponent>();
            }
            unitTypeComponent.SetUnitType(selectedUnitType);

            // Áp dụng cấu hình component
            foreach (var config in configData.Components)
            {
                // Tìm kiếm type của component
                Type componentType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == config.ComponentName);

                if (componentType == null) continue;

                // Tìm hoặc tạo mới component
                BaseComponent component = troopPrefab.GetComponent(componentType) as BaseComponent;
                if (component == null)
                {
                    component = troopPrefab.AddComponent(componentType) as BaseComponent;
                }

                if (component != null)
                {
                    component.IsActive = config.IsActive;

                    // Áp dụng các tham số
                    foreach (var param in config.Parameters)
                    {
                        var field = componentType.GetField(param.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field != null)
                        {
                            try
                            {
                                var value = Convert.ChangeType(param.Value, field.FieldType);
                                field.SetValue(component, value);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Lỗi khi đặt giá trị cho {param.Key} trên {config.ComponentName}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            // Refresh danh sách component
            LoadPrefabComponents();

            EditorUtility.SetDirty(troopPrefab);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Tải cấu hình", "Cấu hình đơn vị đã được tải thành công.", "OK");
        }

        private bool IsSerializable(Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type == typeof(Vector2) || 
                   type == typeof(Vector3) || type == typeof(Quaternion) || type == typeof(Color) ||
                   type.IsEnum || type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        [TabGroup("Thao tác")]
        [Button("Thêm Component mới", ButtonSizes.Medium)]
        [InfoBox("Thêm component mới vào đơn vị")]
        private void AddComponent()
        {
            if (troopPrefab == null)
            {
                EditorUtility.DisplayDialog("Không có Prefab", "Vui lòng chọn một prefab đơn vị trước.", "OK");
                return;
            }

            GenericMenu menu = new GenericMenu();
            
            // Tìm tất cả các loại component kế thừa từ BaseComponent
            var baseComponentType = typeof(BaseComponent);
            var componentTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => baseComponentType.IsAssignableFrom(t) && !t.IsAbstract && t != baseComponentType);

            foreach (var type in componentTypes)
            {
                bool alreadyHas = troopPrefab.GetComponent(type) != null;
                if (alreadyHas)
                {
                    menu.AddDisabledItem(new GUIContent(type.Name), true);
                }
                else
                {
                    menu.AddItem(new GUIContent(type.Name), false, () => AddComponentOfType(type));
                }
            }

            menu.ShowAsContext();
        }

        private void AddComponentOfType(Type componentType)
        {
            var component = troopPrefab.AddComponent(componentType) as BaseComponent;
            if (component != null)
            {
                components.Add(component);
                EditorUtility.SetDirty(troopPrefab);
            }
        }

        [TabGroup("Thao tác")]
        [Button("Xóa Component đã chọn", ButtonSizes.Medium)]
        [GUIColor(1, 0.3f, 0.3f)]
        private void RemoveSelectedComponent()
        {
            if (troopPrefab == null || Selection.activeObject as BaseComponent == null)
            {
                EditorUtility.DisplayDialog("Không có Component được chọn", 
                    "Vui lòng chọn một component trong danh sách trước khi xóa.", "OK");
                return;
            }

            BaseComponent selectedComponent = Selection.activeObject as BaseComponent;
            if (selectedComponent.GetType() == typeof(UnitTypeComponent))
            {
                EditorUtility.DisplayDialog("Không thể xóa", 
                    "Không thể xóa UnitTypeComponent vì nó là thành phần bắt buộc.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Xác nhận xóa", 
                $"Bạn có chắc chắn muốn xóa component {selectedComponent.GetType().Name}?", "Xóa", "Hủy"))
            {
                components.Remove(selectedComponent);
                DestroyImmediate(selectedComponent, true);
                EditorUtility.SetDirty(troopPrefab);
            }
        }
    }
}