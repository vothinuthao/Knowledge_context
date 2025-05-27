using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Core.Utils;
using VikingRaven.Core.Data;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Data;
using VikingRaven.Units.Models;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.Factory
{
    /// <summary>
    /// Enhanced Squad Factory - Creates squads with proper formation prefabs
    /// ENHANCED: Now creates squad GameObjects and spawns units in proper formation positions
    /// Maintains object pooling while providing better squad management
    /// </summary>
    public class SquadFactory : Singleton<SquadFactory>
    {
        #region Configuration

        [TitleGroup("Squad Prefab Configuration")]
        [Tooltip("Default squad container prefab")]
        [SerializeField, AssetsOnly]
        private GameObject _defaultSquadPrefab;
        
        [Tooltip("Enable squad prefab creation for better organization")]
        [SerializeField, ToggleLeft]
        private bool _createSquadPrefabs = true;
        
        [Tooltip("Parent transform for all squad containers")]
        [SerializeField]
        private Transform _squadParentContainer;

        #endregion

        #region Formation Configuration

        [TitleGroup("Formation Settings")]
        [Tooltip("Use initial formation positioning when spawning units")]
        [SerializeField, ToggleLeft]
        private bool _useInitialFormationPositioning = true;
        
        [Tooltip("Formation spacing for initial spawn")]
        [SerializeField, Range(1f, 5f)]
        private float _initialFormationSpacing = 2.0f;
        
        [Tooltip("Random offset range for natural positioning")]
        [SerializeField, Range(0f, 1f)]
        private float _spawnRandomOffset = 0.2f;

        #endregion

        #region Dependencies
        
        [TitleGroup("Dependencies")]
        [Tooltip("Unit factory reference")]
        [SerializeField, Required]
        private UnitFactory _unitFactory;
        
        [Tooltip("Data manager reference")]
        [SerializeField, Required]
        private DataManager _dataManager;

        #endregion

        #region Runtime Data
        
        [TitleGroup("Runtime Information")]
        [ShowInInspector, ReadOnly]
        private int ActiveSquadsCount => _activeSquads?.Count ?? 0;
        
        [ShowInInspector, ReadOnly]
        private int NextSquadId => _nextSquadId;
        
        [ShowInInspector, ReadOnly]
        private int TotalSquadPrefabsCreated => _squadPrefabsCreated;

        #endregion

        #region Private Fields
        
        // Active squads tracking
        private Dictionary<int, SquadModel> _activeSquads = new Dictionary<int, SquadModel>();
        private Dictionary<int, GameObject> _squadPrefabs = new Dictionary<int, GameObject>();
        
        // Squad ID generation
        private int _nextSquadId = 1;
        private int _squadPrefabsCreated = 0;

        #endregion

        #region Events
        
        public event System.Action<SquadModel> OnSquadCreated;
        public event System.Action<SquadModel> OnSquadDisbanded;
        public event System.Action<SquadModel, GameObject> OnSquadPrefabCreated;

        #endregion

        #region Unity Lifecycle
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            ValidateDependencies();
            SetupSquadParentContainer();
            CreateDefaultSquadPrefab();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Validate dependencies and setup default configurations
        /// </summary>
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

        /// <summary>
        /// Setup squad parent container for organization
        /// </summary>
        private void SetupSquadParentContainer()
        {
            if (_squadParentContainer == null)
            {
                GameObject containerObj = new GameObject("Squad Containers");
                containerObj.transform.SetParent(transform);
                _squadParentContainer = containerObj.transform;
                
                Debug.Log("SquadFactory: Created default squad parent container");
            }
        }

        /// <summary>
        /// Create default squad prefab if not assigned
        /// </summary>
        private void CreateDefaultSquadPrefab()
        {
            if (_defaultSquadPrefab == null && _createSquadPrefabs)
            {
                _defaultSquadPrefab = CreateDefaultSquadPrefabAsset();
                Debug.Log("SquadFactory: Created default squad prefab");
            }
        }

        /// <summary>
        /// Create a default squad prefab asset programmatically
        /// </summary>
        private GameObject CreateDefaultSquadPrefabAsset()
        {
            GameObject prefab = new GameObject("DefaultSquadPrefab");
            
            // Add squad identifier component
            var squadIdentifier = prefab.AddComponent<SquadIdentifier>();
            
            // Add transform component for positioning
            prefab.transform.position = Vector3.zero;
            prefab.transform.rotation = Quaternion.identity;
            
            // Don't destroy this prefab when changing scenes
            DontDestroyOnLoad(prefab);
            
            return prefab;
        }

        #endregion

        #region Core Factory Methods
        
        /// <summary>
        /// Create squad by squad data ID with enhanced formation positioning
        /// ENHANCED: Now creates squad prefab and spawns units in proper formation
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
            
            // Create squad prefab container
            GameObject squadPrefab = null;
            if (_createSquadPrefabs)
            {
                squadPrefab = CreateSquadPrefabContainer(squadId, squadData, position, rotation);
                _squadPrefabs[squadId] = squadPrefab;
            }
            
            // Create units for squad with proper formation positioning
            List<UnitModel> squadUnits = CreateSquadUnitsWithFormation(squadData, position, rotation, squadId, squadPrefab);
            if (squadUnits.Count == 0)
            {
                Debug.LogError($"SquadFactory: Failed to create units for squad: {squadDataId}");
                
                // Cleanup squad prefab if units creation failed
                if (squadPrefab != null)
                {
                    DestroyImmediate(squadPrefab);
                    _squadPrefabs.Remove(squadId);
                }
                return null;
            }
            
            // Add units to squad
            squadModel.AddUnits(squadUnits);
            
            // Track squad
            _activeSquads[squadId] = squadModel;
            
            // Trigger events
            OnSquadCreated?.Invoke(squadModel);
            if (squadPrefab != null)
            {
                OnSquadPrefabCreated?.Invoke(squadModel, squadPrefab);
            }
            
            Debug.Log($"SquadFactory: Created squad {squadId} from template {squadDataId} with {squadUnits.Count} units" +
                     $"{(squadPrefab ? " with squad prefab" : "")}");
            
            return squadModel;
        }
        
        /// <summary>
        /// Disband squad and return units to pool
        /// ENHANCED: Also destroys squad prefab container
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
            
            // Destroy squad prefab if exists
            if (_squadPrefabs.TryGetValue(squadId, out GameObject squadPrefab))
            {
                if (squadPrefab != null)
                {
                    DestroyImmediate(squadPrefab);
                }
                _squadPrefabs.Remove(squadId);
            }
            
            // Clean up squad model
            squadModel.Cleanup();
            
            // Remove from tracking
            _activeSquads.Remove(squadId);
            
            Debug.Log($"SquadFactory: Disbanded squad {squadId}");
        }

        #endregion

        #region Squad Prefab Creation

        /// <summary>
        /// Create squad prefab container for organization
        /// </summary>
        private GameObject CreateSquadPrefabContainer(int squadId, SquadDataSO squadData, Vector3 position, Quaternion rotation)
        {
            GameObject squadPrefab;
            
            if (_defaultSquadPrefab != null)
            {
                squadPrefab = Instantiate(_defaultSquadPrefab, position, rotation, _squadParentContainer);
            }
            else
            {
                squadPrefab = new GameObject($"Squad_{squadId}_{squadData.DisplayName}");
                squadPrefab.transform.SetParent(_squadParentContainer);
                squadPrefab.transform.position = position;
                squadPrefab.transform.rotation = rotation;
            }
            
            // Setup squad identifier
            var squadIdentifier = squadPrefab.GetComponent<SquadIdentifier>();
            if (squadIdentifier == null)
            {
                squadIdentifier = squadPrefab.AddComponent<SquadIdentifier>();
            }
            
            squadIdentifier.SetSquadData(squadId, squadData);
            
            // Add visual indicators for debugging
            if (Application.isEditor)
            {
                AddSquadDebugVisualization(squadPrefab, squadData);
            }
            
            _squadPrefabsCreated++;
            
            Debug.Log($"SquadFactory: Created squad prefab container for squad {squadId}");
            return squadPrefab;
        }

        /// <summary>
        /// Add debug visualization to squad prefab
        /// </summary>
        private void AddSquadDebugVisualization(GameObject squadPrefab, SquadDataSO squadData)
        {
            // Add a simple sphere as visual indicator
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "SquadCenter_Debug";
            indicator.transform.SetParent(squadPrefab.transform);
            indicator.transform.localPosition = Vector3.zero;
            indicator.transform.localScale = Vector3.one * 0.5f;
            
            // Remove collider as it's just visual
            Collider indicatorCollider = indicator.GetComponent<Collider>();
            if (indicatorCollider != null)
            {
                DestroyImmediate(indicatorCollider);
            }
            
            // Set color based on squad data
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = GetSquadDebugColor(squadData);
                renderer.material = mat;
            }
        }

        /// <summary>
        /// Get debug color for squad visualization
        /// </summary>
        private Color GetSquadDebugColor(SquadDataSO squadData)
        {
            // Generate color based on squad ID hash
            int hash = squadData.SquadId.GetHashCode();
            float hue = (hash * 0.618034f) % 1f; // Golden ratio for better distribution
            return Color.HSVToRGB(hue, 0.7f, 0.9f);
        }

        #endregion

        #region Enhanced Unit Creation with Formation

        /// <summary>
        /// Create squad units with proper formation positioning
        /// ENHANCED: Units are spawned in formation positions from the start
        /// </summary>
        private List<UnitModel> CreateSquadUnitsWithFormation(SquadDataSO squadData, Vector3 squadPosition, 
            Quaternion squadRotation, int squadId, GameObject squadPrefab)
        {
            List<UnitModel> squadUnits = new List<UnitModel>();
            
            if (_unitFactory == null)
            {
                Debug.LogError("SquadFactory: UnitFactory is not available");
                return squadUnits;
            }
            
            // Calculate total unit count for formation
            int totalUnits = 0;
            foreach (var composition in squadData.UnitCompositions)
            {
                if (composition.UnitData != null)
                {
                    totalUnits += composition.Count;
                }
            }
            
            // Generate formation positions
            Vector3[] formationPositions = GenerateInitialFormationPositions(
                squadData.DefaultFormationType, 
                totalUnits, 
                squadPosition, 
                squadRotation
            );
            
            int unitIndex = 0;
            
            // Create units based on composition
            foreach (var composition in squadData.UnitCompositions)
            {
                if (!composition.UnitData) continue;
                
                uint unitDataId = composition.UnitData.UnitId;
                
                for (int i = 0; i < composition.Count; i++)
                {
                    // Get formation position for this unit
                    Vector3 unitPosition = squadPosition; // Default fallback
                    if (unitIndex < formationPositions.Length)
                    {
                        unitPosition = formationPositions[unitIndex];
                        
                        // Add small random offset for natural look
                        if (_spawnRandomOffset > 0)
                        {
                            Vector3 randomOffset = new Vector3(
                                Random.Range(-_spawnRandomOffset, _spawnRandomOffset),
                                0,
                                Random.Range(-_spawnRandomOffset, _spawnRandomOffset)
                            );
                            unitPosition += randomOffset;
                        }
                    }
                    
                    // Create unit at formation position
                    IEntity unitEntity = _unitFactory.CreateUnit(unitDataId, unitPosition, squadRotation);
                    
                    if (unitEntity != null)
                    {
                        // Set unit parent to squad prefab if available
                        if (squadPrefab != null)
                        {
                            var unitObj = unitEntity as MonoBehaviour;
                            if (unitObj != null)
                            {
                                unitObj.transform.SetParent(squadPrefab.transform);
                            }
                        }
                        
                        // Setup unit model
                        UnitModel unitModel = _unitFactory.GetUnitModel(unitEntity);
                        if (unitModel != null)
                        {
                            unitModel.SetSquadId(squadId);
                            squadUnits.Add(unitModel);
                            
                            // Setup formation component
                            SetupUnitFormationComponent(unitEntity, squadId, unitIndex, squadData.DefaultFormationType);
                        }
                    }
                    
                    unitIndex++;
                }
            }
            
            Debug.Log($"SquadFactory: Created {squadUnits.Count} units with {squadData.DefaultFormationType} formation");
            return squadUnits;
        }

        /// <summary>
        /// Generate initial formation positions for units
        /// </summary>
        private Vector3[] GenerateInitialFormationPositions(FormationType formationType, int unitCount, 
            Vector3 squadPosition, Quaternion squadRotation)
        {
            if (unitCount <= 0) return new Vector3[0];
            
            Vector3[] localPositions = new Vector3[unitCount];
            float spacing = _initialFormationSpacing;
            
            // Generate formation based on type
            switch (formationType)
            {
                case FormationType.Phalanx:
                    GeneratePhalanxFormationPositions(localPositions, spacing);
                    break;
                case FormationType.Testudo:
                    GenerateTestudoFormationPositions(localPositions, spacing);
                    break;
                case FormationType.Normal:
                    GenerateNormalFormationPositions(localPositions, spacing);
                    break;
            }
            
            Vector3[] worldPositions = new Vector3[unitCount];
            for (int i = 0; i < unitCount; i++)
            {
                Vector3 rotatedPosition = squadRotation * localPositions[i];
                worldPositions[i] = squadPosition + rotatedPosition;
            }
            
            return worldPositions;
        }

        private void GenerateLineFormationPositions(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            for (int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) * 0.5f) * spacing;
                positions[i] = new Vector3(x, 0, 0);
            }
        }

        private void GenerateColumnFormationPositions(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            for (int i = 0; i < count; i++)
            {
                float z = (i - (count - 1) * 0.5f) * spacing;
                positions[i] = new Vector3(0, 0, z);
            }
        }

        private void GeneratePhalanxFormationPositions(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            int width = Mathf.CeilToInt(Mathf.Sqrt(count));
            
            for (int i = 0; i < count; i++)
            {
                int row = i / width;
                int col = i % width;
                
                float x = (col - (width - 1) * 0.5f) * spacing;
                float z = (row - ((count - 1) / width) * 0.5f) * spacing;
                
                positions[i] = new Vector3(x, 0, z);
            }
        }

        private void GenerateTestudoFormationPositions(Vector3[] positions, float spacing)
        {
            GeneratePhalanxFormationPositions(positions, spacing * 0.7f); // Tighter formation
        }

        private void GenerateCircleFormationPositions(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            
            if (count == 1)
            {
                positions[0] = Vector3.zero;
                return;
            }
            
            float radius = count * spacing / (2 * Mathf.PI);
            radius = Mathf.Max(radius, spacing * 1.5f);
            
            for (int i = 0; i < count; i++)
            {
                float angle = (i * 2 * Mathf.PI) / count;
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;
                
                positions[i] = new Vector3(x, 0, z);
            }
        }

        private void GenerateNormalFormationPositions(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            const int gridWidth = 3;
            
            for (int i = 0; i < count; i++)
            {
                int row = i / gridWidth;
                int col = i % gridWidth;
                
                float x = (col - 1) * spacing;
                float z = (row - 1) * spacing;
                
                positions[i] = new Vector3(x, 0, z);
            }
        }

        /// <summary>
        /// Setup formation component for unit
        /// </summary>
        private void SetupUnitFormationComponent(IEntity unitEntity, int squadId, int slotIndex, FormationType formationType)
        {
            var formationComponent = unitEntity.GetComponent<FormationComponent>();
            if (formationComponent != null)
            {
                formationComponent.SetSquadId(squadId);
                formationComponent.SetFormationSlot(slotIndex);
                formationComponent.SetFormationType(formationType, false);
            }
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
        /// Get squad prefab by ID
        /// </summary>
        public GameObject GetSquadPrefab(int squadId)
        {
            _squadPrefabs.TryGetValue(squadId, out GameObject prefab);
            return prefab;
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

        #region Debug Tools

        [Button("Show Squad Factory Stats"), TitleGroup("Debug Tools")]
        public void ShowSquadFactoryStats()
        {
            string stats = "=== Squad Factory Statistics ===\n";
            stats += $"Active Squads: {ActiveSquadsCount}\n";
            stats += $"Next Squad ID: {NextSquadId}\n";
            stats += $"Squad Prefabs Created: {TotalSquadPrefabsCreated}\n";
            stats += $"Use Formation Positioning: {_useInitialFormationPositioning}\n";
            stats += $"Create Squad Prefabs: {_createSquadPrefabs}\n";
            stats += $"Formation Spacing: {_initialFormationSpacing}\n";
            
            Debug.Log(stats);
        }

        [Button("Force Clean All Squads"), TitleGroup("Debug Tools")]
        public void ForceCleanAllSquads()
        {
            var squadIds = new List<int>(_activeSquads.Keys);
            foreach (int squadId in squadIds)
            {
                DisbandSquad(squadId);
            }
            
            Debug.Log("SquadFactory: Force cleaned all squads");
        }

        #endregion

        #region Cleanup

        protected override void OnDestroy()
        {
            // Disband all squads
            var squadIds = new List<int>(_activeSquads.Keys);
            foreach (int squadId in squadIds)
            {
                DisbandSquad(squadId);
            }
            
            _activeSquads.Clear();
            _squadPrefabs.Clear();
        }

        #endregion
    }
}