using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;
using VikingRaven.Units.Models;

namespace VikingRaven.Units.Components
{
    public class UnitInfoComponent : BaseComponent
    {
        #region Primary Data Source (Single Source of Truth)
        
        [TitleGroup("Data Source")]
        [InfoBox("Primary data source - All information comes from UnitModel via BaseEntity", InfoMessageType.Info)]
        [SerializeField, ReadOnly] private UnitModel _unitModel;
        [TitleGroup("Data Source")]
        [InfoBox("Primary data source - All information comes from UnitModel via BaseEntity")]
        [SerializeField, ReadOnly] private BaseEntity _baseEntity;
        
        #endregion
        
        #region Component Management
        
        [TitleGroup("Component Management")]
        [InfoBox("All IComponent references auto-detected from object and children", InfoMessageType.None)]
        [SerializeField, ReadOnly] private List<IComponent> _allComponents = new List<IComponent>();
        
        [TitleGroup("Component Management")]
        [BoxGroup("Component Management/Status")]
        [LabelText("Components Found"), ReadOnly]
        [ShowInInspector]
        private int ComponentsFoundCount => _allComponents.Count;
        
        [BoxGroup("Component Management/Status")]
        [LabelText("Components Registered"), ReadOnly]
        [ShowInInspector]
        private bool ComponentsRegistered => _componentsRegistered;
        
        private bool _componentsRegistered = false;
        
        #endregion
        
        #region Component References (Auto-detected)
        
        [TitleGroup("Component References")]
        [InfoBox("Auto-detected component references for enhanced display", InfoMessageType.None)]
        
        [HorizontalGroup("Component References/Row1")]
        [BoxGroup("Component References/Row1/Core")]
        [SerializeField, ReadOnly] private HealthComponent _healthComponent;
        
        [BoxGroup("Component References/Row1/Core")]
        [SerializeField, ReadOnly] private CombatComponent _combatComponent;
        
        [HorizontalGroup("Component References/Row1")]
        [BoxGroup("Component References/Row1/Extended")]
        [SerializeField, ReadOnly] private WeaponComponent _weaponComponent;
        
        [BoxGroup("Component References/Row1/Extended")]
        [SerializeField, ReadOnly] private FormationComponent _formationComponent;
        
        #endregion
        
        #region Auto-Fill System
        
        [TitleGroup("Auto-Fill Utilities")]
        [HorizontalGroup("Auto-Fill Utilities/Buttons")]
        [Button(ButtonSizes.Large, Name = "🔍 Find All Components")]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void AutoFindComponents()
        {
            int foundCount = 0;
            int warningCount = 0;
            
            _allComponents.Clear();
            
            var allFoundComponents = GetComponentsInChildren<MonoBehaviour>()
                .OfType<IComponent>()
                .Where(c => c != this)
                .ToList();
            
            _allComponents.AddRange(allFoundComponents);
            
            // Find specific component types
            var healthComponents = _allComponents.OfType<HealthComponent>().ToArray();
            if (healthComponents.Length > 1)
            {
                Debug.LogWarning($"⚠️ DUPLICATE WARNING: Found {healthComponents.Length} HealthComponents in hierarchy! Using first one.");
                warningCount++;
            }
            if (healthComponents.Length > 0)
            {
                _healthComponent = healthComponents[0];
                foundCount++;
            }
            
            var combatComponents = _allComponents.OfType<CombatComponent>().ToArray();
            if (combatComponents.Length > 1)
            {
                Debug.LogWarning($"⚠️ DUPLICATE WARNING: Found {combatComponents.Length} CombatComponents in hierarchy! Using first one.");
                warningCount++;
            }
            if (combatComponents.Length > 0)
            {
                _combatComponent = combatComponents[0];
                foundCount++;
            }
            
            var weaponComponents = _allComponents.OfType<WeaponComponent>().ToArray();
            if (weaponComponents.Length > 1)
            {
                Debug.LogWarning($"⚠️ DUPLICATE WARNING: Found {weaponComponents.Length} WeaponComponents in hierarchy! Using first one.");
                warningCount++;
            }
            if (weaponComponents.Length > 0)
            {
                _weaponComponent = weaponComponents[0];
                foundCount++;
            }
            
            var formationComponents = _allComponents.OfType<FormationComponent>().ToArray();
            if (formationComponents.Length > 1)
            {
                warningCount++;
            }
            if (formationComponents.Length > 0)
            {
                _formationComponent = formationComponents[0];
                foundCount++;
            }
            
            RegisterComponentsWithBaseEntity();
        }
        
