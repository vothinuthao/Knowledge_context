using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using VikingRaven.Core.ECS;

namespace Editor
{
    /// <summary>
    /// Cửa sổ cấu hình System - cho phép chỉnh sửa và quản lý các System trong game
    /// </summary>
    public class SystemConfigWindow : OdinEditorWindow
    {
        [MenuItem("VikingRaven/Công cụ/Cấu hình System")]
        private static void OpenWindow()
        {
            GetWindow<SystemConfigWindow>().Show();
        }

        [Serializable]
        public class SystemConfig
        {
            public string SystemName;
            public bool IsActive;
            public int Priority;
            public Dictionary<string, object> Parameters = new Dictionary<string, object>();
        }

        [Serializable]
        public class SystemsConfigData
        {
            public List<SystemConfig> Systems = new List<SystemConfig>();
        }

        [TabGroup("Danh sách System")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "GetSystemLabel")]
        [InlineEditor]
        [LabelText("Danh sách các System")]
        public List<BaseSystem> systems = new List<BaseSystem>();

        private string GetSystemLabel(BaseSystem system)
        {
            if (system == null) return "Null";
            return $"{system.GetType().Name} (Priority: {system.Priority})";
        }

        [TabGroup("Cấu hình tự động")]
        [LabelText("Cân bằng độ ưu tiên")]
        [InfoBox("Cài đặt độ ưu tiên tự động cho các system để đảm bảo thứ tự thực thi hợp lý")]
        public bool autoBalancePriority = true;

        [TabGroup("Cấu hình tự động")]
        [LabelText("Cấu hình mặc định")]
        [InfoBox("Đặt các thông số mặc định cho các loại system")]
        public bool useDefaultConfig = true;
        
        [TabGroup("Cấu hình tự động")]
        [Button("Tự động cấu hình System", ButtonSizes.Large)]
        [GUIColor(0, 0.8f, 0)]
        private void AutoConfigureSystems()
        {
            if (systems.Count == 0)
            {
                EditorUtility.DisplayDialog("Không tìm thấy System", "Không có system nào trong scene. Vui lòng nhấn nút Refresh để cập nhật.", "OK");
                return;
            }

            // Cấu hình độ ưu tiên các system
            if (autoBalancePriority)
            {
                // Movement cần thực thi sớm
                SetPriorityForSystemType("MovementSystem", 10);
                SetPriorityForSystemType("SquadCoordinationSystem", 20);
                SetPriorityForSystemType("FormationSystem", 30);
                SetPriorityForSystemType("SteeringSystem", 40);
                SetPriorityForSystemType("AggroDetectionSystem", 50);
                SetPriorityForSystemType("AIDecisionSystem", 60);
                SetPriorityForSystemType("CombatSystem", 70);
                SetPriorityForSystemType("WeightedBehaviorSystem", 80);
                SetPriorityForSystemType("TacticalAnalysisSystem", 90);
            }

            // Cấu hình cụ thể cho từng loại system
            if (useDefaultConfig)
            {
                foreach (var system in systems)
                {
                    ConfigureSystemParameters(system);
                }
            }

            EditorUtility.SetDirty(systems[0].gameObject);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Cấu hình tự động hoàn tất", "Các system đã được cấu hình tự động.", "OK");
        }

        private void SetPriorityForSystemType(string systemTypeName, int priority)
        {
            foreach (var system in systems)
            {
                if (system.GetType().Name == systemTypeName)
                {
                    system.Priority = priority;
                    break;
                }
            }
        }

        private void ConfigureSystemParameters(BaseSystem system)
        {
            string typeName = system.GetType().Name;
            
            // Cấu hình tùy chỉnh cho từng loại system
            switch (typeName)
            {
                case "AIDecisionSystem":
                    SetFieldValue(system, "_decisionUpdateInterval", 0.5f);
                    break;
                
                case "AggroDetectionSystem":
                    // Có thể cấu hình AggroDetectionSystem
                    break;
                
                case "FormationSystem":
                    // Cấu hình FormationSystem
                    break;
                
                case "MovementSystem":
                    // Cấu hình MovementSystem
                    break;
                
                case "TacticalAnalysisSystem":
                    SetFieldValue(system, "_combatEvaluationInterval", 2.0f);
                    break;
                
                // Thêm cấu hình cho các system khác nếu cần
            }
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
            if (string.IsNullOrEmpty(configFilePath))
            {
                configFilePath = EditorUtility.SaveFilePanel("Lưu cấu hình System", "", "SystemConfig", "json");
                if (string.IsNullOrEmpty(configFilePath)) return;
            }

            SystemsConfigData configData = new SystemsConfigData();

            foreach (var system in systems)
            {
                SystemConfig config = new SystemConfig
                {
                    SystemName = system.GetType().Name,
                    IsActive = system.IsActive,
                    Priority = system.Priority
                };

                // Lấy các thuộc tính có thể serialize
                foreach (var field in system.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                    {
                        var value = field.GetValue(system);
                        if (value != null && IsSerializable(value.GetType()))
                        {
                            config.Parameters[field.Name] = value;
                        }
                    }
                }

                configData.Systems.Add(config);
            }

            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(configData, Formatting.Indented));
            EditorUtility.DisplayDialog("Lưu cấu hình", "Cấu hình System đã được lưu thành công.", "OK");
        }

        [TabGroup("Lưu/Tải")]
        [Button("Tải cấu hình")]
        [GUIColor(1, 0.5f, 0)]
        private void LoadConfiguration()
        {
            if (string.IsNullOrEmpty(configFilePath) || !File.Exists(configFilePath))
            {
                configFilePath = EditorUtility.OpenFilePanel("Tải cấu hình System", "", "json");
                if (string.IsNullOrEmpty(configFilePath) || !File.Exists(configFilePath)) return;
            }

            string json = File.ReadAllText(configFilePath);
            SystemsConfigData configData = JsonConvert.DeserializeObject<SystemsConfigData>(json);

            foreach (var config in configData.Systems)
            {
                var system = systems.FirstOrDefault(s => s.GetType().Name == config.SystemName);
                if (system != null)
                {
                    system.IsActive = config.IsActive;
                    system.Priority = config.Priority;

                    // Áp dụng các tham số
                    foreach (var param in config.Parameters)
                    {
                        var field = system.GetType().GetField(param.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field != null)
                        {
                            try
                            {
                                var value = Convert.ChangeType(param.Value, field.FieldType);
                                field.SetValue(system, value);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Lỗi khi đặt giá trị cho {param.Key} trên {config.SystemName}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            EditorUtility.SetDirty(systems[0].gameObject);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Tải cấu hình", "Cấu hình System đã được tải thành công.", "OK");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshSystemsList();
        }

        [Button("Refresh Danh sách System")]
        [TabGroup("Danh sách System")]
        private void RefreshSystemsList()
        {
            systems.Clear();
            systems.AddRange(FindObjectsOfType<BaseSystem>());
        }

        private bool IsSerializable(Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type == typeof(Vector2) || 
                   type == typeof(Vector3) || type == typeof(Quaternion) || type == typeof(Color) ||
                   type.IsEnum || type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }
    }
}