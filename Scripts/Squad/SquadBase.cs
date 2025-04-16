using System.Collections.Generic;
using Core;
using Troops.Base;
using Troops.Config;
using UnityEngine;

namespace Squad
{
    /// <summary>
    /// Model class for Squad - contains all data but no game logic
    /// </summary>
    public class SquadBase
    {
        // Squad information
        public string SquadName { get; private set; }
        public GameDefineData.SquadType SquadType { get; private set; }
        public GameDefineData.Formation.FormationType FormationType { get; private set; }
        public int MaxTroops { get; private set; }
        public Color SquadColor { get; private set; }
        
        // Troop settings
        public float TroopSpacing { get; private set; }
        public TroopConfigSO TroopConfig { get; private set; }
        
        // Behavior settings
        public float SquadCohesionStrength { get; private set; }
        public float MoveSpeed { get; private set; }
        
        // Visual settings
        public GameObject SquadBannerPrefab { get; private set; }
        
        // Runtime data
        private List<TroopBase> _troops = new List<TroopBase>();
        
        /// <summary>
        /// Constructor to initialize from a ScriptableObject config
        /// </summary>
        public SquadBase(SquadConfigSO config)
        {
            // Copy all data from config to this model
            SquadName = config.squadName;
            SquadType = config.squadType;
            FormationType = config.formationType;
            MaxTroops = config.maxTroops;
            SquadColor = config.squadColor;
            TroopSpacing = config.troopSpacing;
            TroopConfig = config.troopConfig; // Reference only, not modified
            SquadCohesionStrength = config.squadCohesionStrength;
            MoveSpeed = config.moveSpeed;
            SquadBannerPrefab = config.squadBannerPrefab;
        }
        
        /// <summary>
        /// Add a troop to the squad
        /// </summary>
        public void AddTroop(TroopBase troop)
        {
            if (!_troops.Contains(troop))
            {
                _troops.Add(troop);
            }
        }
        
        /// <summary>
        /// Remove a troop from the squad
        /// </summary>
        public void RemoveTroop(TroopBase troop)
        {
            if (_troops.Contains(troop))
            {
                _troops.Remove(troop);
            }
        }
        
        /// <summary>
        /// Get all troops in the squad
        /// </summary>
        public List<TroopBase> GetTroops()
        {
            return new List<TroopBase>(_troops); // Return a copy to prevent external modification
        }
        
        /// <summary>
        /// Check if the squad contains a specific troop
        /// </summary>
        public bool ContainsTroop(TroopBase troop)
        {
            return _troops.Contains(troop);
        }
        
        /// <summary>
        /// Get the count of troops in the squad
        /// </summary>
        public int TroopCount => _troops.Count;
        
        /// <summary>
        /// Check if the squad is at full capacity
        /// </summary>
        public bool IsAtFullCapacity => _troops.Count >= MaxTroops;
    }
}