        [HorizontalGroup("Auto-Fill Utilities/Buttons")]
        [Button(ButtonSizes.Medium, Name = "🗑️ Clear All")]
        [GUIColor(1f, 0.4f, 0.4f)]
        private void ClearAllReferences()
        {
            _healthComponent = null;
            _combatComponent = null;
            _weaponComponent = null;
            _formationComponent = null;
            _allComponents.Clear();
            _componentsRegistered = false;
        }
        
        private void RegisterComponentsWithBaseEntity()
        {
            if (_baseEntity && _allComponents.Count > 0)
            {
                _baseEntity.RegisterComponentsFromUnitInfo(_allComponents);
                _componentsRegistered = true;
            }
        }
        
        #endregion
        
        #region Component Information Display
        
        [TitleGroup("All Components Info")]
        [InfoBox("Complete list of all IComponent found in object hierarchy", InfoMessageType.None)]
        
        [ShowInInspector, ReadOnly]
        [ListDrawerSettings(ShowIndexLabels = true, ShowPaging = false, ShowItemCount = true)]
        private List<ComponentInfo> AllComponentsInfo => _allComponents.Select(c => new ComponentInfo
        {
            Name = c.GetType().Name,
            Type = c.GetType().ToString(),
            GameObject = (c as MonoBehaviour)?.gameObject.name ?? "Unknown",
            IsActive = (c as MonoBehaviour)?.enabled ?? false
        }).ToList();
        
        [System.Serializable]
        private class ComponentInfo
        {
            [ReadOnly] public string Name;
            [ReadOnly] public string Type;
            [ReadOnly] public string GameObject;
            [ReadOnly] public bool IsActive;
        }
        
        #endregion
        
        #region Unit Information Display
        
        [TitleGroup("Unit Information")]
        [InfoBox("Core unit identity and configuration from UnitModel", InfoMessageType.None)]
        
        [HorizontalGroup("Unit Information/Row1")]
        [BoxGroup("Unit Information/Row1/Identity")]
        [LabelText("Unit ID"), ReadOnly]
        [ShowInInspector] 
        private uint UnitID => _unitModel?.UnitId ?? 0;
        
        [BoxGroup("Unit Information/Row1/Identity")]
        [LabelText("Display Name"), ReadOnly]
        [ShowInInspector] 
        private string UnitName => _unitModel?.DisplayName ?? "Unknown Unit";
        
        [BoxGroup("Unit Information/Row1/Identity")]
        [LabelText("Unit Type"), ReadOnly, EnumToggleButtons]
        [ShowInInspector] 
        private UnitType UnitType => _unitModel?.UnitType ?? UnitType.Infantry;
        
        [HorizontalGroup("Unit Information/Row1")]
        [BoxGroup("Unit Information/Row1/Runtime")]
        [LabelText("Entity ID"), ReadOnly]
        [ShowInInspector] 
        private int EntityID => Entity?.Id ?? -1;
        
        [BoxGroup("Unit Information/Row1/Runtime")]
        [LabelText("Is Initialized"), ReadOnly, ToggleLeft]
        [ShowInInspector] 
        private bool IsInitialized => _unitModel != null;
        
        [BoxGroup("Unit Information/Row1/Runtime")]
        [LabelText("Total Components"), ReadOnly]
        [ShowInInspector] 
        private int TotalComponentCount => _allComponents.Count;
        
        #endregion
        
        #region Live Statistics Display
        
        [TitleGroup("Live Statistics")]
        [InfoBox("Real-time unit statistics from UnitModel and components", InfoMessageType.Info)]
        
        [HorizontalGroup("Live Statistics/HealthRow")]
        [BoxGroup("Live Statistics/HealthRow/Health")]
        [LabelText("Current Health"), ProgressBar(0, "MaxHealth", ColorGetter = "GetHealthColor")]
        [ShowInInspector, ReadOnly] 
        private float CurrentHealth => _unitModel?.CurrentHealth ?? 0f;
        
        [BoxGroup("Live Statistics/HealthRow/Health")]
        [LabelText("Max Health"), ReadOnly]
        [ShowInInspector] 
        private float MaxHealth => _unitModel?.MaxHealth ?? 0f;
        
