using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Units.Components;
using VikingRaven.Units.Models;

namespace VikingRaven.Core.ECS
{
    public class BaseEntity : MonoBehaviour, IEntity
    {
        [SerializeField] private int _id;
        [SerializeField] private bool _isActive = true;
        [SerializeField] private bool _isRegistered = false;
        
        private readonly Dictionary<Type, IComponent> _components = new Dictionary<Type, IComponent>();
        private UnitModel _unitModel;
        private UnitInfoComponent _unitInfoComponent;
        private bool _isComponentsInitialized = false;
        
        #region Entity Properties
        
        [TitleGroup("Entity Information")]
        [BoxGroup("Entity Information/Core")]
        [LabelText("Entity ID"), ReadOnly]
        [ShowInInspector]
        public int Id => _id;
        
        [BoxGroup("Entity Information/Core")]
        [LabelText("Is Active"), ReadOnly]
        [ShowInInspector]
        public bool IsActive { get => _isActive; set => _isActive = value; }
        
        [BoxGroup("Entity Information/Core")]
        [LabelText("Is Registered"), ReadOnly]
        [ShowInInspector]
        private bool IsRegistered => _isRegistered;
        
        [BoxGroup("Entity Information/Core")]
        [LabelText("Components Initialized"), ReadOnly]
        [ShowInInspector]
        private bool ComponentsInitialized => _isComponentsInitialized;
        
        [BoxGroup("Entity Information/Core")]
        [LabelText("Has Unit Model"), ReadOnly]
        [ShowInInspector]
        private bool HasUnitModel => _unitModel != null;
        
        [BoxGroup("Entity Information/Core")]
        [LabelText("Unit Model Name"), ReadOnly]
        [ShowInInspector]
        private string UnitModelName => _unitModel?.DisplayName ?? "No Unit Model";
        
        #endregion
        
        #region Component Dictionary Display
        
        [TitleGroup("Component Management")]
        [InfoBox("Real-time view of all registered IComponents in this entity", InfoMessageType.Info)]
        
        [BoxGroup("Component Management/Stats")]
        [LabelText("Total Components"), ReadOnly]
        [ShowInInspector]
        private int ComponentCount => _components.Count;
        
        [BoxGroup("Component Management/Stats")]
        [LabelText("UnitInfo Available"), ReadOnly]
        [ShowInInspector]
        private bool HasUnitInfo => _unitInfoComponent != null;
        
        [TitleGroup("Component Management/Components List")]
        [InfoBox("Detailed view of all components with their status and information", InfoMessageType.None)]
        [ShowInInspector, ReadOnly]
        [ListDrawerSettings(
            ShowIndexLabels = true, 
            ShowPaging = false, 
            ShowItemCount = true,
            Expanded = true,
            DraggableItems = false
        )]
        [PropertyOrder(1)]
        private List<ComponentDisplayInfo> ComponentsDisplay => GetComponentsDisplayList();
        
        [System.Serializable]
        private class ComponentDisplayInfo
        {
            [HorizontalGroup("Info")]
            [LabelText("Component Name"), ReadOnly]
            [GUIColor("GetComponentTypeColor")]
            public string ComponentName;
            
            [HorizontalGroup("Info")]
            [LabelText("Type"), ReadOnly]
            [PropertyTooltip("Full type name of the component")]
            public string ComponentType;
            
            [HorizontalGroup("Status")]
            [LabelText("GameObject"), ReadOnly]
            [PropertyTooltip("GameObject that owns this component")]
            public string GameObjectName;
            
            [HorizontalGroup("Status")]
            [LabelText("Active"), ReadOnly]
            [GUIColor("GetActiveStatusColor")]
            public bool IsActive;
            
            [HorizontalGroup("Status")]
            [LabelText("Entity Set"), ReadOnly]
            [PropertyTooltip("Whether this component has its Entity reference set")]
            [GUIColor("GetEntityStatusColor")]
            public bool HasEntityReference;
            
            [HideInInspector]
            public IComponent ComponentReference;
            
            private Color GetComponentTypeColor()
            {
                if (ComponentName.Contains("Health")) return Color.red;
                if (ComponentName.Contains("Combat")) return new Color(1f, 0.5f, 0f); // Orange
                if (ComponentName.Contains("Weapon")) return Color.yellow;
                if (ComponentName.Contains("Formation")) return Color.cyan;
                if (ComponentName.Contains("UnitInfo")) return Color.green;
                return Color.white;
            }
            
