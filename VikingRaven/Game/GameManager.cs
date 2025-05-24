using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Core.Data;
using VikingRaven.Units.Components;
using VikingRaven.Units.Models;
using VikingRaven.Units.Systems;

namespace VikingRaven.Game
{
    /// <summary>
    /// Enhanced GameManager quản lý toàn bộ lifecycle game từ initialization đến completion
    /// Hoạt động như central coordinator cho tất cả hệ thống game
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Game State Management
        
        [TitleGroup("Game State")]
        [Tooltip("Current state of the game")]
        [SerializeField, ReadOnly, EnumToggleButtons] 
        private GameState _currentGameState = GameState.NotInitialized;
        
        [Tooltip("Enable auto-progression through game states")]
        [SerializeField, ToggleLeft]
        private bool _autoProgressGameStates = true;
        
        [Tooltip("Time to wait between state transitions")]
        [SerializeField, Range(0.1f, 5f)]
        private float _stateTransitionDelay = 1f;

        #endregion

        #region Factory and Manager References
        
        [TitleGroup("Core Systems")]
        [Tooltip("Data manager for game data")]
        [SerializeField, Required]
        private DataManager _dataManager;
        
        [Tooltip("Unit factory for creating individual units")]
        [SerializeField, Required]
        private UnitFactory _unitFactory;
        
        [Tooltip("Squad factory for creating unit squads")]
        [SerializeField, Required]
        private SquadFactory _squadFactory;
        
        [Tooltip("Entity registry for ECS management")]
        [SerializeField, Required]
        private EntityRegistry _entityRegistry;
        
        [Tooltip("System registry for ECS systems")]
        [SerializeField, Required]
        private SystemRegistry _systemRegistry;

        #endregion

        #region Game Configuration
        
        [TitleGroup("Game Configuration")]
        [Tooltip("Number of player squads to spawn at game start")]
        [SerializeField, Range(1, 5), ProgressBar(1, 5)]
        private int _initialPlayerSquadCount = 2;
        
        [Tooltip("Number of enemy squads to spawn at game start")]
        [SerializeField, Range(1, 5), ProgressBar(1, 5)]
        private int _initialEnemySquadCount = 2;
        
        [Tooltip("Number of units per squad")]
        [SerializeField, Range(3, 15), ProgressBar(3, 15)]
        private int _unitsPerSquad = 9;

        #endregion

        #region Spawn Configuration
        