        [BoxGroup("Live Statistics/HealthRow/Health")]
        [LabelText("Health %"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetHealthColor")]
        [ShowInInspector] 
        private float HealthPercentage => MaxHealth > 0 ? (CurrentHealth / MaxHealth) * 100f : 0f;
        
        [HorizontalGroup("Live Statistics/HealthRow")]
        [BoxGroup("Live Statistics/HealthRow/Shield")]
        [LabelText("Current Shield"), ProgressBar(0, "MaxShield", ColorGetter = "GetShieldColor")]
        [ShowInInspector, ReadOnly] 
        private float CurrentShield => _unitModel?.CurrentShield ?? 0f;
        
        [BoxGroup("Live Statistics/HealthRow/Shield")]
        [LabelText("Max Shield"), ReadOnly]
        [ShowInInspector] 
        private float MaxShield => _unitModel?.MaxShield ?? 0f;
        
        [HorizontalGroup("Live Statistics/MovementRow")]
        [BoxGroup("Live Statistics/MovementRow/Movement")]
        [LabelText("Move Speed"), ReadOnly]
        [ShowInInspector] 
        private float MoveSpeed => _unitModel?.MoveSpeed ?? 0f;
        
        [BoxGroup("Live Statistics/MovementRow/Movement")]
        [LabelText("Detection Range"), ReadOnly]
        [ShowInInspector] 
        private float DetectionRange => _unitModel?.DetectionRange ?? 0f;
        
        #endregion
        
        #region Component Status
        
        [TitleGroup("Component Status")]
        [InfoBox("Status of auto-detected components", InfoMessageType.None)]
        
        [HorizontalGroup("Component Status/Row1")]
        [LabelText("Health Component"), ShowInInspector, ReadOnly, GUIColor("GetComponentStatusColor")]
        private bool HealthComponentReady => _healthComponent != null;
        
        [HorizontalGroup("Component Status/Row1")]
        [LabelText("Combat Component"), ShowInInspector, ReadOnly, GUIColor("GetComponentStatusColor")]
        private bool CombatComponentReady => _combatComponent != null;
        
        [HorizontalGroup("Component Status/Row2")]
        [LabelText("Weapon Component"), ShowInInspector, ReadOnly, GUIColor("GetComponentStatusColor")]
        private bool WeaponComponentReady => _weaponComponent != null;
        
        [HorizontalGroup("Component Status/Row2")]
        [LabelText("Formation Component"), ShowInInspector, ReadOnly, GUIColor("GetComponentStatusColor")]
        private bool FormationComponentReady => _formationComponent != null;
        
        [LabelText("All Components Ready"), ShowInInspector, ReadOnly, GUIColor("GetOverallStatusColor")]
        private bool AllComponentsReady => HealthComponentReady && CombatComponentReady && WeaponComponentReady && FormationComponentReady;
        
        #endregion
        
        #region Combat Status Analysis
        
        [TitleGroup("Combat Analysis")]
        [InfoBox("Calculated combat effectiveness based on current state", InfoMessageType.None)]
        
        [HorizontalGroup("Combat Analysis/StatusRow")]
        [BoxGroup("Combat Analysis/StatusRow/Status")]
        [LabelText("Is Alive"), ReadOnly, ToggleLeft]
        [ShowInInspector] 
        private bool IsAlive => CurrentHealth > 0f;
        
        [BoxGroup("Combat Analysis/StatusRow/Status")]
        [LabelText("Can Attack"), ReadOnly, ToggleLeft]
        [ShowInInspector] 
        private bool CanAttack => IsAlive && (_combatComponent?.CanAttack() ?? false);
        
        [BoxGroup("Combat Analysis/StatusRow/Status")]
        [LabelText("In Combat"), ReadOnly, ToggleLeft]
        [ShowInInspector] 
        private bool InCombat => _combatComponent?.IsInCombat ?? false;
        
        [HorizontalGroup("Combat Analysis/StatusRow")]
        [BoxGroup("Combat Analysis/StatusRow/Effectiveness")]
        [LabelText("Combat Readiness"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetReadinessColor")]
        [ShowInInspector] 
        private float CombatReadiness => CalculateCombatReadiness();
        
        #endregion
        
        #region UnitModel Integration
        
        [TitleGroup("UnitModel Integration")]
        [InfoBox("UnitModel data integration and synchronization", InfoMessageType.None)]
        
        [Button(ButtonSizes.Medium, Name = "🔄 Sync with UnitModel")]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void SyncWithUnitModel()
        {
        }
        
        public void SetUnitModel(UnitModel unitModel, BaseEntity baseEntity)
        {
            if (unitModel == null)
            {
                Debug.LogWarning("⚠️ Attempted to set null UnitModel");
                return;
            }
            
            _unitModel = unitModel;
            _baseEntity = baseEntity;
            SyncWithUnitModel();
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        public override void Initialize()
        {
            base.Initialize();
            
            // Only find components if UnitModel is available
            if (_unitModel != null)
            {
                AutoFindComponents();
                ValidateSetup();
                Debug.Log($"🎯 UnitInfoComponent initialized for {_unitModel.DisplayName}");
            }
            else
            {
                Debug.LogWarning("⚠️ UnitInfoComponent initialized without UnitModel. Manual setup required.");
            }
        }
        
        #endregion
        
        #region Validation Methods
        
        private void ValidateSetup()
        {
            if (_unitModel == null)
            {
                Debug.LogError($"❌ SETUP ERROR: UnitModel is null for {gameObject.name}. Component will not function properly.");
                return;
            }
            
            CheckComponentDataSources();
        }
        
        private void CheckComponentDataSources()
        {
            // Validate that components use UnitModel as single source of truth
            if (_healthComponent != null && _healthComponent.GetType().GetField("_independentMaxHealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null)
            {
                Debug.LogWarning($"⚠️ DATA SOURCE WARNING: HealthComponent has independent health values. Should use UnitModel data only.");
            }
            
            if (_combatComponent != null && _combatComponent.GetType().GetField("_independentDamage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null)
            {
                Debug.LogWarning($"⚠️ DATA SOURCE WARNING: CombatComponent has independent damage values. Should use UnitModel data only.");
            }
        }
        
        #endregion
        
        #region Color Methods for Odin Inspector
        
        private Color GetHealthColor()
        {
            float healthPercent = HealthPercentage;
            if (healthPercent > 75f) return Color.green;
            if (healthPercent > 50f) return Color.yellow;
            if (healthPercent > 25f) return new Color(1f, 0.5f, 0f);
            return Color.red;
        }
        
        private Color GetShieldColor()
        {
            float shieldPercent = MaxShield > 0 ? (CurrentShield / MaxShield) * 100f : 0f;
            if (shieldPercent > 75f) return Color.cyan;
            if (shieldPercent > 50f) return Color.blue;
            if (shieldPercent > 25f) return new Color(0.5f, 0f, 1f);
            return Color.gray;
        }
        
        private Color GetReadinessColor()
        {
            float readiness = CombatReadiness;
            if (readiness > 80f) return Color.green;
            if (readiness > 60f) return Color.yellow;
            if (readiness > 40f) return new Color(1f, 0.5f, 0f);
            return Color.red;
        }
        
        private Color GetComponentStatusColor()
        {
            return true ? Color.green : Color.red;
        }
        
        private Color GetOverallStatusColor()
        {
            return AllComponentsReady ? Color.green : Color.red;
        }
        
        #endregion
        
        #region Calculation Methods
        
        private float CalculateCombatReadiness()
        {
            if (!IsAlive) return 0f;
            
            float readiness = HealthPercentage;
            
            if (MaxShield > 0)
            {
                float shieldFactor = (CurrentShield / MaxShield) * 20f;
                readiness = Mathf.Min(100f, readiness + shieldFactor);
            }
            
            if (!CanAttack)
            {
                readiness *= 0.5f;
            }
            
            return readiness;
        }
        
        #endregion
        
        #region Public API (Read-Only Access)
        
        public UnitModel UnitModel => _unitModel;
        public bool IsUnitAlive => IsAlive;
        public bool CanUnitAttack => CanAttack;
        public float GetHealthPercentage() => HealthPercentage;
        public float GetCombatReadiness() => CombatReadiness;
        public UnitType GetUnitType() => UnitType;
        public List<IComponent> GetAllComponents() => new List<IComponent>(_allComponents);
        
        public HealthComponent HealthComponent => _healthComponent;
        public CombatComponent CombatComponent => _combatComponent;
        public WeaponComponent WeaponComponent => _weaponComponent;
        public FormationComponent FormationComponent => _formationComponent;
        
        #endregion
    }
}