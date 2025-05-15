using System.Collections.Generic;
using Core.Utils;
using UnityEngine;
using VikingRaven.Core.Data;
using VikingRaven.Core.ECS;
using VikingRaven.Game;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;

namespace VikingRaven.EnhanceFactory
{
    /// <summary>
    /// Factory responsible for creating and managing squads
    /// </summary>
    public class SquadFactory : Singleton<SquadFactory>
    {
        [SerializeField] private int _nextSquadId = 1;
        [SerializeField] private UnitFactory _unitFactory;
        
        // Map of active squads by ID
        private Dictionary<int, SquadInfo> _activeSquads = new Dictionary<int, SquadInfo>();
        
        // Reference to other necessary components
        // private UnitFactory UnitFactory => GameManager.Instance.UnitFactory;
        private UnitFactory UnitFactory => _unitFactory;
        private DataManager DataManager => DataManager.Instance;
        
        // Class to store information about a squad
        private class SquadInfo
        {
            public int SquadId;
            public SquadDataSO Data;
            public List<IEntity> Members = new List<IEntity>();
            public FormationType CurrentFormation;
            public Vector3 CurrentPosition;
            public Quaternion CurrentRotation;
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            Debug.Log("SquadFactory initialized as singleton");
        }
        
        /// <summary>
        /// Create a squad based on SquadData
        /// </summary>
        /// <param name="squadData">Data defining the squad composition</param>
        /// <param name="position">Position in world space</param>
        /// <param name="rotation">Rotation in world space</param>
        /// <returns>List of created unit entities</returns>
        public List<IEntity> CreateSquadFromData(SquadDataSO squadData, Vector3 position, Quaternion rotation)
        {
            if (squadData == null)
            {
                Debug.LogError("SquadFactory: Cannot create squad - squadData is null");
                return new List<IEntity>();
            }
            
            int squadId = _nextSquadId++;
            List<IEntity> squadMembers = new List<IEntity>();
            
            // Get the UnitFactory
            if (UnitFactory == null)
            {
                Debug.LogError("SquadFactory: Cannot create squad - UnitFactory is null");
                return squadMembers;
            }
            
            // Create all units according to composition
            int slotIndex = 0;
            foreach (var composition in squadData.UnitCompositions)
            {
                if (composition.UnitData == null || composition.Count <= 0)
                    continue;
                    
                for (int i = 0; i < composition.Count; i++)
                {
                    // Slight offset to avoid units spawning at exactly the same position
                    Vector3 spawnOffset = new Vector3(
                        UnityEngine.Random.Range(-0.5f, 0.5f),
                        0,
                        UnityEngine.Random.Range(-0.5f, 0.5f)
                    );
                    
                    // Create unit from data
                    IEntity unitEntity = UnitFactory.CreateUnitFromData(
                        composition.UnitData,
                        position + spawnOffset,
                        rotation
                    );
                    
                    if (unitEntity != null)
                    {
                        // Set up formation component
                        var formationComponent = unitEntity.GetComponent<FormationComponent>();
                        if (formationComponent != null)
                        {
                            formationComponent.SetSquadId(squadId);
                            formationComponent.SetFormationSlot(slotIndex++);
                            formationComponent.SetFormationType(squadData.DefaultFormationType);
                        }
                        
                        // Add to members list
                        squadMembers.Add(unitEntity);
                    }
                }
            }
            
            // Store information about this squad
            if (squadMembers.Count > 0)
            {
                var squadInfo = new SquadInfo
                {
                    SquadId = squadId,
                    Data = squadData,
                    Members = new List<IEntity>(squadMembers),
                    CurrentFormation = squadData.DefaultFormationType,
                    CurrentPosition = position,
                    CurrentRotation = rotation
                };
                
                _activeSquads[squadId] = squadInfo;
                
                Debug.Log($"SquadFactory: Created squad ID {squadId} with {squadMembers.Count} units based on {squadData.DisplayName}");
            }
            
            return squadMembers;
        }
        
