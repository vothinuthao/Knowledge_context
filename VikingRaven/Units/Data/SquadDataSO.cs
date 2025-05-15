using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Data
{
    /// <summary>
    /// ScriptableObject defining squad composition and properties
    /// </summary>
    [CreateAssetMenu(fileName = "NewSquadData", menuName = "VikingRaven/Squad Data SO")]
    public class SquadDataSO : SerializedScriptableObject
    {
        [FoldoutGroup("Basic Information")]
        [Tooltip("Unique identifier for this squad type")]
        [SerializeField] private string _squadId;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Display name of the squad")]
        [SerializeField] private string _displayName;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Description of the squad")]
        [TextArea(3, 5)]
        [SerializeField] private string _description;
        
        [FoldoutGroup("Squad Composition")]
        [Tooltip("Units in this squad and their quantities")]
        [TableList]
        [SerializeField] private List<UnitComposition> _unitCompositions = new List<UnitComposition>();
        
        [FoldoutGroup("Formation Settings")]
        [Tooltip("Default formation type for this squad")]
        [SerializeField, EnumPaging]
        private FormationType _defaultFormationType = FormationType.Line;
        
        [FoldoutGroup("Formation Settings")]
        [Tooltip("Special formation types available to this squad")]
        [SerializeField, EnumToggleButtons]
        private List<FormationType> _availableFormations = new List<FormationType>();
        
        [FoldoutGroup("Tactical Settings")]
        [Tooltip("Default aggression level (0-1)")]
        [Range(0, 1)]
        [SerializeField] private float _aggressionLevel = 0.5f;
        
        [FoldoutGroup("Tactical Settings")]
        [Tooltip("Default cohesion level (0-1)")]
        [Range(0, 1)]
        [SerializeField] private float _cohesionLevel = 0.7f;
        
        [FoldoutGroup("Tactical Settings")]
        [Tooltip("Default spacing between units")]
        [SerializeField, Range(0.5f, 2.0f)]
        private float _spacingMultiplier = 1.0f;
        
        [FoldoutGroup("Tactical Settings")]
        [Tooltip("Preferred attack strategy")]
        [SerializeField, EnumToggleButtons]
        private AttackStrategy _preferredAttackStrategy = AttackStrategy.Balanced;
        
        [FoldoutGroup("Economy")]
        [Tooltip("Gold cost to create this squad")]
        [SerializeField, Range(50, 1000)]
        private int _goldCost = 100;
        
        [FoldoutGroup("Economy")]
        [Tooltip("Food cost to maintain this squad")]
        [SerializeField, Range(1, 100)]
        private int _foodUpkeep = 10;
        
        [FoldoutGroup("Economy")]
        [Tooltip("Time to train this squad (in seconds)")]
        [SerializeField, Range(5, 120)]
        private float _trainingTime = 30f;

        // Properties for accessing data
        public string SquadId => _squadId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public IReadOnlyList<UnitComposition> UnitCompositions => _unitCompositions;
        public FormationType DefaultFormationType => _defaultFormationType;
        public IReadOnlyList<FormationType> AvailableFormations => _availableFormations;
        public float AggressionLevel => _aggressionLevel;
        public float CohesionLevel => _cohesionLevel;
        public float SpacingMultiplier => _spacingMultiplier;
        public AttackStrategy PreferredAttackStrategy => _preferredAttackStrategy;
        public int GoldCost => _goldCost;
        public int FoodUpkeep => _foodUpkeep;
        public float TrainingTime => _trainingTime;

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
        /// Total gold cost of the squad
        /// </summary>
        [ShowInInspector, ReadOnly]
        [FoldoutGroup("Squad Statistics")]
        public int TotalGoldCost
        {
            get
            {
                int total = _goldCost;
                foreach (var composition in _unitCompositions)
                {
                    if (composition.UnitData != null)
                    {
                        // Add the cost of each unit
                        total += 10; // A base cost per unit - this could be enhanced
                    }
                }
                return total;
            }
        }
        
        /// <summary>
        /// Calculate the combined strength of the squad
        /// </summary>
        [ShowInInspector, ReadOnly]
        [FoldoutGroup("Squad Statistics"), ProgressBar(0, 100, ColorMember = "GetStrengthColor")]
        public float SquadStrength
        {
            get
            {
                float strength = 0;
                foreach (var composition in _unitCompositions)
                {
                    if (composition.UnitData != null)
                    {
                        // Calculate strength based on unit stats
                        float unitStrength = composition.UnitData.HitPoints * 0.1f +
                                            composition.UnitData.Damage * 0.3f +
                                            composition.UnitData.DamageRanged * 0.3f +
                                            composition.UnitData.Shield * 0.2f;
                                            
                        strength += unitStrength * composition.Count;
                    }
                }
                return Mathf.Min(strength, 100f);
            }
        }
        
        // Odin helper for progress bar color
        private Color GetStrengthColor
        {
            get
            {
                float strength = SquadStrength;
                if (strength < 30) return Color.red;
                if (strength < 60) return Color.yellow;
                return Color.green;
            }
        }

        /// <summary>
        /// Create a dictionary mapping unit types to counts
        /// </summary>
        /// <returns>Dictionary with unit type as key and count as value</returns>
        public Dictionary<UnitType, int> GetUnitTypeCounts()
        {
            Dictionary<UnitType, int> counts = new Dictionary<UnitType, int>();
            
            foreach (var composition in _unitCompositions)
            {
                if (composition.UnitData != null)
                {
                    UnitType type = composition.UnitData.UnitType;
                    if (!counts.ContainsKey(type))
                    {
                        counts[type] = 0;
                    }
                    counts[type] += composition.Count;
                }
            }
            
            return counts;
        }
        
        /// <summary>
        /// Get list of unit data with their counts
        /// </summary>
        public List<(UnitDataSO UnitData, int Count)> GetUnitDataWithCounts()
        {
            List<(UnitDataSO UnitData, int Count)> result = new List<(UnitDataSO UnitData, int Count)>();
            
            foreach (var composition in _unitCompositions)
            {
                if (composition.UnitData != null && composition.Count > 0)
                {
                    result.Add((composition.UnitData, composition.Count));
                }
            }
            
            return result;
        }

        /// <summary>
        /// Create a clone of this SquadData
        /// </summary>
        /// <returns>A new instance with copied values</returns>
        public SquadDataSO Clone()
        {
            var clone = CreateInstance<SquadDataSO>();
            
            // Copy basic information
            clone._squadId = this._squadId;
            clone._displayName = this._displayName;
            clone._description = this._description;
            
            // Copy unit compositions
            clone._unitCompositions = new List<UnitComposition>();
            foreach (var composition in this._unitCompositions)
            {
                clone._unitCompositions.Add(new UnitComposition
                {
                    UnitData = composition.UnitData,
                    Count = composition.Count
                });
            }
            
            // Copy formation settings
            clone._defaultFormationType = this._defaultFormationType;
            clone._availableFormations = new List<FormationType>(this._availableFormations);
            
            // Copy tactical settings
            clone._aggressionLevel = this._aggressionLevel;
            clone._cohesionLevel = this._cohesionLevel;
            clone._spacingMultiplier = this._spacingMultiplier;
            clone._preferredAttackStrategy = this._preferredAttackStrategy;
            
            // Copy economy settings
            clone._goldCost = this._goldCost;
            clone._foodUpkeep = this._foodUpkeep;
            clone._trainingTime = this._trainingTime;
            
            return clone;
        }
        
        [Button("Fill Available Formations Based on Units"), FoldoutGroup("Formation Settings")]
        private void AutoFillAvailableFormations()
        {
            _availableFormations.Clear();
            
            // Basic formations available to all
            _availableFormations.Add(FormationType.Line);
            _availableFormations.Add(FormationType.Column);
            
            // Check unit types to determine special formations
            bool hasInfantry = false;
            bool hasArchers = false;
            bool hasPikes = false;
            
            foreach (var composition in _unitCompositions)
            {
                if (composition.UnitData != null)
                {
                    switch (composition.UnitData.UnitType)
                    {
                        case UnitType.Infantry:
                            hasInfantry = true;
                            break;
                        case UnitType.Archer:
                            hasArchers = true;
                            break;
                        case UnitType.Pike:
                            hasPikes = true;
                            break;
                    }
                }
            }
            
            // Add special formations based on unit types
            if (hasInfantry)
            {
                _availableFormations.Add(FormationType.Testudo);
            }
            
            if (hasPikes)
            {
                _availableFormations.Add(FormationType.Phalanx);
            }
            
            if (hasArchers || (hasInfantry && hasPikes))
            {
                _availableFormations.Add(FormationType.Circle);
            }
            
            // Always add Normal formation
            if (!_availableFormations.Contains(FormationType.Normal))
            {
                _availableFormations.Add(FormationType.Normal);
            }
        }
        
        [Button("Calculate Optimal Costs"), FoldoutGroup("Economy")]
        private void CalculateOptimalCosts()
        {
            int baseCost = 50;
            int unitCost = 0;
            int foodCost = 5;
            
            foreach (var composition in _unitCompositions)
            {
                if (composition.UnitData != null)
                {
                    // Calculate cost based on unit stats
                    float unitStrength = composition.UnitData.HitPoints * 0.1f +
                                        composition.UnitData.Damage * 0.2f +
                                        composition.UnitData.DamageRanged * 0.3f +
                                        composition.UnitData.Shield * 0.2f;
                                        
                    unitCost += Mathf.RoundToInt(unitStrength * 0.5f) * composition.Count;
                    foodCost += Mathf.RoundToInt(composition.UnitData.HitPoints * 0.02f) * composition.Count;
                }
            }
            
            _goldCost = baseCost + unitCost;
            _foodUpkeep = Mathf.Max(5, foodCost);
            _trainingTime = Mathf.Min(120, 10 + TotalUnitCount * 2);
        }
    }
    
    /// <summary>
    /// Represents a specific unit type and its count within a squad
    /// </summary>
    [Serializable]
    public class UnitComposition
    {
        [Tooltip("Unit data reference"), Required, PreviewField(60)]
        public UnitDataSO UnitData;
        
        [Tooltip("Number of units of this type"), Range(1, 20)]
        public int Count = 1;
        
        [ShowInInspector, ReadOnly]
        public float TotalStrength => UnitData != null ? 
            (UnitData.HitPoints * 0.1f + UnitData.Damage * 0.3f + UnitData.DamageRanged * 0.3f + UnitData.Shield * 0.2f) * Count : 0;
    }
    
    /// <summary>
    /// Enum defining different attack strategies for squads
    /// </summary>
    public enum AttackStrategy
    {
        Defensive,
        Balanced,
        Aggressive,
        Flanking,
        Ambush
    }
}