using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Core.Data;
using VikingRaven.Game.DefineData;
using VikingRaven.Units.Components;
using VikingRaven.Units.Models;
using VikingRaven.Units.Systems;

namespace VikingRaven.Game
{
    public class OptimizedGameManager : MonoBehaviour
    {
        #region Game State Management
        
        [TitleGroup("Game State")]
        [Tooltip("Current state of the game")]
        [SerializeField, ReadOnly, EnumToggleButtons] 
        private GameState _currentGameState = GameState.NotInitialized;
        
        [Tooltip("Enable automatic game progression")]
        [SerializeField, ToggleLeft]
        private bool _autoProgressGameStates = true;
        
        [Tooltip("Time between state transitions")]
        [SerializeField, Range(0.1f, 5f)]
        private float _stateTransitionDelay = 1f;

        #endregion

        #region Core Systems (Optimized Dependencies)
        
        [TitleGroup("Core Systems")]
        [Tooltip("Optimized system registry")]
        [SerializeField, Required]
        private SystemRegistry _optimizedSystemRegistry;
        
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

        #endregion

        #region Game Configuration
        
        [TitleGroup("Game Configuration")]
        [Tooltip("Number of player squads to spawn")]
        [SerializeField, Range(1, 5), ProgressBar(1, 5)]
        private int _initialPlayerSquadCount = 2;
        
        [Tooltip("Number of enemy squads to spawn")]
        [SerializeField, Range(1, 5), ProgressBar(1, 5)]
        private int _initialEnemySquadCount = 2;

        #endregion

        #region Reactive Update Configuration
        
        [TitleGroup("Reactive Updates")]
        [Tooltip("Frequency to check game conditions")]
        [SerializeField, Range(1, 10)]
        private float _gameConditionCheckInterval = 2f;
        
        [Tooltip("Frequency to update game statistics")]
        [SerializeField, Range(1, 30)]
        private float _statisticsUpdateInterval = 5f;
        
        [Tooltip("Enable performance monitoring")]
        [SerializeField, ToggleLeft]
        private bool _enablePerformanceMonitoring = true;
        
        [Tooltip("Performance monitoring interval")]
        [SerializeField, Range(1, 60)]
        private float _performanceMonitoringInterval = 10f;

        #endregion

        #region Spawn Configuration
        
