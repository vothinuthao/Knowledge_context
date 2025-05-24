using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Core.Utils;
using VikingRaven.Core.Data;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Data;
using VikingRaven.Units.Models;

namespace VikingRaven.Core.Factory
{
    /// <summary>
    /// Simplified Squad Factory - Only creates squads by ID
    /// Follows Single Responsibility Principle
    /// </summary>
    public class SquadFactory : Singleton<SquadFactory>
    {
        #region Dependencies
        
        [Title("Dependencies")]
        [Tooltip("Unit factory reference")]
        [SerializeField, Required]
        private UnitFactory _unitFactory;
        
        [Tooltip("Data manager reference")]
        [SerializeField, Required]
        private DataManager _dataManager;

        #endregion

        #region Runtime Data
        
        [Title("Runtime Information")]
        [ShowInInspector, ReadOnly]
        private int ActiveSquadsCount => _activeSquads?.Count ?? 0;
        
        [ShowInInspector, ReadOnly]
        private int NextSquadId => _nextSquadId;

        #endregion

        #region Private Fields
        
        // Active squads tracking
        private Dictionary<int, SquadModel> _activeSquads = new Dictionary<int, SquadModel>();
        
        // Squad ID generation
        private int _nextSquadId = 1;

        #endregion

        #region Events
        
        public event System.Action<SquadModel> OnSquadCreated;
        public event System.Action<SquadModel> OnSquadDisbanded;

        #endregion

        #region Unity Lifecycle
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            ValidateDependencies();
        }

        #endregion

        #region Core Factory Methods
        
        /// <summary>
        /// Create squad by squad data ID
        /// </summary>
        /// <param name="squadDataId">Squad data identifier</param>
        /// <param name="position">Squad spawn position</param>
        /// <param name="rotation">Squad spawn rotation</param>
        /// <returns>Created squad model or null if failed</returns>
        public SquadModel CreateSquad(uint squadDataId, Vector3 position, Quaternion rotation)
        {
            // Get squad data
            SquadDataSO squadData = _dataManager.GetSquadData(squadDataId);
            if (squadData == null)
            {
                Debug.LogError($"SquadFactory: Squad data not found for ID: {squadDataId}");
                return null;
            }
            
            // Create squad model
            int squadId = _nextSquadId++;
            SquadModel squadModel = new SquadModel(squadId, squadData, position, rotation);
            
            // Create units for squad
            List<UnitModel> squadUnits = CreateSquadUnits(squadData, position, rotation, squadId);
            if (squadUnits.Count == 0)
            {
                Debug.LogError($"SquadFactory: Failed to create units for squad: {squadDataId}");
                return null;
            }
            
            // Add units to squad
            squadModel.AddUnits(squadUnits);
            
            // Track squad
            _activeSquads[squadId] = squadModel;
            
            // Trigger event
            OnSquadCreated?.Invoke(squadModel);
            
            Debug.Log($"SquadFactory: Created squad {squadId} from template {squadDataId} with {squadUnits.Count} units");
            
            return squadModel;
        }
        
        /// <summary>
        /// Disband squad and return units to pool
        /// </summary>
        /// <param name="squadId">Squad identifier</param>
        public void DisbandSquad(int squadId)
        {
            if (!_activeSquads.TryGetValue(squadId, out SquadModel squadModel))
            {
                Debug.LogWarning($"SquadFactory: Squad not found: {squadId}");
                return;
            }
            
            // Trigger event before disbanding
            OnSquadDisbanded?.Invoke(squadModel);
            
            // Return all units to pool
            List<IEntity> unitEntities = squadModel.GetAllUnitEntities();
            foreach (var entity in unitEntities)
            {
                if (entity != null && _unitFactory != null)
                {
                    _unitFactory.ReturnUnit(entity);
                }
            }
            
            // Clean up squad model
            squadModel.Cleanup();
            
            // Remove from tracking
            _activeSquads.Remove(squadId);
            
            Debug.Log($"SquadFactory: Disbanded squad {squadId}");
        }

        #endregion

        #region Public Queries
        
        /// <summary>
        /// Get squad by ID
        /// </summary>
        public SquadModel GetSquad(int squadId)
        {
            _activeSquads.TryGetValue(squadId, out SquadModel squad);
            return squad;
        }
        
        /// <summary>
        /// Get all active squads
        /// </summary>
        public List<SquadModel> GetAllSquads()
        {
            return new List<SquadModel>(_activeSquads.Values);
        }
        
        /// <summary>
        /// Check if squad exists
        /// </summary>
        public bool HasSquad(int squadId)
        {
            return _activeSquads.ContainsKey(squadId);
        }

        #endregion

        #region Private Methods
        
        private void ValidateDependencies()
        {
            if (_unitFactory == null)
            {
                _unitFactory = FindObjectOfType<UnitFactory>();
                if (_unitFactory == null)
                {
                    Debug.LogError("SquadFactory: UnitFactory dependency is missing!");
                }
            }
            
            if (_dataManager == null)
            {
                _dataManager = DataManager.Instance;
                if (_dataManager == null)
                {
                    Debug.LogError("SquadFactory: DataManager dependency is missing!");
                }
            }
        }
        
        private List<UnitModel> CreateSquadUnits(SquadDataSO squadData, Vector3 position, 
            Quaternion rotation, int squadId)
        {
            List<UnitModel> squadUnits = new List<UnitModel>();
            
            if (_unitFactory == null)
            {
                Debug.LogError("SquadFactory: UnitFactory is not available");
                return squadUnits;
            }
            
            foreach (var composition in squadData.UnitCompositions)
            {
                if (!composition.UnitData)
                    continue;
                
                uint unitDataId = composition.UnitData.UnitId;
                
                for (int i = 0; i < composition.Count; i++)
                {
                    IEntity unitEntity = _unitFactory.CreateUnit(unitDataId, position, rotation);
                    
                    if (unitEntity != null)
                    {
                        UnitModel unitModel = _unitFactory.GetUnitModel(unitEntity);
                        if (unitModel != null)
                        {
                            unitModel.SetSquadId(squadId);
                            squadUnits.Add(unitModel);
                        }
                    }
                }
            }
            
            return squadUnits;
        }

        #endregion

        #region Cleanup
        
        private void OnDestroy()
        {
            // Disband all squads
            var squadIds = new List<int>(_activeSquads.Keys);
            foreach (int squadId in squadIds)
            {
                DisbandSquad(squadId);
            }
            
            _activeSquads.Clear();
        }

        #endregion
    }
}