        /// <summary>
        /// Create a squad with a single unit type
        /// </summary>
        /// <param name="unitType">Type of unit</param>
        /// <param name="unitCount">Number of units to create</param>
        /// <param name="position">Position in world space</param>
        /// <param name="rotation">Rotation in world space</param>
        /// <returns>List of created unit entities</returns>
        public List<IEntity> CreateSquad(UnitType unitType, int unitCount, Vector3 position, Quaternion rotation)
        {
            int squadId = _nextSquadId++;
            List<IEntity> squadMembers = new List<IEntity>();
            
            if (UnitFactory == null)
            {
                Debug.LogError("SquadFactory: Cannot create squad - UnitFactory is null");
                return squadMembers;
            }
            
            // Create units
            for (int i = 0; i < unitCount; i++)
            {
                // Add a small random offset to prevent spawning at exactly the same position
                Vector3 spawnOffset = new Vector3(
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    0,
                    UnityEngine.Random.Range(-0.5f, 0.5f)
                );
                
                IEntity unitEntity = UnitFactory.CreateUnit(unitType, position + spawnOffset, rotation);
                
                if (unitEntity != null)
                {
                    var formationComponent = unitEntity.GetComponent<FormationComponent>();
                    if (formationComponent != null)
                    {
                        formationComponent.SetSquadId(squadId);
                        formationComponent.SetFormationSlot(i);
                        formationComponent.SetFormationType(FormationType.Line); // Default formation
                    }
                    
                    squadMembers.Add(unitEntity);
                }
            }
            
            // Store information about this squad
            if (squadMembers.Count > 0)
            {
                // Try to find an appropriate squad data to associate with this squad
                SquadDataSO matchingSquadData = null;
                if (DataManager != null && DataManager.IsInitialized)
                {
                    // Get all squad data
                    List<SquadDataSO> allSquadData = DataManager.GetAllSquadData();
                    
                    // Find the first one that only uses this unit type
                    foreach (var data in allSquadData)
                    {
                        bool isMatch = true;
                        foreach (var composition in data.UnitCompositions)
                        {
                            if (composition.UnitData != null && composition.UnitData.UnitType != unitType)
                            {
                                isMatch = false;
                                break;
                            }
                        }
                        
                        if (isMatch)
                        {
                            matchingSquadData = data;
                            break;
                        }
                    }
                }
                
                var squadInfo = new SquadInfo
                {
                    SquadId = squadId,
                    Data = matchingSquadData, // May be null
                    Members = new List<IEntity>(squadMembers),
                    CurrentFormation = FormationType.Line,
                    CurrentPosition = position,
                    CurrentRotation = rotation
                };
                
                _activeSquads[squadId] = squadInfo;
                
                Debug.Log($"SquadFactory: Created squad ID {squadId} with {squadMembers.Count} units of type {unitType}");
            }
            
            return squadMembers;
        }
        
        /// <summary>
        /// Create a mixed squad with different unit types
        /// </summary>
        /// <param name="unitCounts">Dictionary mapping unit types to counts</param>
        /// <param name="position">Position in world space</param>
        /// <param name="rotation">Rotation in world space</param>
        /// <returns>List of created unit entities</returns>
        public List<IEntity> CreateMixedSquad(Dictionary<UnitType, int> unitCounts, Vector3 position, Quaternion rotation)
        {
            int squadId = _nextSquadId++;
            List<IEntity> squadMembers = new List<IEntity>();
            
            if (UnitFactory == null)
            {
                Debug.LogError("SquadFactory: Cannot create squad - UnitFactory is null");
                return squadMembers;
            }
            
            int slotIndex = 0;
            foreach (var kvp in unitCounts)
            {
                UnitType unitType = kvp.Key;
                int count = kvp.Value;
                
                for (int i = 0; i < count; i++)
                {
                    // Add a small random offset
                    Vector3 spawnOffset = new Vector3(
                        UnityEngine.Random.Range(-0.5f, 0.5f),
                        0,
                        UnityEngine.Random.Range(-0.5f, 0.5f)
                    );
                    
                    IEntity unitEntity = UnitFactory.CreateUnit(unitType, position + spawnOffset, rotation);
                    
                    if (unitEntity != null)
                    {
                        var formationComponent = unitEntity.GetComponent<FormationComponent>();
                        if (formationComponent != null)
                        {
                            formationComponent.SetSquadId(squadId);
                            formationComponent.SetFormationSlot(slotIndex++);
                            formationComponent.SetFormationType(FormationType.Line); // Default formation
                        }
                        
                        squadMembers.Add(unitEntity);
                    }
                }
            }
            
            // Store information about this squad
            if (squadMembers.Count > 0)
            {
                // Try to find a matching squad data
                SquadDataSO matchingSquadData = null;
                if (DataManager != null && DataManager.IsInitialized)
                {
                    // Get all squad data
                    List<SquadDataSO> allSquadData = DataManager.GetAllSquadData();
                    
                    // Compare composition
                    foreach (var data in allSquadData)
                    {
                        // Count units by type in the data
                        Dictionary<UnitType, int> dataUnitCounts = new Dictionary<UnitType, int>();
                        foreach (var composition in data.UnitCompositions)
                        {
                            if (composition.UnitData != null)
                            {
                                UnitType type = composition.UnitData.UnitType;
                                if (!dataUnitCounts.ContainsKey(type))
                                {
                                    dataUnitCounts[type] = 0;
                                }
                                dataUnitCounts[type] += composition.Count;
                            }
                        }
                        
                        // Compare with requested counts
                        bool isMatch = true;
                        foreach (var unitType in unitCounts.Keys)
                        {
                            if (!dataUnitCounts.TryGetValue(unitType, out int count) || count != unitCounts[unitType])
                            {
                                isMatch = false;
                                break;
                            }
                        }
                        
                        if (isMatch)
                        {
                            matchingSquadData = data;
                            break;
                        }
                    }
                }
                
                var squadInfo = new SquadInfo
                {
                    SquadId = squadId,
                    Data = matchingSquadData, // May be null
                    Members = new List<IEntity>(squadMembers),
                    CurrentFormation = FormationType.Line,
                    CurrentPosition = position,
                    CurrentRotation = rotation
                };
                
                _activeSquads[squadId] = squadInfo;
                
                Debug.Log($"SquadFactory: Created mixed squad ID {squadId} with {squadMembers.Count} units");
            }
            
            return squadMembers;
        }
        
