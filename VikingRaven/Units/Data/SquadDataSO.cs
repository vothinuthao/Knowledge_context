using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Data
{
    /// <summary>
    /// ScriptableObject defining squad composition and properties
    /// Fixed Odin Inspector group conflicts
    /// </summary>
    [CreateAssetMenu(fileName = "NewSquadData", menuName = "VikingRaven/Squad Data SO")]
    public class SquadDataSO : SerializedScriptableObject
    {
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
        
        [FoldoutGroup("Squad Composition")]
        [Tooltip("Units in this squad and their quantities")]
        [TableList(ShowIndexLabels = true)]
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
        [Range(0, 1), PropertyRange(0, 1)]
        [SerializeField] private float _aggressionLevel = 0.5f;
        
        [FoldoutGroup("Tactical Settings")]
        [Tooltip("Default cohesion level (0-1)")]
        [Range(0, 1), PropertyRange(0, 1)]
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
        
        [FoldoutGroup("Tags and Categories")]
        [Tooltip("Tags for categorizing squads")]
        [SerializeField]
        private List<string> _squadTags = new List<string>();
        
        [FoldoutGroup("Tags and Categories")]
        [Tooltip("Faction that this squad belongs to")]
        [SerializeField, ValueDropdown("GetAvailableFactions")]
        private string _faction = "Player";

        // Properties
        public uint SquadId => _squadId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
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
        public IReadOnlyList<string> SquadTags => _squadTags;
        public string Faction => _faction;

        // Helper for dropdown menu
        private string[] GetAvailableFactions()
        {
            return new string[] { "Player", "Enemy", "Neutral", "Mercenary", "Rebel" };
        }

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
        /// Get unit type counts
        /// </summary>
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
        /// Validate squad composition
        /// </summary>
        [Button("Validate Squad Composition"), FoldoutGroup("Squad Composition")]
        private void ValidateSquadComposition()
        {
            bool hasIssues = false;
            
            // Check for empty compositions
            for (int i = _unitCompositions.Count - 1; i >= 0; i--)
            {
                if (_unitCompositions[i].UnitData == null || _unitCompositions[i].Count <= 0)
                {
                    Debug.LogWarning($"Squad {_displayName}: Removing invalid composition at index {i}");
                    _unitCompositions.RemoveAt(i);
                    hasIssues = true;
                }
            }
            
            if (hasIssues)
            {
                Debug.LogWarning($"Squad {_displayName} has composition issues that were fixed");
            }
            else
            {
                Debug.Log($"Squad {_displayName} composition is valid");
            }
        }

        /// <summary>
        /// Calculate optimal costs based on unit composition
        /// </summary>
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
        
        [Tooltip("Position in formation")]
        [EnumToggleButtons]
        public FormationPosition FormationPosition = FormationPosition.Auto;
    }
    
    /// <summary>
    /// Position in formation
    /// </summary>
    public enum FormationPosition
    {
        Auto,
        Front,
        Middle,
        Back,
        Left,
        Right
    }
    
    /// <summary>
    /// Attack strategy for squads
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