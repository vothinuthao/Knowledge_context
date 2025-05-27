using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Data
{
    /// <summary>
    /// Enhanced SquadDataSO with integrated FormationSpacingConfig
    /// FIXED: Added SpacingMultiplier property for SquadModel compatibility
    /// Maintains backward compatibility while adding new formation system
    /// </summary>
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

        #region Validation and Debug Tools

        [Button("Validate Squad Configuration"), FoldoutGroup("Debug Tools")]
        private void ValidateSquadConfiguration()
        {
            bool hasIssues = false;
            
            // Check spacing multiplier
            if (_spacingMultiplier <= 0)
            {
                Debug.LogError($"Squad {_displayName}: Spacing multiplier must be greater than 0!");
                hasIssues = true;
            }
            
            // Check formation config recommendation
            if (_formationSpacingConfig == null)
            {
                Debug.LogWarning($"Squad {_displayName}: Formation Spacing Config is recommended for enhanced formation control.");
            }
            
            // Check unit compositions
            for (int i = _unitCompositions.Count - 1; i >= 0; i--)
            {
                if (_unitCompositions[i].UnitData == null || _unitCompositions[i].Count <= 0)
                {
                    Debug.LogWarning($"Squad {_displayName}: Removing invalid composition at index {i}");
                    _unitCompositions.RemoveAt(i);
                    hasIssues = true;
                }
            }
            
            // Check total units vs formation efficiency
            int totalUnits = TotalUnitCount;
            if (totalUnits > 9 && _defaultFormationType == FormationType.Normal)
            {
                Debug.LogWarning($"Squad {_displayName}: {totalUnits} units may not fit well in Normal (3x3) formation. Consider Phalanx or Testudo.");
            }
            
            // Check spacing values
            float effectiveSpacing = GetFormationSpacing(_defaultFormationType);
            if (effectiveSpacing < 1.0f)
            {
                Debug.LogWarning($"Squad {_displayName}: Effective formation spacing ({effectiveSpacing:F2}) is very tight. Units may overlap.");
            }
            
            if (!hasIssues)
            {
                Debug.Log($"Squad {_displayName}: Configuration is valid");
            }
        }
        
        [Button("Auto-Setup Formation Config"), FoldoutGroup("Debug Tools")]
        [InfoBox("This will try to find and assign a FormationSpacingConfig, and set optimal spacing values")]
        private void AutoSetupFormationConfig()
        {
            bool configChanged = false;
            
            // Try to find formation config if not assigned
            if (_formationSpacingConfig == null)
            {
                #if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:FormationSpacingConfig");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    _formationSpacingConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<FormationSpacingConfig>(path);
                    configChanged = true;
                    
                    if (_formationSpacingConfig != null)
                    {
                        Debug.Log($"Squad {_displayName}: Auto-assigned formation config: {_formationSpacingConfig.name}");
                    }
                }
                #endif
            }
            
            // Set optimal spacing multiplier based on unit count
            int unitCount = TotalUnitCount;
            if (unitCount <= 4)
            {
                _spacingMultiplier = 1.2f; // More space for small squads
            }
            else if (unitCount <= 9)
            {
                _spacingMultiplier = 1.0f; // Standard spacing for 3x3
            }
            else
            {
                _spacingMultiplier = 0.9f; // Tighter spacing for large squads
            }
            
            configChanged = true;
            
            if (configChanged)
            {
                Debug.Log($"Squad {_displayName}: Auto-setup completed. Spacing multiplier: {_spacingMultiplier}");
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }
        
        [Button("Test All Formation Spacings"), FoldoutGroup("Debug Tools")]
        private void TestAllFormationSpacings()
        {
            Debug.Log($"=== Formation Spacing Test for {_displayName} ===");
            Debug.Log($"Base Spacing Multiplier: {_spacingMultiplier}");
            Debug.Log($"Effective Spacing Multiplier: {GetEffectiveSpacingMultiplier()}");
            Debug.Log($"Has Formation Config: {HasValidFormationConfig()}");
            Debug.Log($"Unit Count: {TotalUnitCount}");
            Debug.Log($"Default Formation: {_defaultFormationType}");
            
            Debug.Log("--- Formation Spacing Results ---");
            Debug.Log($"Normal Formation: {GetFormationSpacing(FormationType.Normal):F2} units");
            Debug.Log($"Phalanx Formation: {GetFormationSpacing(FormationType.Phalanx):F2} units");
            Debug.Log($"Testudo Formation: {GetFormationSpacing(FormationType.Testudo):F2} units");
            Debug.Log($"Position Tolerance: {GetPositionTolerance():F2} units");
        }
        
        [Button("Reset to Optimal Values"), FoldoutGroup("Debug Tools")]
        private void ResetToOptimalValues()
        {
            // Reset formation settings to optimal values
            _defaultFormationType = FormationType.Normal;
            _spacingMultiplier = 1.0f;
            _useCustomSpacing = false;
            _customSpacingMultiplier = 1.0f;
            
            // Reset tactical settings
            _aggressionLevel = 0.5f;
            _cohesionLevel = 0.7f;
            
            Debug.Log($"Squad {_displayName}: Reset to optimal values");
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        #endregion
    }
    
    /// <summary>
    /// Unit composition data - unchanged from original
    /// </summary>
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