        [TitleGroup("Spawn Settings")]
        [Tooltip("Player spawn positions")]
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true)]
        private List<Transform> _playerSpawnPoints = new List<Transform>();
        
        [Tooltip("Enemy spawn positions")]
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true)]
        private List<Transform> _enemySpawnPoints = new List<Transform>();
        
        [TitleGroup("All Systems in Game")]
        [Tooltip("Systems in game")]
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true)]
        private List<Transform> _gameSystems = new List<Transform>();
        
        [Tooltip("Default unit types for player squads")]
        [SerializeField, EnumToggleButtons]
        private List<UnitType> _playerUnitTypes = new List<UnitType> { UnitType.Infantry, UnitType.Archer };
        
        [Tooltip("Default unit types for enemy squads")]
        [SerializeField, EnumToggleButtons]
        private List<UnitType> _enemyUnitTypes = new List<UnitType> { UnitType.Infantry, UnitType.Pike };

        #endregion

        #region Runtime Data
        
        [TitleGroup("Runtime Information")]
        [ShowInInspector, ReadOnly]
        private List<SquadModel> _playerSquads = new List<SquadModel>();
        
        [ShowInInspector, ReadOnly]
        private List<SquadModel> _enemySquads = new List<SquadModel>();
        
        [ShowInInspector, ReadOnly]
        private List<UnitModel> _allUnits = new List<UnitModel>();
        
        [ShowInInspector, ReadOnly, ProgressBar(0, 1)]
        private float _gameProgressPercentage = 0f;

        #endregion

        #region Events
        
        public event Action<GameState> OnGameStateChanged;
        public event Action<SquadModel> OnSquadSpawned;
        public event Action<SquadModel> OnSquadDestroyed;
        public event Action OnGameCompleted;
        public event Action OnGameFailed;

        #endregion

        #region Properties
        
        public GameState CurrentGameState => _currentGameState;
        public bool IsGameActive => _currentGameState == GameState.Playing;
        public bool IsGameInitialized => _currentGameState != GameState.NotInitialized;
        public List<SquadModel> PlayerSquads => new List<SquadModel>(_playerSquads);
        public List<SquadModel> EnemySquads => new List<SquadModel>(_enemySquads);
        public int TotalUnitsCount => _allUnits.Count;
        
        // Public accessors for other systems (avoiding singleton pattern)
        public DataManager DataManager => _dataManager;
        public UnitFactory UnitFactory => _unitFactory;
        public SquadFactory SquadFactory => _squadFactory;
        public EntityRegistry EntityRegistry => _entityRegistry;
        public SystemRegistry SystemRegistry => _systemRegistry;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
        }

        private void Start()
        {
            if (_autoProgressGameStates)
            {
                StartCoroutine(AutoProgressGameStates());
            }
        }

        private void Update()
        {
            UpdateGameLogic();
        }

        private void OnDestroy()
        {
            CleanupGame();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Validate all required references are assigned
        /// </summary>
        private void ValidateReferences()
        {
            bool hasErrors = false;
            
            if (_dataManager == null)
            {
                Debug.LogError("GameManager: DataManager reference is missing!");
                hasErrors = true;
            }
            
            if (_unitFactory == null)
            {
                Debug.LogError("GameManager: UnitFactory reference is missing!");
                hasErrors = true;
            }
            
            if (_squadFactory == null)
            {
                Debug.LogError("GameManager: SquadFactory reference is missing!");
                hasErrors = true;
            }
            
            if (_entityRegistry == null)
            {
                Debug.LogError("GameManager: EntityRegistry reference is missing!");
                hasErrors = true;
            }
            
            if (_systemRegistry == null)
            {
                Debug.LogError("GameManager: SystemRegistry reference is missing!");
                hasErrors = true;
            }
            
            if (hasErrors)
            {
                Debug.LogError("GameManager: Critical references missing! Please assign them in the inspector.");
                return;
            }
            
            Debug.Log("GameManager: All references validated successfully");
        }

        /// <summary>
        /// Auto progress through game states
        /// </summary>
        private IEnumerator AutoProgressGameStates()
        {
            yield return StartCoroutine(InitializeGame());
            
            if (_currentGameState == GameState.Initialized)
            {
                yield return new WaitForSeconds(_stateTransitionDelay);
                yield return StartCoroutine(LoadGameData());
            }
            
            if (_currentGameState == GameState.DataLoaded)
            {
                yield return new WaitForSeconds(_stateTransitionDelay);
                yield return StartCoroutine(SpawnInitialSquads());
            }
            
            if (_currentGameState == GameState.SquadsSpawned)
            {
                yield return new WaitForSeconds(_stateTransitionDelay);
                StartGame();
            }
        }

        /// <summary>
        /// Initialize core game systems
        /// </summary>
        private IEnumerator InitializeGame()
        {
            Debug.Log("GameManager: Initializing game systems...");
            ChangeGameState(GameState.Initializing);
            
            // Initialize DataManager first
            if (_dataManager != null && !_dataManager.IsInitialized)
            {
                _dataManager.Initialize();
                yield return new WaitForSeconds(0.1f); // Small delay for initialization
            }
            
            // Initialize EntityRegistry
            if (_entityRegistry != null)
            {
                Debug.Log("GameManager: EntityRegistry ready");
            }
            
            // Initialize and register ECS systems
            if (_systemRegistry != null)
            {
                InitializeAllSystems();
                yield return new WaitForSeconds(0.1f);
            }
            
            // Initialize factories with dependencies
            InitializeFactories();
            
            ChangeGameState(GameState.Initialized);
            Debug.Log("GameManager: Game systems initialized successfully");
        }

        /// <summary>
        /// Initialize ECS systems
        /// </summary>
        private void InitializeAllSystems()
        {
            var allSystems = FindObjectsOfType<MonoBehaviour>();
            
            foreach (var system in allSystems)
            {
                if (system is ISystem ecsSystem)
                {
                    _systemRegistry.RegisterSystem(ecsSystem);
                }
            }
            _systemRegistry.InitializeAllSystems();
            Debug.Log("GameManager: ECS systems initialized");
        }

        /// <summary>
        /// Initialize factories with proper dependencies
        /// </summary>
        private void InitializeFactories()
        {
            // Initialize UnitFactory
            if (_unitFactory != null)
            {
                // UnitFactory will use DataManager for unit data
                Debug.Log("GameManager: UnitFactory initialized");
            }
            
            // Initialize SquadFactory
            if (_squadFactory != null)
            {
                // SquadFactory will use both DataManager and UnitFactory
                Debug.Log("GameManager: SquadFactory initialized");
            }
        }

        #endregion

        #region Data Loading

        /// <summary>
        /// Load all game data
        /// </summary>
        private IEnumerator LoadGameData()
        {
            Debug.Log("GameManager: Loading game data...");
            ChangeGameState(GameState.LoadingData);
            
            // Load data through DataManager
            if (_dataManager != null)
            {
                // DataManager will handle loading unit and squad data
                yield return new WaitForSeconds(0.1f); // Simulate loading time
            }
            
            ChangeGameState(GameState.DataLoaded);
            Debug.Log("GameManager: Game data loaded successfully");
        }

        #endregion

        #region Squad Spawning

        /// <summary>
        /// Spawn initial squads for the game
        /// </summary>
        private IEnumerator SpawnInitialSquads()
        {
            Debug.Log("GameManager: Spawning initial squads...");
            ChangeGameState(GameState.SpawningSquads);
            
            // Spawn player squads
            for (int i = 0; i < _initialPlayerSquadCount; i++)
            {
                yield return StartCoroutine(SpawnPlayerSquad(i));
                yield return new WaitForSeconds(0.2f); // Small delay between spawns
            }
            
            // Spawn enemy squads
            for (int i = 0; i < _initialEnemySquadCount; i++)
            {
                yield return StartCoroutine(SpawnEnemySquad(i));
                yield return new WaitForSeconds(0.2f);
            }
            
            ChangeGameState(GameState.SquadsSpawned);
            Debug.Log($"GameManager: Spawned {_playerSquads.Count} player squads and {_enemySquads.Count} enemy squads");
        }

        /// <summary>
        /// Spawn a single player squad
        /// </summary>
        private IEnumerator SpawnPlayerSquad(int squadIndex)
        {
            Vector3 spawnPosition = GetPlayerSpawnPosition(squadIndex);
            UnitType unitType = GetPlayerUnitType(squadIndex);
            
            SquadModel newSquad = _squadFactory.CreateSquad(1, _playerSpawnPoints[0].position, _playerSpawnPoints[0].rotation);
            
            if (newSquad != null)
            {
                _playerSquads.Add(newSquad);
                CollectUnitsFromSquad(newSquad);
                OnSquadSpawned?.Invoke(newSquad);
                
                Debug.Log($"GameManager: Spawned player squad {newSquad.SquadId} with {newSquad.UnitCount} {unitType} units at {spawnPosition}");
            }
            
            yield return null;
        }

        /// <summary>
        /// Spawn a single enemy squad
        /// </summary>
        private IEnumerator SpawnEnemySquad(int squadIndex)
        {
            Vector3 spawnPosition = GetEnemySpawnPosition(squadIndex);
            UnitType unitType = GetEnemyUnitType(squadIndex);
            
            SquadModel newSquad = _squadFactory.CreateSquad(1, _playerSpawnPoints[2].position, _playerSpawnPoints[2].rotation);
            
            if (newSquad != null)
            {
                _enemySquads.Add(newSquad);
                CollectUnitsFromSquad(newSquad);
                OnSquadSpawned?.Invoke(newSquad);
                
                Debug.Log($"GameManager: Spawned enemy squad {newSquad.SquadId} with {newSquad.UnitCount} {unitType} units at {spawnPosition}");
            }
            
            yield return null;
        }

        /// <summary>
        /// Collect all units from a squad for tracking
        /// </summary>
        private void CollectUnitsFromSquad(SquadModel squad)
        {
            if (squad == null) return;
            
            foreach (var unit in squad.Units)
            {
                if (unit != null && !_allUnits.Contains(unit))
                {
                    _allUnits.Add(unit);
                }
            }
        }

        #endregion

        #region Spawn Position Management

        /// <summary>
        /// Get spawn position for player squad
        /// </summary>
        private Vector3 GetPlayerSpawnPosition(int squadIndex)
        {
            if (_playerSpawnPoints.Count > 0)
            {
                int spawnIndex = squadIndex % _playerSpawnPoints.Count;
                return _playerSpawnPoints[spawnIndex].position;
            }
            
            // Default spawn positions if no spawn points assigned
            return new Vector3(-10f + squadIndex * 5f, 0, 0);
        }

        /// <summary>
        /// Get spawn position for enemy squad
        /// </summary>
        private Vector3 GetEnemySpawnPosition(int squadIndex)
        {
            if (_enemySpawnPoints.Count > 0)
            {
                int spawnIndex = squadIndex % _enemySpawnPoints.Count;
                return _enemySpawnPoints[spawnIndex].position;
            }
            
            // Default spawn positions if no spawn points assigned
            return new Vector3(10f + squadIndex * 5f, 0, 0);
        }

        /// <summary>
        /// Get unit type for player squad
        /// </summary>
        private UnitType GetPlayerUnitType(int squadIndex)
        {
            if (_playerUnitTypes.Count > 0)
            {
                int typeIndex = squadIndex % _playerUnitTypes.Count;
                return _playerUnitTypes[typeIndex];
            }
            
            return UnitType.Infantry; // Default
        }

        /// <summary>
        /// Get unit type for enemy squad
        /// </summary>
        private UnitType GetEnemyUnitType(int squadIndex)
        {
            if (_enemyUnitTypes.Count > 0)
            {
                int typeIndex = squadIndex % _enemyUnitTypes.Count;
                return _enemyUnitTypes[typeIndex];
            }
            
            return UnitType.Infantry; // Default
        }

        #endregion

        #region Game Flow Management

        /// <summary>
        /// Start the actual gameplay
        /// </summary>
        public void StartGame()
        {
            Debug.Log("GameManager: Starting game...");
            ChangeGameState(GameState.Playing);
        }

        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            if (_currentGameState == GameState.Playing)
            {
                ChangeGameState(GameState.Paused);
                Time.timeScale = 0f;
                Debug.Log("GameManager: Game paused");
            }
        }

        /// <summary>
        /// Resume the game
        /// </summary>
        public void ResumeGame()
        {
            if (_currentGameState == GameState.Paused)
            {
                ChangeGameState(GameState.Playing);
                Time.timeScale = 1f;
                Debug.Log("GameManager: Game resumed");
            }
        }

        /// <summary>
        /// End the game with victory condition
        /// </summary>
        public void CompleteGame()
        {
            Debug.Log("GameManager: Game completed successfully!");
            ChangeGameState(GameState.Completed);
            OnGameCompleted?.Invoke();
        }

        /// <summary>
        /// End the game with failure condition
        /// </summary>
        public void FailGame()
        {
            Debug.Log("GameManager: Game failed!");
            ChangeGameState(GameState.Failed);
            OnGameFailed?.Invoke();
        }

        /// <summary>
        /// Restart the game
        /// </summary>
        public void RestartGame()
        {
            Debug.Log("GameManager: Restarting game...");
            
            CleanupGame();
            
            // Reset state and restart
            ChangeGameState(GameState.NotInitialized);
            
            if (_autoProgressGameStates)
            {
                StartCoroutine(AutoProgressGameStates());
            }
        }

        #endregion

        #region Game Logic Updates

        /// <summary>
        /// Update game logic each frame
        /// </summary>
        private void UpdateGameLogic()
        {
            if (_currentGameState != GameState.Playing) return;
            
            // Update ECS systems
            if (_systemRegistry != null)
            {
                _systemRegistry.ExecuteAllSystems();
            }
            
            // Check win/lose conditions
            CheckGameConditions();
            
            // Update game progress
            UpdateGameProgress();
        }

        /// <summary>
        /// Check for game win/lose conditions
        /// </summary>
        private void CheckGameConditions()
        {
            // Check if all enemy squads are destroyed
            int viableEnemySquads = 0;
            foreach (var squad in _enemySquads)
            {
                if (squad != null && squad.IsViable())
                {
                    viableEnemySquads++;
                }
            }
            
            if (viableEnemySquads == 0)
            {
                CompleteGame();
                return;
            }
            
            // Check if all player squads are destroyed
            int viablePlayerSquads = 0;
            foreach (var squad in _playerSquads)
            {
                if (squad != null && squad.IsViable())
                {
                    viablePlayerSquads++;
                }
            }
            
            if (viablePlayerSquads == 0)
            {
                FailGame();
                return;
            }
        }

        /// <summary>
        /// Update game progress percentage
        /// </summary>
        private void UpdateGameProgress()
        {
            if (_enemySquads.Count == 0) return;
            
            int totalEnemySquads = _enemySquads.Count;
            int destroyedEnemySquads = 0;
            
            foreach (var squad in _enemySquads)
            {
                if (squad == null || !squad.IsViable())
                {
                    destroyedEnemySquads++;
                }
            }
            
            _gameProgressPercentage = (float)destroyedEnemySquads / totalEnemySquads;
        }

        #endregion

        #region State Management

        /// <summary>
        /// Change game state and notify listeners
        /// </summary>
        private void ChangeGameState(GameState newState)
        {
            if (_currentGameState == newState) return;
            
            GameState oldState = _currentGameState;
            _currentGameState = newState;
            
            Debug.Log($"GameManager: State changed from {oldState} to {newState}");
            OnGameStateChanged?.Invoke(newState);
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Create a new squad at runtime
        /// </summary>
        public SquadModel CreateSquad(UnitType unitType, Vector3 position, bool isEnemy = false)
        {
            if (_squadFactory == null)
            {
                Debug.LogError("GameManager: SquadFactory is not available");
                return null;
            }
            
            SquadModel newSquad = _squadFactory.CreateSquad(1, _playerSpawnPoints[0].position, _playerSpawnPoints[0].rotation);
            
            if (newSquad != null)
            {
                if (isEnemy)
                {
                    _enemySquads.Add(newSquad);
                }
                else
                {
                    _playerSquads.Add(newSquad);
                }
                
                CollectUnitsFromSquad(newSquad);
                OnSquadSpawned?.Invoke(newSquad);
                
                Debug.Log($"GameManager: Created {(isEnemy ? "enemy" : "player")} squad {newSquad.SquadId} with {unitType} units");
            }
            
            return newSquad;
        }

        /// <summary>
        /// Get squad by ID
        /// </summary>
        public SquadModel GetSquad(int squadId)
        {
            if (_squadFactory != null)
            {
                return _squadFactory.GetSquad(squadId);
            }
            
            return null;
        }

        /// <summary>
        /// Destroy a squad
        /// </summary>
        public void DestroySquad(int squadId)
        {
            if (_squadFactory == null) return;
            
            SquadModel squad = _squadFactory.GetSquad(squadId);
            if (squad != null)
            {
                _playerSquads.Remove(squad);
                _enemySquads.Remove(squad);
                foreach (var unit in squad.Units)
                {
                    _allUnits.Remove(unit);
                }
                _squadFactory.DisbandSquad(squadId);
                OnSquadDestroyed?.Invoke(squad);
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup all game resources
        /// </summary>
        private void CleanupGame()
        {
            Debug.Log("GameManager: Cleaning up game resources...");
            
            // Clear tracking lists
            _playerSquads.Clear();
            _enemySquads.Clear();
            _allUnits.Clear();
            
            // Cleanup factories
            if (_squadFactory != null)
            {
                // SquadFactory should handle its own cleanup
            }
            
            if (_unitFactory != null)
            {
                // UnitFactory should handle its own cleanup
            }
            
            // Reset time scale
            Time.timeScale = 1f;
            
            // Reset progress
            _gameProgressPercentage = 0f;
            
            Debug.Log("GameManager: Cleanup completed");
        }

        #endregion

        #region Debug Tools

        [Button("Force Initialize Game"), TitleGroup("Debug Tools")]
        public void ForceInitializeGame()
        {
            if (_currentGameState == GameState.NotInitialized)
            {
                StartCoroutine(InitializeGame());
            }
        }

        [Button("Force Start Game"), TitleGroup("Debug Tools")]
        public void ForceStartGame()
        {
            if (_currentGameState != GameState.Playing)
            {
                StartGame();
            }
        }

        [Button("Create Test Squad"), TitleGroup("Debug Tools")]
        public void CreateTestSquad()
        {
            CreateSquad(UnitType.Infantry, Vector3.zero, false);
        }

        [Button("Show Game Stats"), TitleGroup("Debug Tools")]
        public void ShowGameStats()
        {
            string stats = "=== Game Manager Statistics ===\n";
            stats += $"Game State: {_currentGameState}\n";
            stats += $"Player Squads: {_playerSquads.Count}\n";
            stats += $"Enemy Squads: {_enemySquads.Count}\n";
            stats += $"Total Units: {_allUnits.Count}\n";
            stats += $"Game Progress: {_gameProgressPercentage * 100:F1}%\n";
            
            Debug.Log(stats);
        }

        #endregion
    }

    /// <summary>
    /// Enum defining different game states
    /// </summary>
    public enum GameState
    {
        NotInitialized,
        Initializing,
        Initialized,
        LoadingData,
        DataLoaded,
        SpawningSquads,
        SquadsSpawned,
        Playing,
        Paused,
        Completed,
        Failed
    }
}