using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Data
{
    [CreateAssetMenu(fileName = "NewSquadData", menuName = "VikingRaven/Squad Data SO")]
    public class SquadDataSO : SerializedScriptableObject
    {
        #region Basic Information
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Unique identifier for this squad type")]
        [SerializeField] private uint _squadId;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Display name of the squad")]
        [SerializeField] private string _displayName;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Description of the squad")]
        [TextArea(3, 5)]
        [SerializeField] private string _description;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Squad icon for UI")]
        [SerializeField, PreviewField(80)]
        private Sprite _icon;

        #endregion

        #region Squad Composition
        
        [FoldoutGroup("Squad Composition")]
        [Tooltip("Units in this squad and their quantities")]
        [TableList(ShowIndexLabels = true)]
        [SerializeField] private List<UnitComposition> _unitCompositions = new List<UnitComposition>();

        #endregion

        #region Formation Configuration
        
        [FoldoutGroup("Formation Configuration")]
        [Title("Formation Settings")]
        [InfoBox("Configure formation behavior for this squad. Both spacing multiplier and config are used for complete formation control.", 
                 InfoMessageType.Info)]
        
        [Tooltip("Default formation type for this squad")]
        [SerializeField, EnumPaging]
        private FormationType _defaultFormationType = FormationType.Normal;
        
        [Tooltip("Formation spacing configuration (REQUIRED for new formation system)")]
        [SerializeField, AssetsOnly]
        [InfoBox("@_formationSpacingConfig == null", "Formation Spacing Config is recommended for enhanced formation control.")]
        private FormationSpacingConfig _formationSpacingConfig;
        
        [FoldoutGroup("Formation Configuration")]
        [Title("Legacy Compatibility")]
        [InfoBox("SpacingMultiplier is maintained for SquadModel compatibility. New system uses FormationSpacingConfig.", 
                 InfoMessageType.Info)]
        
        [Tooltip("Spacing multiplier for formation positions (REQUIRED for SquadModel)")]
        [SerializeField, Range(0.5f, 3f)]
        [SuffixLabel("x multiplier")]
        private float _spacingMultiplier = 1.0f;
        
        [Tooltip("Override formation spacing using custom multiplier")]
        [SerializeField, ToggleLeft]
        private bool _useCustomSpacing = false;
        
        [Tooltip("Additional custom spacing multiplier (applied on top of base multiplier)")]
        [SerializeField, Range(0.5f, 2f), ShowIf("_useCustomSpacing")]
        [SuffixLabel("x additional")]
        private float _customSpacingMultiplier = 1.0f;

        #endregion

        #region Tactical Settings
        
        [FoldoutGroup("Tactical Settings")]
        [Tooltip("Default aggression level (0-1)")]
        [Range(0, 1), PropertyRange(0, 1)]
        [SerializeField] private float _aggressionLevel = 0.5f;
        
        [FoldoutGroup("Tactical Settings")]
        [Tooltip("Default cohesion level (0-1)")]
        [Range(0, 1), PropertyRange(0, 1)]
        [SerializeField] private float _cohesionLevel = 0.7f;

        #endregion

        #region Economy
        
        [FoldoutGroup("Economy")]
        [Tooltip("Gold cost to create this squad")]
        [SerializeField, Range(50, 1000), PropertyRange(50, 1000), SuffixLabel("gold")]
        private int _goldCost = 100;
        
        [FoldoutGroup("Economy")]
        [Tooltip("Food upkeep to maintain this squad")]
        [SerializeField, Range(1, 100), PropertyRange(1, 100), SuffixLabel("food/min")]
        private int _foodUpkeep = 10;
        
        [FoldoutGroup("Economy")]
        [Tooltip("Time to train this squad (in seconds)")]
        [SerializeField, Range(5, 120), SuffixLabel("seconds")]
        private float _trainingTime = 30f;

        #endregion

        #region Properties (FIXED: Added missing SpacingMultiplier)

        public uint SquadId => _squadId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public IReadOnlyList<UnitComposition> UnitCompositions => _unitCompositions;
        public FormationType DefaultFormationType => _defaultFormationType;
        public FormationSpacingConfig FormationSpacingConfig => _formationSpacingConfig;
        
        /// <summary>
        /// FIXED: SpacingMultiplier property for SquadModel compatibility
        /// This is required by SquadModel constructor
        /// </summary>
        public float SpacingMultiplier => GetEffectiveSpacingMultiplier();
        
        public float AggressionLevel => _aggressionLevel;
        public float CohesionLevel => _cohesionLevel;
        public int GoldCost => _goldCost;
        public int FoodUpkeep => _foodUpkeep;
        public float TrainingTime => _trainingTime;

        #endregion

        #region Formation Helper Methods (ENHANCED)

        /// <summary>
        /// Get effective spacing multiplier combining base and custom values
        /// </summary>
        private float GetEffectiveSpacingMultiplier()
        {
            float baseMultiplier = _spacingMultiplier;
            
            if (_useCustomSpacing)
            {
                return baseMultiplier * _customSpacingMultiplier;
            }
            
            return baseMultiplier;
        }
        
        /// <summary>
        /// Get formation spacing for this squad using new FormationSpacingConfig
        /// Falls back to legacy spacing calculation if config is not available
        /// </summary>
        public float GetFormationSpacing(FormationType formationType)
        {
            // Method 1: Use new FormationSpacingConfig if available
            if (_formationSpacingConfig != null)
            {
                float baseSpacing = _formationSpacingConfig.GetSpacing(formationType);
                return baseSpacing * GetEffectiveSpacingMultiplier();
            }
            
            // Method 2: Fallback to legacy spacing calculation
            float legacyBaseSpacing = GetLegacyFormationSpacing(formationType);
            return legacyBaseSpacing * GetEffectiveSpacingMultiplier();
        }
        
        /// <summary>
        /// Legacy formation spacing calculation for backward compatibility
        /// </summary>
        private float GetLegacyFormationSpacing(FormationType formationType)
        {
            return formationType switch
            {
                FormationType.Normal => 2.5f,
                FormationType.Phalanx => 1.8f,
                FormationType.Testudo => 1.2f,
                _ => 2.0f
            };
        }
        
        /// <summary>
        /// Get position tolerance for formation
        /// Uses config if available, otherwise uses default
        /// </summary>
        public float GetPositionTolerance()
        {
            if (_formationSpacingConfig != null)
            {
                return _formationSpacingConfig.PositionTolerance;
            }
            
            return 0.3f; // Legacy default value
        }
        
        /// <summary>
        /// Check if formation spacing config is available
        /// </summary>
        public bool HasValidFormationConfig()
        {
            return _formationSpacingConfig != null;
        }
        
        /// <summary>
        /// Get formation debug color if config is available
        /// </summary>
        public Color GetFormationDebugColor(FormationType formationType)
        {
            if (_formationSpacingConfig != null)
            {
                return _formationSpacingConfig.GetFormationDebugColor(formationType);
            }
            
            // Legacy color mapping
            return formationType switch
            {
                FormationType.Normal => Color.green,
                FormationType.Phalanx => Color.red,
                FormationType.Testudo => Color.blue,
                _ => Color.white
            };
        }

        #endregion

        #region Squad Statistics

        /// <summary>
        /// Get total unit count in this squad
        /// </summary>
        [ShowInInspector, ReadOnly]
        [FoldoutGroup("Squad Statistics")]
        public int TotalUnitCount
        {
            get
            {
                int total = 0;
                foreach (var composition in _unitCompositions)
                {
                    total += composition.Count;
                }
                return total;
            }
        }
        
        /// <summary>
        /// Get ideal formation grid size for this squad
        /// </summary>
        [ShowInInspector, ReadOnly]
        [FoldoutGroup("Squad Statistics")]
        public string IdealFormationSize
        {
            get
            {
                int totalUnits = TotalUnitCount;
                if (totalUnits <= 9)
                {
                    return "3x3 Grid (Perfect fit)";
                }
                else
                {
                    int width = Mathf.CeilToInt(Mathf.Sqrt(totalUnits));
                    int height = Mathf.CeilToInt((float)totalUnits / width);
                    return $"{width}x{height} Grid ({totalUnits} units)";
                }
            }
        }
        
        /// <summary>
        /// Get effective spacing for default formation
        /// </summary>
        [ShowInInspector, ReadOnly]
        [FoldoutGroup("Squad Statistics")]
        public float EffectiveFormationSpacing
        {
            get
            {
                return GetFormationSpacing(_defaultFormationType);
            }
        }

        #endregion
    }
    [Serializable]
    public class UnitComposition
    {
        [Tooltip("Unit data reference"), Required, PreviewField(60)]
        public UnitDataSO UnitData;
        
        [Tooltip("Number of units of this type"), Range(1, 20)]
        public int Count = 1;
        
        [Tooltip("Position preference in formation")]
        [EnumToggleButtons]
        public FormationPosition FormationPosition = FormationPosition.Auto;
    }
    
}