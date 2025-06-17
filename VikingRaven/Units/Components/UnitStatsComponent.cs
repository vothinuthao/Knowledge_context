using System;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;
using VikingRaven.Units.Models;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Component for displaying unit statistics from UnitModel
    /// Enhanced with Odin Inspector for beautiful data visualization
    /// This is purely for display and debugging purposes
    /// </summary>
    public class UnitStatsComponent : BaseComponent
    {
        #region Dependencies
        
        private UnitModel _unitModel;
        private HealthComponent _healthComponent;
        private CombatComponent _combatComponent;
        
        #endregion
        
        #region Live Stats Overview
        
        [TitleGroup("Live Stats Overview")]
        [InfoBox("Real-time statistics from UnitModel - Read-only display for debugging", InfoMessageType.Info)]
        
        [HorizontalGroup("Live Stats Overview/Row1")]
        [BoxGroup("Live Stats Overview/Row1/Health Status")]
        [LabelText("Current Health"), ProgressBar(0, "MaxHealth", ColorGetter = "GetHealthColor")]
        [ShowInInspector, ReadOnly] 
        private float CurrentHealth => _healthComponent?.CurrentHealth ?? 0f;
        
        [BoxGroup("Live Stats Overview/Row1/Health Status")]
        [LabelText("Max Health"), ReadOnly]
        [ShowInInspector] 
        private float MaxHealth => _unitModel?.MaxHealth ?? 0f;
        
        [BoxGroup("Live Stats Overview/Row1/Health Status")]
        [LabelText("Health %"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetHealthColor")]
        [ShowInInspector] 
        private float HealthPercentage => MaxHealth > 0 ? (CurrentHealth / MaxHealth) * 100f : 0f;
        
        [HorizontalGroup("Live Stats Overview/Row1")]
        [BoxGroup("Live Stats Overview/Row1/Shield Status")]
        [LabelText("Current Shield"), ProgressBar(0, "MaxShield", ColorGetter = "GetShieldColor")]
        [ShowInInspector, ReadOnly] 
        private float CurrentShield => _healthComponent?.CurrentShield ?? 0f;
        
        [BoxGroup("Live Stats Overview/Row1/Shield Status")]
        [LabelText("Max Shield"), ReadOnly]
        [ShowInInspector] 
        private float MaxShield => _unitModel?.MaxShield ?? 0f;
        
        // [HorizontalGroup("Live Stats Overview/Row2")]
        // [BoxGroup("Live Stats Overview/Row2/Combat Stats")]
        // [LabelText("Attack Damage"), ReadOnly]
        // [ShowInInspector] 
        // private float AttackDamage => _unitModel.AttackDamage ?? 0f;
        //
        // [BoxGroup("Live Stats Overview/Row2/Combat Stats")]
        // [LabelText("Attack Range"), ReadOnly]
        // [ShowInInspector] 
        // private float AttackRange => _unitModel?.AttackRange ?? 0f;
        //
        // [BoxGroup("Live Stats Overview/Row2/Combat Stats")]
        // [LabelText("Attack Speed"), ReadOnly]
        // [ShowInInspector] 
        // private float AttackSpeed => _unitModel?.AttackSpeed ?? 0f;
        
        [HorizontalGroup("Live Stats Overview/Row2")]
        [BoxGroup("Live Stats Overview/Row2/Movement")]
        [LabelText("Move Speed"), ReadOnly]
        [ShowInInspector] 
        private float MoveSpeed => _unitModel?.MoveSpeed ?? 0f;
        
        [BoxGroup("Live Stats Overview/Row2/Movement")]
        [LabelText("Detection Range"), ReadOnly]
        [ShowInInspector] 
        private float DetectionRange => _unitModel?.DetectionRange ?? 0f;
        
        #endregion
        
        #region Combat Effectiveness Analysis
        
        [TitleGroup("Combat Effectiveness Analysis")]
        [InfoBox("Calculated effectiveness metrics based on current unit state", InfoMessageType.None)]
        
        [HorizontalGroup("Combat Effectiveness Analysis/EffectivenessRow")]
        [BoxGroup("Combat Effectiveness Analysis/EffectivenessRow/Overall")]
        [LabelText("Health Factor"), ReadOnly, ProgressBar(0, 100)]
        [ShowInInspector] 
        private float HealthFactor => MaxHealth > 0 ? (CurrentHealth / MaxHealth) * 100f : 0f;
        
        [BoxGroup("Combat Effectiveness Analysis/EffectivenessRow/Overall")]
        [LabelText("Combat Readiness"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetReadinessColor")]
        [ShowInInspector] 
        private float CombatReadiness => CalculateCombatReadiness();
        
        [HorizontalGroup("Combat Effectiveness Analysis/EffectivenessRow")]
        [BoxGroup("Combat Effectiveness Analysis/EffectivenessRow/Status")]
        [LabelText("Is Alive"), ReadOnly, ToggleLeft]
        [ShowInInspector] 
        private bool IsAlive => CurrentHealth > 0f;
        
        [BoxGroup("Combat Effectiveness Analysis/EffectivenessRow/Status")]
        [LabelText("Can Attack"), ReadOnly, ToggleLeft]
        [ShowInInspector] 
        private bool CanAttack => IsAlive && _combatComponent.CanAttack();
        
        [BoxGroup("Combat Effectiveness Analysis/EffectivenessRow/Status")]
        [LabelText("In Combat"), ReadOnly, ToggleLeft]
        [ShowInInspector] 
        private bool InCombat => _combatComponent?.IsInCombat == true;
        
        #endregion
        
        #region Unit Information
        
        [TitleGroup("Unit Information")]
        [InfoBox("Basic unit identification and configuration details", InfoMessageType.None)]
        
        [HorizontalGroup("Unit Information/InfoRow")]
        [BoxGroup("Unit Information/InfoRow/Identity")]
        [LabelText("Unit ID"), ReadOnly]
        [ShowInInspector] 
        private uint UnitID => _unitModel?.UnitId ?? 0;
        
        [BoxGroup("Unit Information/InfoRow/Identity")]
        [LabelText("Unit Name"), ReadOnly]
        [ShowInInspector] 
        private string UnitName => _unitModel?.DisplayName ?? "Unknown";
        
        [BoxGroup("Unit Information/InfoRow/Identity")]
        [LabelText("Unit Type"), ReadOnly, EnumToggleButtons]
        [ShowInInspector] 
        private UnitType UnitType => _unitModel?.UnitType ?? UnitType.Infantry;
        
        [HorizontalGroup("Unit Information/InfoRow")]
        [BoxGroup("Unit Information/InfoRow/Runtime")]
        [LabelText("Entity ID"), ReadOnly]
        [ShowInInspector] 
        private int EntityID => Entity?.Id ?? -1;
        
        [BoxGroup("Unit Information/InfoRow/Runtime")]
        [LabelText("Is Initialized"), ReadOnly, ToggleLeft]
        [ShowInInspector] 
        private bool IsInitialized => _unitModel != null;
        
        #endregion
        
        #region Unity Lifecycle

        [Obsolete("Obsolete")]
        public override void Initialize()
        {
            base.Initialize();

            var unitFactory = FindObjectOfType<UnitFactory>();
            if (unitFactory != null)
            {
                _unitModel = unitFactory.GetUnitModel(Entity);
                if (_unitModel == null)
                {
                    Debug.LogWarning($"UnitStatsComponent: UnitModel not found for entity {Entity.Id}");
                }
            }
            _healthComponent = Entity.GetComponent<HealthComponent>();
        }

        #endregion
        
        #region Color Methods for Odin Inspector
        
        private Color GetHealthColor()
        {
            float healthPercent = HealthPercentage;
            if (healthPercent > 75f) return Color.green;
            if (healthPercent > 50f) return Color.yellow;
            if (healthPercent > 25f) return new Color(1f, 0.5f, 0f); // Orange
            return Color.red;
        }
        
        private Color GetShieldColor()
        {
            float shieldPercent = MaxShield > 0 ? (CurrentShield / MaxShield) * 100f : 0f;
            if (shieldPercent > 75f) return Color.cyan;
            if (shieldPercent > 50f) return Color.blue;
            if (shieldPercent > 25f) return new Color(0.5f, 0f, 1f); // Purple
            return Color.gray;
        }
        
        private Color GetReadinessColor()
        {
            float readiness = CombatReadiness;
            if (readiness > 80f) return Color.green;
            if (readiness > 60f) return Color.yellow;
            if (readiness > 40f) return new Color(1f, 0.5f, 0f); // Orange
            return Color.red;
        }
        
        #endregion
        
        #region Calculation Methods
        
        /// <summary>
        /// Calculate overall combat readiness based on health and status
        /// </summary>
        private float CalculateCombatReadiness()
        {
            if (!IsAlive) return 0f;
            
            float readiness = HealthFactor; // Base on health percentage
            
            // Adjust based on shield status
            if (MaxShield > 0)
            {
                float shieldFactor = (CurrentShield / MaxShield) * 20f; // Shield contributes up to 20 points
                readiness = Mathf.Min(100f, readiness + shieldFactor);
            }
            
            // Reduce if can't attack
            if (!CanAttack)
            {
                readiness *= 0.5f;
            }
            
            return readiness;
        }
        
        #endregion
        
        #region Public API (Read-Only Access)
        
        /// <summary>
        /// Get the unit model reference
        /// </summary>
        public UnitModel UnitModel => _unitModel;
        
        /// <summary>
        /// Quick access properties for other systems
        /// </summary>
        public bool IsUnitAlive => IsAlive;
        public bool CanUnitAttack => CanAttack;
        public float GetHealthPercentage() => HealthPercentage;
        public float GetCombatReadiness() => CombatReadiness;
        
        #endregion
    }
}