        [TitleGroup("Spawn Settings")]
        [Tooltip("Player spawn positions")]
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true)]
        private List<Transform> _playerSpawnPoints = new List<Transform>();
        
        [Tooltip("Enemy spawn positions")]
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true)]
        private List<Transform> _enemySpawnPoints = new List<Transform>();
        
        [Tooltip("Player unit types")]
        [SerializeField, EnumToggleButtons]
        private List<UnitType> _playerUnitTypes = new List<UnitType> { UnitType.Infantry, UnitType.Archer };
        
        [Tooltip("Enemy unit types")]
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
        private HashSet<UnitModel> _allUnits = new HashSet<UnitModel>();
        
        [ShowInInspector, ReadOnly, ProgressBar(0, 1)]
        private float _gameProgressPercentage = 0f;
        
        [ShowInInspector, ReadOnly]
        private GameStatistics _gameStatistics = new GameStatistics();

        #endregion

        #region Performance Data
        
        [TitleGroup("Performance Metrics")]
        [ShowInInspector, ReadOnly]
        private float _averageFrameTime = 0f;
        
        [ShowInInspector, ReadOnly]
        private int _totalManagedEntities = 0;
        
        [ShowInInspector, ReadOnly]
        private float _memoryUsageMB = 0f;

        #endregion

        #region Private Fields
        
        private readonly Dictionary<SquadModel, List<System.Action>> _squadEventSubscriptions = new Dictionary<SquadModel, List<System.Action>>();
        
        private Coroutine _gameConditionCheckCoroutine;
        private Coroutine _statisticsUpdateCoroutine;
        private Coroutine _performanceMonitoringCoroutine;
        private Coroutine _gameInitializationCoroutine;
        
        // Performance tracking
        private readonly Queue<float> _frameTimeHistory = new Queue<float>();
        private const int MAX_FRAME_HISTORY = 60;

        #endregion

        #region Events (Event-Driven Architecture)
        
        public event Action<GameState> OnGameStateChanged;
        public event Action<SquadModel> OnSquadSpawned;
        public event Action<SquadModel> OnSquadDestroyed;
        public event Action<GameStatistics> OnStatisticsUpdated;
        public event Action OnGameCompleted;
        public event Action OnGameFailed;
        public event Action<float> OnGameProgressChanged;

        #endregion

        #region Properties
        
        public GameState CurrentGameState => _currentGameState;
        public bool IsGameActive => _currentGameState == GameState.Playing;
        public bool IsGameInitialized => _currentGameState != GameState.NotInitialized;
        public List<SquadModel> PlayerSquads => new List<SquadModel>(_playerSquads);
        public List<SquadModel> EnemySquads => new List<SquadModel>(_enemySquads);
        public int TotalUnitsCount => _allUnits.Count;
        public GameStatistics Statistics => _gameStatistics;
        
        // Optimized system access
        public SystemRegistry SystemRegistry => _optimizedSystemRegistry;
        public DataManager DataManager => _dataManager;
        public UnitFactory UnitFactory => _unitFactory;
        public SquadFactory SquadFactory => _squadFactory;
        public EntityRegistry EntityRegistry => _entityRegistry;

        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            ValidateReferences();
            InitializeEventSubscriptions();
        }
        
        private void Start()
        {
            if (_autoProgressGameStates)
            {
                _gameInitializationCoroutine = StartCoroutine(AutoProgressGameStatesCoroutine());
            }
        }
        
        private void OnDestroy()
        {
            StopAllCoroutines();
            CleanupEventSubscriptions();
            CleanupGame();
        }

        #endregion

        #region Initialization (Event-Driven)
        
        /// <summary>
        /// Start game initialization
        /// </summary>
        [Button("Initialize Game"), TitleGroup("Initialization")]
        public void InitializeGame()
        {
            if (_currentGameState != GameState.NotInitialized)
            {
                Debug.LogWarning("OptimizedGameManager: Game is already initialized or in progress!");
                return;
            }
            
            ChangeGameState(GameState.Initializing);
            _gameInitializationCoroutine = StartCoroutine(InitializeGameCoroutine());
        }
        
        /// <summary>
        /// Validate tất cả dependencies và setup event subscriptions
        /// </summary>
        private void ValidateReferences()
        {
            bool hasErrors = false;
            
            if (_optimizedSystemRegistry == null)
            {
                Debug.LogError("OptimizedGameManager: OptimizedSystemRegistry is missing!");
                hasErrors = true;
            }
            
            if (_dataManager == null)
            {
                Debug.LogError("OptimizedGameManager: DataManager is missing!");
                hasErrors = true;
            }
            
            if (_unitFactory == null)
            {
                Debug.LogError("OptimizedGameManager: UnitFactory is missing!");
                hasErrors = true;
            }
            
            if (_squadFactory == null)
            {
                Debug.LogError("OptimizedGameManager: SquadFactory is missing!");
                hasErrors = true;
            }
            
            if (_entityRegistry == null)
            {
                Debug.LogError("OptimizedGameManager: EntityRegistry is missing!");
                hasErrors = true;
            }
            
            if (hasErrors)
            {
                Debug.LogError("OptimizedGameManager: Critical references missing!");
                return;
            }
            
            Debug.Log("OptimizedGameManager: All references validated successfully");
        }
        
        /// <summary>
        /// Khởi tạo event subscriptions cho reactive architecture
        /// </summary>
        private void InitializeEventSubscriptions()
        {
            if (_optimizedSystemRegistry != null)
            {
                _optimizedSystemRegistry.OnAllSystemsInitialized += OnSystemsInitialized;
                _optimizedSystemRegistry.OnSystemUpdateCompleted += OnSystemUpdateCompleted;
            }
            
            if (_squadFactory != null)
            {
                _squadFactory.OnSquadCreated += OnSquadCreatedHandler;
                _squadFactory.OnSquadDisbanded += OnSquadDisbandedHandler;
            }
        }
        
        /// <summary>
        /// Auto progression qua các game states sử dụng coroutine
        /// </summary>
        private IEnumerator AutoProgressGameStatesCoroutine()
        {
            yield return StartCoroutine(InitializeGameCoroutine());
            
            if (_currentGameState == GameState.Initialized)
            {
                yield return new WaitForSeconds(_stateTransitionDelay);
                yield return StartCoroutine(LoadGameDataCoroutine());
            }
            
            if (_currentGameState == GameState.DataLoaded)
            {
                yield return new WaitForSeconds(_stateTransitionDelay);
                yield return StartCoroutine(SpawnInitialSquadsCoroutine());
            }
            
            if (_currentGameState == GameState.SquadsSpawned)
            {
                yield return new WaitForSeconds(_stateTransitionDelay);
                StartGameReactive();
            }
        }
        
        /// <summary>
        /// 
        private IEnumerator InitializeGameCoroutine()
        {
            Debug.Log("GameManager: Initializing game systems...");
            ChangeGameState(GameState.Initializing);
            
            // Initialize DataManager
            if (_dataManager != null && !_dataManager.IsInitialized)
            {
                _dataManager.Initialize();
                yield return new WaitForSeconds(0.1f);
            }
            
            // modidy EntityRegistry initialization
            if (_entityRegistry != null)
            {
                Debug.Log("OptimizedGameManager: EntityRegistry ready");
            }
            
            // Wait for OptimizedSystemRegistry to complete initialization
            if (_optimizedSystemRegistry != null)
            {
                // OptimizedSystemRegistry will initialize itself and call OnSystemsInitialized when ready
                while (!_optimizedSystemRegistry.HasSystem<MovementSystem>()) // Wait for at least one system
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            InitializeFactories();
            
            ChangeGameState(GameState.Initialized);
            Debug.Log("OptimizedGameManager: Game systems initialized successfully");
        }
        
        /// <summary>
        /// Initialize factories với dependencies
        /// </summary>
        private void InitializeFactories()
        {
            Debug.Log("OptimizedGameManager: Factories initialized");
        }

        #endregion

        #region Data Loading
        
        /// <summary>
        /// Load game data sử dụng coroutine
        /// </summary>
        private IEnumerator LoadGameDataCoroutine()
        {
            Debug.Log("OptimizedGameManager: Loading game data...");
            ChangeGameState(GameState.LoadingData);
            
            if (_dataManager != null)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            ChangeGameState(GameState.DataLoaded);
            Debug.Log("OptimizedGameManager: Game data loaded successfully");
        }

        #endregion

        #region Squad Spawning (Optimized)
        
        /// <summary>
        /// Spawn initial squads với coroutine và event handling
        /// </summary>
        private IEnumerator SpawnInitialSquadsCoroutine()
        {
            Debug.Log("OptimizedGameManager: Spawning initial squads...");
            ChangeGameState(GameState.SpawningSquads);
            
            // Spawn player squads
            for (int i = 0; i < _initialPlayerSquadCount; i++)
            {
                yield return StartCoroutine(SpawnPlayerSquadCoroutine(i));
                yield return new WaitForSeconds(0.2f);
            }
            
            // Spawn enemy squads
            for (int i = 0; i < _initialEnemySquadCount; i++)
            {
                yield return StartCoroutine(SpawnEnemySquadCoroutine(i));
                yield return new WaitForSeconds(0.2f);
            }
            
            ChangeGameState(GameState.SquadsSpawned);
            Debug.Log($"OptimizedGameManager: Spawned {_playerSquads.Count} player squads and {_enemySquads.Count} enemy squads");
        }
        
        /// <summary>
        /// Spawn player squad với async approach
        /// </summary>
        private IEnumerator SpawnPlayerSquadCoroutine(int squadIndex)
        {
            Vector3 spawnPosition = GetPlayerSpawnPosition(squadIndex);
            Quaternion spawnRotation = GetPlayerSpawnRotation(squadIndex);
            
            SquadModel newSquad = _squadFactory.CreateSquad(1, spawnPosition, spawnRotation);
            
            if (newSquad != null)
            {
                _playerSquads.Add(newSquad);
                RegisterSquadEvents(newSquad);
                CollectUnitsFromSquad(newSquad);
                
                // Event sẽ được trigger tự động qua OnSquadCreatedHandler
                Debug.Log($"OptimizedGameManager: Spawned player squad {newSquad.SquadId} with {newSquad.UnitCount} units");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Spawn enemy squad với async approach
        /// </summary>
        private IEnumerator SpawnEnemySquadCoroutine(int squadIndex)
        {
            Vector3 spawnPosition = GetEnemySpawnPosition(squadIndex);
            Quaternion spawnRotation = GetEnemySpawnRotation(squadIndex);
            
            SquadModel newSquad = _squadFactory.CreateSquad(1, spawnPosition, spawnRotation);
            
            if (newSquad != null)
            {
                _enemySquads.Add(newSquad);
                RegisterSquadEvents(newSquad);
                CollectUnitsFromSquad(newSquad);
                
                Debug.Log($"OptimizedGameManager: Spawned enemy squad {newSquad.SquadId} with {newSquad.UnitCount} units");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Register events cho squad để reactive handling
        /// </summary>
        private void RegisterSquadEvents(SquadModel squad)
        {
            if (squad == null) return;
            
            var eventSubscriptions = new List<System.Action>();
            
            // Subscribe to squad events
            System.Action<UnitModel> onUnitDied = (unit) => OnUnitDiedHandler(squad, unit);
            System.Action<FormationType> onFormationChanged = (formation) => OnSquadFormationChanged(squad, formation);
            System.Action<bool> onCombatStateChanged = (inCombat) => OnSquadCombatStateChanged(squad, inCombat);
            
            squad.OnUnitDied += onUnitDied;
            squad.OnFormationChanged += onFormationChanged;
            squad.OnCombatStateChanged += onCombatStateChanged;
            
            // Store subscriptions for cleanup
            eventSubscriptions.Add(() => squad.OnUnitDied -= onUnitDied);
            eventSubscriptions.Add(() => squad.OnFormationChanged -= onFormationChanged);
            eventSubscriptions.Add(() => squad.OnCombatStateChanged -= onCombatStateChanged);
            
            _squadEventSubscriptions[squad] = eventSubscriptions;
        }

        #endregion

        #region Reactive Game Flow (Thay thế Update)
        public void StartGameReactive()
        {
            Debug.Log("OptimizedGameManager: Starting reactive game...");
            ChangeGameState(GameState.Playing);
            
            // Start reactive update coroutines
            StartReactiveUpdateCoroutines();
        }
        
        /// <summary>
        /// Start các coroutines cho reactive updates
        /// </summary>
        private void StartReactiveUpdateCoroutines()
        {
            // Game condition checking (thay thế Update logic)
            _gameConditionCheckCoroutine = StartCoroutine(GameConditionCheckCoroutine());
            
            // Statistics updates
            _statisticsUpdateCoroutine = StartCoroutine(StatisticsUpdateCoroutine());
            
            // Performance monitoring
            if (_enablePerformanceMonitoring)
            {
                _performanceMonitoringCoroutine = StartCoroutine(PerformanceMonitoringCoroutine());
            }
        }
        
        private IEnumerator GameConditionCheckCoroutine()
        {
            var waitTime = new WaitForSeconds(_gameConditionCheckInterval);
            
            while (_currentGameState == GameState.Playing)
            {
                CheckGameConditionsReactive();
                UpdateGameProgressReactive();
                yield return waitTime;
            }
        }
        
        private IEnumerator StatisticsUpdateCoroutine()
        {
            var waitTime = new WaitForSeconds(_statisticsUpdateInterval);
            
            while (_currentGameState == GameState.Playing)
            {
                UpdateGameStatistics();
                OnStatisticsUpdated?.Invoke(_gameStatistics);
                yield return waitTime;
            }
        }
        
        private IEnumerator PerformanceMonitoringCoroutine()
        {
            var waitTime = new WaitForSeconds(_performanceMonitoringInterval);
            
            while (_currentGameState == GameState.Playing)
            {
                UpdatePerformanceMetrics();
                yield return waitTime;
            }
        }

        #endregion

        #region Event Handlers (Reactive Logic)
        
        private void OnSystemsInitialized()
        {
            Debug.Log("OptimizedGameManager: All systems initialized via event");
        }
        
        private void OnSystemUpdateCompleted()
        {
            
        }
        
        private void OnSquadCreatedHandler(SquadModel squad)
        {
            OnSquadSpawned?.Invoke(squad);
        }
        
        /// <summary>
        /// Handler khi squad bị disbanded
        /// </summary>
        private void OnSquadDisbandedHandler(SquadModel squad)
        {
            OnSquadDestroyed?.Invoke(squad);
            UnregisterSquadEvents(squad);
        }
        
        /// <summary>
        /// Handler khi unit chết
        /// </summary>
        private void OnUnitDiedHandler(SquadModel squad, UnitModel unit)
        {
            _allUnits.Remove(unit);
            _gameStatistics.TotalDeaths++;
            
            // Check nếu squad không còn viable
            if (!squad.IsViable())
            {
                if (_playerSquads.Contains(squad))
                {
                    _playerSquads.Remove(squad);
                }
                else if (_enemySquads.Contains(squad))
                {
                    _enemySquads.Remove(squad);
                }
                
                // Reactive check cho game end conditions
                CheckGameConditionsReactive();
            }
        }
        
        /// <summary>
        /// Handler khi squad thay đổi formation
        /// </summary>
        private void OnSquadFormationChanged(SquadModel squad, FormationType formation)
        {
            _gameStatistics.FormationChanges++;
            Debug.Log($"OptimizedGameManager: Squad {squad.SquadId} changed formation to {formation}");
        }
        
        /// <summary>
        /// Handler khi squad combat state thay đổi
        /// </summary>
        private void OnSquadCombatStateChanged(SquadModel squad, bool inCombat)
        {
            if (inCombat)
            {
                _gameStatistics.CombatEngagements++;
            }
        }

        #endregion

        #region Game Logic (Optimized Reactive)
        
        /// <summary>
        /// Check game conditions reactively thay vì mỗi frame
        /// </summary>
        private void CheckGameConditionsReactive()
        {
            // Check victory condition
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
            
            // Check defeat condition
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
        /// Update game progress reactively
        /// </summary>
        private void UpdateGameProgressReactive()
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
            
            float newProgress = (float)destroyedEnemySquads / totalEnemySquads;
            
            if (Mathf.Abs(newProgress - _gameProgressPercentage) > 0.01f)
            {
                _gameProgressPercentage = newProgress;
                OnGameProgressChanged?.Invoke(_gameProgressPercentage);
            }
        }
        
        /// <summary>
        /// Update game statistics
        /// </summary>
        private void UpdateGameStatistics()
        {
            _gameStatistics.PlayTime = Time.time;
            _gameStatistics.TotalUnits = _allUnits.Count;
            _gameStatistics.ActivePlayerSquads = _playerSquads.Count;
            _gameStatistics.ActiveEnemySquads = _enemySquads.Count;
            
            // Calculate average squad health
            float totalHealth = 0f;
            int squadCount = 0;
            
            foreach (var squad in _playerSquads)
            {
                if (squad != null && squad.IsViable())
                {
                    totalHealth += squad.GetAverageHealthPercentage();
                    squadCount++;
                }
            }
            
            _gameStatistics.AverageSquadHealth = squadCount > 0 ? totalHealth / squadCount : 0f;
        }
        
        /// <summary>
        /// Update performance metrics
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            // Frame time tracking
            _frameTimeHistory.Enqueue(Time.unscaledDeltaTime * 1000f);
            if (_frameTimeHistory.Count > MAX_FRAME_HISTORY)
            {
                _frameTimeHistory.Dequeue();
            }
            
            if (_frameTimeHistory.Count > 0)
            {
                float sum = 0f;
                foreach (float time in _frameTimeHistory)
                {
                    sum += time;
                }
                _averageFrameTime = sum / _frameTimeHistory.Count;
            }
            
            // Entity count
            _totalManagedEntities = _allUnits.Count;
            
            // Memory usage (approximation)
            _memoryUsageMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
        }

        #endregion

        #region Helper Methods
        
        private Vector3 GetPlayerSpawnPosition(int squadIndex)
        {
            if (_playerSpawnPoints.Count > 0)
            {
                int spawnIndex = squadIndex % _playerSpawnPoints.Count;
                return _playerSpawnPoints[spawnIndex].position;
            }
            return new Vector3(-10f + squadIndex * 5f, 0, 0);
        }
        
        private Quaternion GetPlayerSpawnRotation(int squadIndex)
        {
            if (_playerSpawnPoints.Count > 0)
            {
                int spawnIndex = squadIndex % _playerSpawnPoints.Count;
                return _playerSpawnPoints[spawnIndex].rotation;
            }
            return Quaternion.identity;
        }
        
        private Vector3 GetEnemySpawnPosition(int squadIndex)
        {
            if (_enemySpawnPoints.Count > 0)
            {
                int spawnIndex = squadIndex % _enemySpawnPoints.Count;
                return _enemySpawnPoints[spawnIndex].position;
            }
            return new Vector3(10f + squadIndex * 5f, 0, 0);
        }
        
        private Quaternion GetEnemySpawnRotation(int squadIndex)
        {
            if (_enemySpawnPoints.Count > 0)
            {
                int spawnIndex = squadIndex % _enemySpawnPoints.Count;
                return _enemySpawnPoints[spawnIndex].rotation;
            }
            return Quaternion.identity;
        }
        
        private void CollectUnitsFromSquad(SquadModel squad)
        {
            if (squad == null) return;
            
            foreach (var unit in squad.Units)
            {
                if (unit != null)
                {
                    _allUnits.Add(unit);
                }
            }
        }

        #endregion

        #region Game Flow Control
        
        public void PauseGame()
        {
            if (_currentGameState == GameState.Playing)
            {
                ChangeGameState(GameState.Paused);
                
                // Pause system registry
                if (_optimizedSystemRegistry != null)
                {
                    _optimizedSystemRegistry.PauseUpdates();
                }
                
                Time.timeScale = 0f;
                Debug.Log("OptimizedGameManager: Game paused");
            }
        }
        
        public void ResumeGame()
        {
            if (_currentGameState == GameState.Paused)
            {
                ChangeGameState(GameState.Playing);
                
                // Resume system registry
                if (_optimizedSystemRegistry != null)
                {
                    _optimizedSystemRegistry.ResumeUpdates();
                }
                
                Time.timeScale = 1f;
                StartReactiveUpdateCoroutines();
                Debug.Log("OptimizedGameManager: Game resumed");
            }
        }
        
        public void CompleteGame()
        {
            Debug.Log("OptimizedGameManager: Game completed successfully!");
            ChangeGameState(GameState.Completed);
            StopReactiveUpdates();
            OnGameCompleted?.Invoke();
        }
        
        public void FailGame()
        {
            Debug.Log("OptimizedGameManager: Game failed!");
            ChangeGameState(GameState.Failed);
            StopReactiveUpdates();
            OnGameFailed?.Invoke();
        }
        
        /// <summary>
        /// Stop tất cả reactive update coroutines
        /// </summary>
        private void StopReactiveUpdates()
        {
            if (_gameConditionCheckCoroutine != null)
            {
                StopCoroutine(_gameConditionCheckCoroutine);
                _gameConditionCheckCoroutine = null;
            }
            
            if (_statisticsUpdateCoroutine != null)
            {
                StopCoroutine(_statisticsUpdateCoroutine);
                _statisticsUpdateCoroutine = null;
            }
            
            if (_performanceMonitoringCoroutine != null)
            {
                StopCoroutine(_performanceMonitoringCoroutine);
                _performanceMonitoringCoroutine = null;
            }
        }

        #endregion

        #region State Management
        
        private void ChangeGameState(GameState newState)
        {
            if (_currentGameState == newState) return;
            
            GameState oldState = _currentGameState;
            _currentGameState = newState;
            
            Debug.Log($"OptimizedGameManager: State changed from {oldState} to {newState}");
            OnGameStateChanged?.Invoke(newState);
        }

        #endregion

        #region Cleanup (Event-Driven)
        
        /// <summary>
        /// Cleanup event subscriptions
        /// </summary>
        private void CleanupEventSubscriptions()
        {
            if (_optimizedSystemRegistry != null)
            {
                _optimizedSystemRegistry.OnAllSystemsInitialized -= OnSystemsInitialized;
                _optimizedSystemRegistry.OnSystemUpdateCompleted -= OnSystemUpdateCompleted;
            }
            
            if (_squadFactory != null)
            {
                _squadFactory.OnSquadCreated -= OnSquadCreatedHandler;
                _squadFactory.OnSquadDisbanded -= OnSquadDisbandedHandler;
            }
            
            // Cleanup squad event subscriptions
            foreach (var kvp in _squadEventSubscriptions)
            {
                foreach (var cleanup in kvp.Value)
                {
                    cleanup?.Invoke();
                }
            }
            _squadEventSubscriptions.Clear();
        }
        
        /// <summary>
        /// Unregister events cho specific squad
        /// </summary>
        private void UnregisterSquadEvents(SquadModel squad)
        {
            if (_squadEventSubscriptions.TryGetValue(squad, out var eventSubscriptions))
            {
                foreach (var cleanup in eventSubscriptions)
                {
                    cleanup?.Invoke();
                }
                _squadEventSubscriptions.Remove(squad);
            }
        }
        
        /// <summary>
        /// Cleanup tất cả game resources
        /// </summary>
        private void CleanupGame()
        {
            Debug.Log("OptimizedGameManager: Cleaning up game resources...");
            
            // Stop reactive updates
            StopReactiveUpdates();
            
            // Clear tracking collections
            _playerSquads.Clear();
            _enemySquads.Clear();
            _allUnits.Clear();
            
            // Reset time scale
            Time.timeScale = 1f;
            
            // Reset statistics
            _gameStatistics = new GameStatistics();
            _gameProgressPercentage = 0f;
            
            Debug.Log("OptimizedGameManager: Cleanup completed");
        }

        #endregion

        #region Debug Tools
        
        [Button("Force Start Reactive Game"), TitleGroup("Debug Tools")]
        public void ForceStartReactiveGame()
        {
            if (_currentGameState != GameState.Playing)
            {
                StartGameReactive();
            }
        }
        
        [Button("Show Performance Stats"), TitleGroup("Debug Tools")]
        public void ShowPerformanceStats()
        {
            string stats = "=== Optimized GameManager Performance ===\n";
            stats += $"Game State: {_currentGameState}\n";
            stats += $"Average Frame Time: {_averageFrameTime:F2}ms\n";
            stats += $"Total Managed Entities: {_totalManagedEntities}\n";
            stats += $"Memory Usage: {_memoryUsageMB:F1}MB\n";
            stats += $"Player Squads: {_playerSquads.Count}\n";
            stats += $"Enemy Squads: {_enemySquads.Count}\n";
            stats += $"Total Units: {_allUnits.Count}\n";
            stats += $"Game Progress: {_gameProgressPercentage * 100:F1}%\n";
            
            Debug.Log(stats);
        }
        
        [Button("Test Reactive Events"), TitleGroup("Debug Tools")]
        public void TestReactiveEvents()
        {
            OnGameProgressChanged?.Invoke(0.5f);
            OnStatisticsUpdated?.Invoke(_gameStatistics);
            Debug.Log("OptimizedGameManager: Reactive events tested");
        }

        #endregion
    }
    
    /// <summary>
    /// Game statistics data structure
    /// </summary>
    [Serializable]
    public class GameStatistics
    {
        public float PlayTime;
        public int TotalUnits;
        public int TotalDeaths;
        public int CombatEngagements;
        public int FormationChanges;
        public int ActivePlayerSquads;
        public int ActiveEnemySquads;
        public float AverageSquadHealth;
    }
}