            private Color GetActiveStatusColor()
            {
                return IsActive ? Color.green : Color.red;
            }
            
            private Color GetEntityStatusColor()
            {
                return HasEntityReference ? Color.green : Color.red;
            }
        }
        
        #endregion
        
        #region Component Management Utilities
        
        [TitleGroup("Component Management/Actions")]
        [HorizontalGroup("Component Management/Actions/Buttons")]
        [Button(ButtonSizes.Medium, Name = "🔄 Refresh Display")]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void RefreshComponentDisplay()
        {
            Debug.Log($"🔄 Refreshed component display for Entity {Id}. Found {_components.Count} components");
        }
        
        [HorizontalGroup("Component Management/Actions/Buttons")]
        [Button(ButtonSizes.Medium, Name = "📊 Log Components")]
        [GUIColor(0.8f, 0.8f, 0.4f)]
        private void LogAllComponents()
        {
            Debug.Log($"📊 === Component Report for Entity {Id} ===");
            Debug.Log($"📊 Total Components: {_components.Count}");
            
            foreach (var kvp in _components)
            {
                var comp = kvp.Value;
                var monoBehaviour = comp as MonoBehaviour;
                var isActive = monoBehaviour?.enabled ?? false;
                var gameObjectName = monoBehaviour?.gameObject.name ?? "Unknown";
                
                Debug.Log($"📦 {kvp.Key.Name} | GameObject: {gameObjectName} | Active: {isActive} | HasEntity: {comp.Entity != null}");
            }
            
            Debug.Log($"📊 === End Component Report ===");
        }
        
        [HorizontalGroup("Component Management/Actions/Buttons")]
        [Button(ButtonSizes.Medium, Name = "🔧 Initialize UnitInfo")]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void ManualInitializeUnitInfo()
        {
            if (_unitInfoComponent != null && _unitModel != null)
            {
                InitializeUnitInfoComponent();
                Debug.Log("🔧 Manual UnitInfo initialization completed");
            }
            else
            {
                Debug.LogWarning("⚠️ Cannot initialize: UnitInfoComponent or UnitModel is null");
            }
        }
        
        #endregion
        
        #region Component Type Summary
        
        [TitleGroup("Component Management/Summary")]
        [InfoBox("Summary of component types currently registered", InfoMessageType.None)]
        [ShowInInspector, ReadOnly]
        [DictionaryDrawerSettings(
            KeyLabel = "Component Type",
            ValueLabel = "Instance",
            DisplayMode = DictionaryDisplayOptions.OneLine
        )]
        private Dictionary<string, string> ComponentTypeSummary => GetComponentTypeSummary();
        
        #endregion
        
        #region Unity Lifecycle
        
        public void Awake()
        {
            RegisterEntity();
            FindUnitInfoComponent();
        }
        
        #endregion
        
        #region Core Methods
        
        public void SetUnitModel(UnitModel unitModel)
        {
            if (unitModel == null) 
            {
                Debug.LogWarning($"⚠️ Attempted to set null UnitModel for Entity {Id}");
                return;
            }
            _unitModel = unitModel;
            InitializeComponents();
        }
        
