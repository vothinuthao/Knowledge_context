using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Components;
using Core.ECS;
using Core.Grid;
using Data;
using Components.Squad;
using Core.Singleton;
using Squad;
using Systems.Movement;
using Systems.Squad;

namespace Managers
{
    /// <summary>
    /// Main game manager that coordinates all game systems
    /// </summary>
    public class GameManager : ManualSingletonMono<GameManager>
    {

        #region Inspector Fields
        [Header("Core References")]
        [SerializeField] private WorldManager _worldManager;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private UIManager _uiManager;
        
        [Header("Configuration")]
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private SquadConfig _defaultSquadConfig;
        [SerializeField] private TroopConfig _defaultTroopConfig;
        
        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;
        [SerializeField] private bool _showPerformanceOverlay = false;
        #endregion

        #region Private Fields
        private GameState _currentState = GameState.INITIALIZING;
        private Dictionary<int, SquadData> _squadData = new Dictionary<int, SquadData>();
        private List<Entity> _activeSquads = new List<Entity>();
        private float _gameTime = 0f;
        private bool _isPaused = false;
        #endregion
        
        #region Public Fields
        public GameState CurrentState => _currentState;
        #endregion

        #region Events
        public event Action<GameState, GameState> OnGameStateChanged;
        public event Action<Entity> OnSquadCreated;
        public event Action<Entity> OnSquadDestroyed;
        public event Action<Entity, Entity> OnCombatStarted;
        public event Action<Entity, Entity> OnCombatEnded;
        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
           base.Awake();
            ValidateReferences();
        }
        
        private void Start()
        {
            if (_currentState == GameState.INITIALIZING)
            {
                InitializeGame();
            }
            Invoke(nameof(VerifySystems), 0.5f);
        }

        private void Update()
        {
            if (_isPaused) return;
            
            _gameTime += Time.deltaTime;
            UpdateGameState();
            
            if (_debugMode)
            {
                HandleDebugInput();
            }
        }
        #endregion

        #region Initialization
        private void ValidateReferences()
        {
        }
        