        /// <summary>
        /// Create a named squad by ID from DataManager
        /// </summary>
        /// <param name="squadDataId">ID of the squad data in DataManager</param>
        /// <param name="position">Position in world space</param>
        /// <param name="rotation">Rotation in world space</param>
        /// <returns>List of created unit entities</returns>
        public List<IEntity> CreateNamedSquad(string squadDataId, Vector3 position, Quaternion rotation)
        {
            if (DataManager == null || !DataManager.IsInitialized)
            {
                Debug.LogError("SquadFactory: Cannot create named squad - DataManager is not initialized");
                return new List<IEntity>();
            }
            
            SquadDataSO squadData = DataManager.GetSquadData(squadDataId);
            if (squadData == null)
            {
                Debug.LogError($"SquadFactory: Cannot create named squad - SquadData not found for ID: {squadDataId}");
                return new List<IEntity>();
            }
            
            return CreateSquadFromData(squadData, position, rotation);
        }
        
        /// <summary>
        /// Return a squad to the pool (return all its units)
        /// </summary>
        /// <param name="squadId">ID of the squad to destroy</param>
        public void DestroySquad(int squadId)
        {
            if (_activeSquads.TryGetValue(squadId, out SquadInfo squadInfo))
            {
                // Return all units to the factory
                if (UnitFactory != null)
                {
                    foreach (var unit in squadInfo.Members)
                    {
                        UnitFactory.ReturnUnit(unit);
                    }
                }
                
                // Remove from active squads
                _activeSquads.Remove(squadId);
                
                Debug.Log($"SquadFactory: Destroyed squad ID {squadId}");
            }
        }
        
        /// <summary>
        /// Destroy all active squads
        /// </summary>
        public void DestroyAllSquads()
        {
            List<int> squadIds = new List<int>(_activeSquads.Keys);
            foreach (int squadId in squadIds)
            {
                DestroySquad(squadId);
            }
            
            _activeSquads.Clear();
            Debug.Log("SquadFactory: Destroyed all squads");
        }
        
        /// <summary>
        /// Get a list of units in a squad
        /// </summary>
        /// <param name="squadId">ID of the squad</param>
        /// <returns>List of unit entities</returns>
        public List<IEntity> GetSquadMembers(int squadId)
        {
            if (_activeSquads.TryGetValue(squadId, out SquadInfo squadInfo))
            {
                return new List<IEntity>(squadInfo.Members);
            }
            
            return new List<IEntity>();
        }
        
        /// <summary>
        /// Get all active squad IDs
        /// </summary>
        /// <returns>List of squad IDs</returns>
        public List<int> GetAllSquadIds()
        {
            return new List<int>(_activeSquads.Keys);
        }
        
        /// <summary>
        /// Get information about a squad
        /// </summary>
        /// <param name="squadId">ID of the squad</param>
        /// <returns>Squad data and current formation</returns>
        public (SquadDataSO Data, FormationType Formation) GetSquadInfo(int squadId)
        {
            if (_activeSquads.TryGetValue(squadId, out SquadInfo squadInfo))
            {
                return (squadInfo.Data, squadInfo.CurrentFormation);
            }
            
            return (null, FormationType.None);
        }
        
        /// <summary>
        /// Update the formation type of a squad
        /// </summary>
        /// <param name="squadId">ID of the squad</param>
        /// <param name="formationType">New formation type</param>
        public void UpdateSquadFormation(int squadId, FormationType formationType)
        {
            if (_activeSquads.TryGetValue(squadId, out SquadInfo squadInfo))
            {
                squadInfo.CurrentFormation = formationType;
                
                // Update the formation component of all units
                foreach (var unit in squadInfo.Members)
                {
                    var formationComponent = unit.GetComponent<FormationComponent>();
                    if (formationComponent != null)
                    {
                        formationComponent.SetFormationType(formationType);
                    }
                }
                
                Debug.Log($"SquadFactory: Updated squad {squadId} formation to {formationType}");
            }
        }
        
        /// <summary>
        /// Get total count of active squads
        /// </summary>
        public int ActiveSquadCount => _activeSquads.Count;
        
        /// <summary>
        /// Get total count of units across all squads
        /// </summary>
        public int TotalUnitCount
        {
            get
            {
                int count = 0;
                foreach (var squad in _activeSquads.Values)
                {
                    count += squad.Members.Count;
                }
                return count;
            }
        }
    }
}