        public void RegisterComponentsFromUnitInfo(List<IComponent> components)
        {
            if (components == null || components.Count == 0)
            {
                return;
            }
            
            int addedCount = 0;
            int skippedCount = 0;
            
            foreach (var component in components)
            {
                if (RegisterSingleComponent(component))
                {
                    addedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void RegisterEntity()
        {
            if (_id > 0 && !_isRegistered)
            {
                if (EntityRegistry.HasInstance)
                {
                    _isRegistered = true;
                }
            }
        }
        
        private void FindUnitInfoComponent()
        {
            _unitInfoComponent = GetComponent<UnitInfoComponent>();
            
            if (_unitInfoComponent == null)
            {
                _unitInfoComponent = GetComponentInChildren<UnitInfoComponent>();
            }
            
            if (_unitInfoComponent != null)
            {
                Debug.Log($"🔍 Found UnitInfoComponent for Entity {Id}");
            }
            else
            {
                Debug.LogWarning($"⚠️ UnitInfoComponent not found for Entity {Id}");
            }
        }
        
        private void InitializeComponents()
        {
            if (_unitInfoComponent != null && _unitModel != null && !_isComponentsInitialized)
            {
                InitializeUnitInfoComponent();
                _isComponentsInitialized = true;
            }
        }
        
        private void InitializeUnitInfoComponent()
        {
            try
            {
                _unitInfoComponent.SetUnitModel(_unitModel,this);
                _unitInfoComponent.Initialize();
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to initialize UnitInfoComponent for Entity {Id}: {ex.Message}");
            }
        }
        
        private bool RegisterSingleComponent(IComponent component)
        {
            if (component == null) return false;
            var componentType = component.GetType();
            if (!_components.TryAdd(componentType, component))
            {
                return false;
            }
            component.Entity = this;
            return true;
        }
        
        private List<ComponentDisplayInfo> GetComponentsDisplayList()
        {
            var displayList = new List<ComponentDisplayInfo>();
            
            foreach (var kvp in _components)
            {
                var comp = kvp.Value;
                var monoBehaviour = comp as MonoBehaviour;
                
                var displayInfo = new ComponentDisplayInfo
                {
                    ComponentName = kvp.Key.Name,
                    ComponentType = kvp.Key.ToString(),
                    GameObjectName = monoBehaviour?.gameObject.name ?? "Unknown",
                    IsActive = monoBehaviour?.enabled ?? false,
                    HasEntityReference = comp.Entity != null,
                    ComponentReference = comp
                };
                
                displayList.Add(displayInfo);
            }
            
            return displayList.OrderBy(x => x.ComponentName).ToList();
        }
        
        private Dictionary<string, string> GetComponentTypeSummary()
        {
            var summary = new Dictionary<string, string>();
            
            foreach (var kvp in _components)
            {
                var componentName = kvp.Key.Name;
                var monoBehaviour = kvp.Value as MonoBehaviour;
                var status = monoBehaviour?.enabled == true ? "Active" : "Inactive";
                
                summary[componentName] = status;
            }
            
            return summary;
        }
        
        #endregion
        
        #region Public API
        
        [Obsolete("Obsolete")]
        public void SetId(int id)
        {
            if (_id <= 0 || !_isRegistered)
            {
                _id = id;
                if (!_isRegistered && EntityRegistry.HasInstance)
                {
                    EntityRegistry.Instance.RegisterEntity(this);
                    _isRegistered = true;
                }
            }
        }

        public new T GetComponent<T>() where T : class, IComponent
        {
            if (_components.TryGetValue(typeof(T), out var component))
            {
                return component as T;
            }
            return null;
        }

        public T GetComponentBehavior<T>() where T : class
        {
            if (_components.TryGetValue(typeof(T), out var component))
            {
                return component as T;
            }
            return null;
        }

        public bool HasComponent<T>() where T : class, IComponent
        {
            return _components.ContainsKey(typeof(T));
        }

        public void AddComponent(IComponent component)
        {
            if (component == null)
            {
                return;
            }
            var type = component.GetType();
            
            if (!_components.TryAdd(type, component))
            {
                return;
            }
            component.Entity = this;
            component.Initialize();
        }
        public void RemoveComponent<T>() where T : class, IComponent
        {
            var type = typeof(T);
            
            if (!_components.TryGetValue(type, out var component))
            {
                return;
            }
            component.Cleanup();
            component.Entity = null;
            _components.Remove(type);
        }
        
        
        public UnitInfoComponent GetUnitInfoComponent()
        {
            return _unitInfoComponent;
        }
        
        public UnitModel GetUnitModel()
        {
            return _unitModel;
        }
        
        public Dictionary<Type, IComponent> GetComponentsDictionary()
        {
            return new Dictionary<Type, IComponent>(_components);
        }

        #endregion
        
        #region Cleanup
        
        public void OnDestroy()
        {
            CleanupComponents();
        }
        
        private void CleanupComponents()
        {
            foreach (var component in _components.Values)
            {
                if (component != null)
                {
                    component.Cleanup();
                }
            }
            
            _components.Clear();
            Debug.Log($"🧹 Cleaned up all components for Entity {Id}");
        }
        
        #endregion
    }
}