        private void InitializeGame()
        {
            try
            {
                SetGameState(GameState.INITIALIZING);
                
                // Initialize managers
                InitializeManagers();
                
                // Load game data
                LoadGameData();
                
                // Setup initial game state
                SetupInitialState();
                
                SetGameState(GameState.READY);
                
                // Auto-start if configured
                if (_gameConfig.AutoStartGame)
                {
                    StartGame();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Game initialization failed: {e.Message}");
                SetGameState(GameState.ERROR);
            }
        }
        
        private void InitializeManagers()
        {
            // Initialize world if needed
            if (_worldManager && _worldManager.World == null)
            {
                _worldManager.InitializeWorld();
            }
            
            // Initialize grid
            if (_gridManager)
            {
                _gridManager.InitializeGrid();
            }
            
            // Setup input handlers
            if (_inputManager)
            {
                _inputManager.OnSquadSelected += HandleSquadSelected;
                _inputManager.OnCommandIssued += HandleCommandIssued;
            }
        }
        
        private void LoadGameData()
        {
            // Load configs, saved data, etc.
            Debug.Log("Loading game data...");
        }
        
        private void SetupInitialState()
        {
            // Create initial squads for testing
            if (_gameConfig.CreateTestSquads)
            {
                CreateTestSquads();
            }
        }
        #endregion

        #region Game State Management
        public void SetGameState(GameState newState)
        {
            if (_currentState == newState) return;
            
            GameState oldState = _currentState;
            _currentState = newState;
            
            Debug.Log($"Game state changed: {oldState} -> {newState}");
            
            OnGameStateChanged?.Invoke(oldState, newState);
            
            // Handle state-specific logic
            switch (newState)
            {
                case GameState.PLAYING:
                    Time.timeScale = 1f;
                    break;
                    
                case GameState.PAUSED:
                    Time.timeScale = 0f;
                    break;
                    
                case GameState.GAME_OVER:
                    HandleGameOver();
                    break;
            }
        }
        
        private void UpdateGameState()
        {
            switch (_currentState)
            {
                case GameState.PLAYING:
                    UpdateGameplay();
                    break;
                    
                case GameState.COMBAT:
                    UpdateCombat();
                    break;
            }
        }
        
        private void UpdateGameplay()
        {
            // Update game logic
            CheckWinConditions();
            CheckLoseConditions();
        }
        
        private void UpdateCombat()
        {
            // Update combat logic
        }
        #endregion

        #region Squad Management
        public Entity CreateSquad(SquadConfig config, Vector3 position, Faction faction = Faction.PLAYER)
        {
            if (!config) config = _defaultSquadConfig;
            Entity squadEntity = _worldManager.CreateSquad(config, position);
            
            squadEntity.AddComponent(new FactionComponent { Faction = faction });
            
            var squadData = new SquadData
            {
                Entity = squadEntity,
                Config = config,
                Faction = faction,
                CreationTime = _gameTime
            };
            
            _squadData[squadEntity.Id] = squadData;
            _activeSquads.Add(squadEntity);
            PopulateSquad(squadEntity, config);
            OnSquadCreated?.Invoke(squadEntity);
            Debug.Log($"Created squad {squadEntity.Id} at {position} for faction {faction}");
            
            return squadEntity;
        }
        
        private void PopulateSquad(Entity squadEntity, SquadConfig config)
        {
            var squadComponent = squadEntity.GetComponent<SquadComponent>();
            var squadPosition = squadEntity.GetComponent<PositionComponent>().Position;
    
            // Tạo SquadFormationComponent nếu chưa có
            if (!squadEntity.HasComponent<SquadFormationComponent>())
            {
                squadEntity.AddComponent(new SquadFormationComponent(3, 3, 1.5f));
            }
    
            for (int i = 0; i < config.MaxTroops; i++)
            {
                Entity troopEntity = CreateTroop(_defaultTroopConfig, squadPosition, squadEntity.Id);
                squadComponent.AddMember(troopEntity.Id);
        
                // Update troop component
                var troopComponent = troopEntity.GetComponent<TroopComponent>();
                troopComponent.FormationIndex = i;
            }
    
            // Update formation
            squadComponent.UpdateFormation();
        }
        
        public Entity CreateTroop(TroopConfig config, Vector3 position, int squadId = -1)
        {
            return _worldManager.CreateTroop(config, position, squadId);
        }
        
        public void DestroySquad(Entity squadEntity)
        {
            if (!_squadData.ContainsKey(squadEntity.Id)) return;
            
            var squadComponent = squadEntity.GetComponent<SquadComponent>();
            
            // Destroy all troops first
            foreach (int troopId in squadComponent.MemberIds.ToArray())
            {
                Entity troopEntity = _worldManager.World.GetEntityById(troopId);
                if (troopEntity != null)
                {
                    _worldManager.World.DestroyEntity(troopEntity);
                }
            }
            
            // Remove from tracking
            _squadData.Remove(squadEntity.Id);
            _activeSquads.Remove(squadEntity);
            
            // Destroy squad entity
            _worldManager.World.DestroyEntity(squadEntity);
            
            OnSquadDestroyed?.Invoke(squadEntity);
        }
        #endregion

        #region Game Flow
        public void StartGame()
        {
            if (_currentState != GameState.READY) return;
            
            SetGameState(GameState.PLAYING);
            Debug.Log("Game started!");
        }
        
        public void PauseGame()
        {
            if (_currentState != GameState.PLAYING) return;
            
            _isPaused = true;
            SetGameState(GameState.PAUSED);
        }
        
        public void ResumeGame()
        {
            if (_currentState != GameState.PAUSED) return;
            
            _isPaused = false;
            SetGameState(GameState.PLAYING);
        }
        
        public void RestartGame()
        {
            // Clear current game state
            ClearGameState();
            
            // Reinitialize
            InitializeGame();
        }
        
        private void ClearGameState()
        {
            // Destroy all squads
            foreach (var squad in _activeSquads.ToArray())
            {
                DestroySquad(squad);
            }
            
            // Clear data
            _squadData.Clear();
            _activeSquads.Clear();
            
            // Reset world
            if (_worldManager != null && _worldManager.World != null)
            {
                _worldManager.World.Clear();
            }
            
            // Reset grid
            // if (_gridManager != null)
            // {
            //     _gridManager.ResetGrid();
            // }
            
            _gameTime = 0f;
        }
        #endregion

        #region Input Handling
        private void HandleSquadSelected(Entity squad)
        {
            if (_currentState != GameState.PLAYING) return;
            
            // Handle squad selection logic
            Debug.Log($"Squad {squad.Id} selected");
        }
        
        private void HandleCommandIssued(Command command)
        {
            if (_currentState != GameState.PLAYING) return;
        
            switch (command.Type)
            {
                case CommandType.MOVE:
                    HandleMoveCommand(command);
                    break;
                
                case CommandType.ATTACK:
                    HandleAttackCommand(command);
                    break;
                
                case CommandType.DEFEND:
                    HandleDefendCommand(command);
                    break;
                
                case CommandType.FORMATION_CHANGE:
                    HandleFormationCommand(command);
                    break;
            }
        }
        
        private void HandleMoveCommand(Command command)
        {
            if (command.TargetSquad == null) return;
        
            // FIX: Direct implementation if MovementSystem is not found
            var movementSystem = _worldManager.World.GetSystem<GridSquadMovementSystem>();
            if (movementSystem != null)
            {
                movementSystem.CommandMove(command.TargetSquad, command.TargetPosition);
            }
            else
            {
                // Alternative: Use SquadCommandSystem
                var commandSystem = _worldManager.World.GetSystem<SquadCommandSystem>();
                if (commandSystem != null)
                {
                    Vector3 worldPosition = _gridManager.GetCellCenter(command.TargetPosition);
                    commandSystem.CommandMove(command.TargetSquad, worldPosition);
                    Debug.Log($"Moving squad {command.TargetSquad.Id} to {worldPosition}");
                }
            }
        }
        public void VerifySystems()
        {
            if (_worldManager == null || _worldManager.World == null)
            {
                Debug.LogError("WorldManager or World is null!");
                return;
            }
        
            var world = _worldManager.World;
        
            // Check if essential systems are registered
            var commandSystem = world.GetSystem<SquadCommandSystem>();
            if (commandSystem == null)
            {
                Debug.LogWarning("SquadCommandSystem not found! Registering now...");
                world.RegisterSystem(new SquadCommandSystem());
            }
        
            var movementSystem = world.GetSystem<MovementSystem>();
            if (movementSystem == null)
            {
                Debug.LogWarning("MovementSystem not found! Registering now...");
                world.RegisterSystem(new MovementSystem());
            }
        
            Debug.Log($"Systems verified. Total systems: {world.GetRegisteredSystemCount()}");
        }
        
        private void HandleAttackCommand(Command command)
        {
            if (command.TargetSquad == null || command.TargetEntity == null) return;
            
            var squadComponent = command.TargetSquad.GetComponent<SquadComponent>();
            squadComponent.State = SquadState.ATTACKING;
            squadComponent.TargetSquadId = command.TargetEntity.Id;
            
            OnCombatStarted?.Invoke(command.TargetSquad, command.TargetEntity);
        }
        
        private void HandleDefendCommand(Command command)
        {
            if (command.TargetSquad == null) return;
            
            var squadComponent = command.TargetSquad.GetComponent<SquadComponent>();
            squadComponent.State = SquadState.IDLE;
            squadComponent.Formation = FormationType.TESTUDO; // Defensive formation
        }
        
        private void HandleFormationCommand(Command command)
        {
            if (command.TargetSquad == null) return;
            
            var squadComponent = command.TargetSquad.GetComponent<SquadComponent>();
            squadComponent.Formation = command.Formation;
            squadComponent.UpdateFormation();
        }
        #endregion

        #region Win/Lose Conditions
        private void CheckWinConditions()
        {
            // Check if player has won
            bool allEnemiesDefeated = true;
            
            foreach (var squadData in _squadData.Values)
            {
                if (squadData.Faction == Faction.ENEMY && squadData.Entity != null)
                {
                    allEnemiesDefeated = false;
                    break;
                }
            }
            
            if (allEnemiesDefeated && _gameConfig.VictoryCondition == VictoryCondition.DEFEAT_ALL_ENEMIES)
            {
                HandleVictory();
            }
        }
        
        private void CheckLoseConditions()
        {
            // Check if player has lost
            bool allPlayerSquadsDefeated = true;
            
            foreach (var squadData in _squadData.Values)
            {
                if (squadData.Faction == Faction.PLAYER && squadData.Entity != null)
                {
                    allPlayerSquadsDefeated = false;
                    break;
                }
            }
            
            if (allPlayerSquadsDefeated)
            {
                HandleDefeat();
            }
        }
        
        private void HandleVictory()
        {
            SetGameState(GameState.VICTORY);
            Debug.Log("Victory!");
            
            // Show victory UI
            if (_uiManager != null)
            {
                _uiManager.ShowVictoryScreen();
            }
        }
        
        private void HandleDefeat()
        {
            SetGameState(GameState.DEFEAT);
            Debug.Log("Defeat!");
            
            // Show defeat UI
            if (_uiManager != null)
            {
                _uiManager.ShowDefeatScreen();
            }
        }
        
        private void HandleGameOver()
        {
            // Game over logic
            Debug.Log("Game Over");
        }
        #endregion

        #region Debug
        private void HandleDebugInput()
        {
            // F1 - Toggle performance overlay
            if (Input.GetKeyDown(KeyCode.F1))
            {
                _showPerformanceOverlay = !_showPerformanceOverlay;
            }
            
            // F2 - Spawn test squad
            if (Input.GetKeyDown(KeyCode.F2))
            {
                SpawnTestSquad();
            }
            
            // F3 - Spawn enemy squad
            if (Input.GetKeyDown(KeyCode.F3))
            {
                SpawnEnemySquad();
            }
            
            // F4 - Toggle debug mode
            if (Input.GetKeyDown(KeyCode.F4))
            {
                _debugMode = !_debugMode;
                Debug.Log($"Debug mode: {_debugMode}");
            }
            
            // F5 - Quick restart
            if (Input.GetKeyDown(KeyCode.F5))
            {
                RestartGame();
            }
        }
        
        private void CreateTestSquads()
        {
            // Create player squad
            Vector3 playerPos = _gridManager.GetCellCenter(new Vector2Int(10, 10));
            CreateSquad(_defaultSquadConfig, playerPos, Faction.PLAYER);
            
            // Create enemy squad
            Vector3 enemyPos = _gridManager.GetCellCenter(new Vector2Int(15, 15));
            CreateSquad(_defaultSquadConfig, enemyPos, Faction.ENEMY);
        }
        
        private void SpawnTestSquad()
        {
            // Vector3 spawnPos = _gridManager.GetRandomEmptyCellPosition();
            // CreateSquad(_defaultSquadConfig, spawnPos, Faction.PLAYER);
        }
        
        private void SpawnEnemySquad()
        {
            // Vector3 spawnPos = _gridManager.GetRandomEmptyCellPosition();
            // CreateSquad(_defaultSquadConfig, spawnPos, Faction.ENEMY);
        }
        
        private void OnGUI()
        {
            if (!_showPerformanceOverlay) return;
            
            // Draw performance overlay
            GUI.Box(new Rect(10, 10, 200, 150), "Performance");
            GUI.Label(new Rect(20, 30, 180, 20), $"FPS: {1.0f / Time.deltaTime:F0}");
            GUI.Label(new Rect(20, 50, 180, 20), $"Entities: {_worldManager.World.GetEntityCount()}");
            GUI.Label(new Rect(20, 70, 180, 20), $"Active Squads: {_activeSquads.Count}");
            GUI.Label(new Rect(20, 90, 180, 20), $"Game Time: {_gameTime:F1}s");
            GUI.Label(new Rect(20, 110, 180, 20), $"State: {_currentState}");
            
            // Draw current state info
            GUI.Box(new Rect(10, 170, 200, 100), "Game State");
            GUI.Label(new Rect(20, 190, 180, 20), $"Player Squads: {GetSquadCountByFaction(Faction.PLAYER)}");
            GUI.Label(new Rect(20, 210, 180, 20), $"Enemy Squads: {GetSquadCountByFaction(Faction.ENEMY)}");
            GUI.Label(new Rect(20, 230, 180, 20), $"Neutral Squads: {GetSquadCountByFaction(Faction.NEUTRAL)}");
            
            // Performance report
            if (_worldManager != null && _worldManager.World != null)
            {
                var report = _worldManager.World.GetPerformanceReport();
                GUI.Box(new Rect(10, 280, 200, 200), "System Performance");
                
                int y = 300;
                foreach (var timing in report.AverageTimings.OrderByDescending(x => x.Value))
                {
                    GUI.Label(new Rect(20, y, 180, 20), $"{timing.Key}: {timing.Value:F2}ms");
                    y += 20;
                    if (y > 460) break;
                }
            }
        }
        #endregion

        #region Utility Methods
        public List<Entity> GetSquadsByFaction(Faction faction)
        {
            var result = new List<Entity>();
            
            foreach (var squadData in _squadData.Values)
            {
                if (squadData.Faction == faction && squadData.Entity != null)
                {
                    result.Add(squadData.Entity);
                }
            }
            
            return result;
        }
        
        public int GetSquadCountByFaction(Faction faction)
        {
            int count = 0;
            
            foreach (var squadData in _squadData.Values)
            {
                if (squadData.Faction == faction && squadData.Entity != null)
                {
                    count++;
                }
            }
            
            return count;
        }
        
        public SquadData GetSquadData(Entity squadEntity)
        {
            return _squadData.TryGetValue(squadEntity.Id, out var data) ? data : null;
        }
        #endregion
    }
}