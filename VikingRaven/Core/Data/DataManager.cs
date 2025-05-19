using System.Collections.Generic;
using Core.Utils;
using UnityEngine;
using VikingRaven.Core.Utils;
using VikingRaven.Units.Data;
using VikingRaven.Units.Models;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.Data
{
    /// <summary>
    /// Trung tâm quản lý và lưu trữ tất cả dữ liệu game
    /// </summary>
    public class DataManager : PureSingleton<DataManager>
    {
        private Dictionary<string, UnitDataSO> _unitDataCache = new Dictionary<string, UnitDataSO>();
        private Dictionary<string, SquadDataSO> _squadDataCache = new Dictionary<string, SquadDataSO>();

        private string UNIT_LOADING_PATH = "Units";
        private string SQUAD_LOADING_PATH = "Squads";
        private bool _isInitialized = false;
        
        public bool IsInitialized => _isInitialized;
        
        public override void OnInitialize()
        {
            base.OnInitialize();
            LoadAllData();
        }
        
        private void LoadAllData()
        {
            Debug.Log("DataManager: Loading all data...");
            LoadAllUnitData();
            LoadAllSquadData();
            _isInitialized = true;
            Debug.Log($"DataManager: Data loaded successfully. {_unitDataCache.Count} unit types, {_squadDataCache.Count} squad types.");
        }
        
        /// <summary>
        /// Load tất cả UnitDataSO
        /// </summary>
        private void LoadAllUnitData()
        {
            _unitDataCache.Clear();
            UnitDataSO[] unitDataArray = Resources.LoadAll<UnitDataSO>(UNIT_LOADING_PATH);
            
            foreach (var unitData in unitDataArray)
            {
                if (!string.IsNullOrEmpty(unitData.UnitId))
                {
                    _unitDataCache[unitData.UnitId] = unitData;
                }
                else
                {
                    Debug.LogWarning($"DataManager: UnitData with empty ID found: {unitData.name}");
                }
            }
            
            Debug.Log($"DataManager: Loaded {_unitDataCache.Count} unit data types.");
        }
        
        private void LoadAllSquadData()
        {
            _squadDataCache.Clear();
            SquadDataSO[] squadDataArray = Resources.LoadAll<SquadDataSO>(SQUAD_LOADING_PATH);
            
            foreach (var squadData in squadDataArray)
            {
                if (!string.IsNullOrEmpty(squadData.SquadId))
                {
                    _squadDataCache[squadData.SquadId] = squadData;
                }
                else
                {
                    Debug.LogWarning($"DataManager: SquadData with empty ID found: {squadData.name}");
                }
            }
            
            Debug.Log($"DataManager: Loaded {_squadDataCache.Count} squad data types.");
        }
        public UnitDataSO GetUnitData(string unitId)
        {
            if (_unitDataCache.TryGetValue(unitId, out UnitDataSO unitData))
            {
                return unitData;
            }
            
            Debug.LogWarning($"DataManager: UnitData with ID {unitId} not found!");
            return null;
        }
        
        public SquadDataSO GetSquadData(string squadId)
        {
            if (_squadDataCache.TryGetValue(squadId, out SquadDataSO squadData))
            {
                return squadData;
            }
            
            Debug.LogWarning($"DataManager: SquadData with ID {squadId} not found!");
            return null;
        }
        
        public List<UnitDataSO> GetAllUnitData()
        {
            return new List<UnitDataSO>(_unitDataCache.Values);
        }
        
        public List<SquadDataSO> GetAllSquadData()
        {
            return new List<SquadDataSO>(_squadDataCache.Values);
        }
        
        public List<UnitDataSO> GetUnitDataByType(UnitType unitType)
        {
            List<UnitDataSO> result = new List<UnitDataSO>();
            
            foreach (var unitData in _unitDataCache.Values)
            {
                if (unitData.UnitType == unitType)
                {
                    result.Add(unitData);
                }
            }
            
            return result;
        }
        
        public List<SquadDataSO> GetSquadDataByFaction(string faction)
        {
            List<SquadDataSO> result = new List<SquadDataSO>();
            
            foreach (var squadData in _squadDataCache.Values)
            {
                if (squadData.Faction == faction)
                {
                    result.Add(squadData);
                }
            }
            
            return result;
        }
    }
}