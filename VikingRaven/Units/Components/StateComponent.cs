using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Core.StateMachine;
using VikingRaven.Units.StateMachine;
using System;
using System.Collections.Generic;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Simple StateComponent that works with existing NavigationComponent
    /// Only adds essential movement detection without breaking existing functionality
    /// </summary>
    public class StateComponent : BaseComponent
    {
        #region State Configuration
        
        [TitleGroup("State Configuration")]
        [InfoBox("Enhanced state management with combat-specific states and intelligent transitions.", InfoMessageType.Info)]
        
        [Tooltip("Enable intelligent state transitions based on combat conditions")]
        [SerializeField, ToggleLeft] private bool _enableIntelligentTransitions = true;
        
        [Tooltip("State transition delay for smoother behavior")]
        [SerializeField, Range(0.1f, 2f)] private float _stateTransitionDelay = 0.3f;
        
        [Tooltip("Enable state priority system")]
        [SerializeField, ToggleLeft] private bool _enableStatePriority = true;

        #endregion

        #region Component References
        
        [TitleGroup("Component References")]
        [InfoBox("Assign component references for better performance. NavigationComponent must have IsMoving and CurrentSpeed properties.", InfoMessageType.Info)]
        
        [Tooltip("Combat component for battle mechanics")]
        [SerializeField, Required] private CombatComponent _combatComponent;
        
        [Tooltip("Health component for health and stamina tracking")]
        [SerializeField, Required] private HealthComponent _healthComponent;
        
        [Tooltip("Weapon component for weapon state tracking")]
        [SerializeField, Required] private WeaponComponent _weaponComponent;
        
        [Tooltip("Aggro detection component for enemy detection")]
        [SerializeField, Required] private AggroDetectionComponent _aggroComponent;
        
        [Tooltip("Navigation component for movement tracking - Must have IsMoving and CurrentSpeed")]
        [SerializeField, Required] private NavigationComponent _navigationComponent;

        #endregion

        #region Movement Detection Fallback
        
        [TitleGroup("Movement Detection Fallback")]
        [InfoBox("Fallback movement detection if NavigationComponent doesn't have IsMoving/CurrentSpeed", InfoMessageType.Warning)]
        
        [Tooltip("Use fallback movement detection if NavigationComponent properties are missing")]
        [SerializeField, ToggleLeft] private bool _useFallbackMovementDetection = false;
        
        [ShowIf("_useFallbackMovementDetection")]
        [Tooltip("Minimum speed threshold to consider unit as moving")]
        [SerializeField, Range(0.1f, 2f)] private float _movementThreshold = 0.5f;
        
        [ShowIf("_useFallbackMovementDetection")]
        [Tooltip("Optional Rigidbody for movement detection")]
        [SerializeField] private Rigidbody _rigidbody;
        
        // Fallback movement tracking
        private Vector3 _lastPosition;
        private float _lastPositionTime;
        private bool _fallbackIsMoving = false;
        private float _fallbackCurrentSpeed = 0f;

        #endregion

        #region Combat State Tracking
        
        [TitleGroup("Combat State Tracking")]
        [InfoBox("Real-time tracking of combat states and conditions for intelligent behavior.", InfoMessageType.Warning)]
        
        [ShowInInspector, ReadOnly]
        [LabelText("Current Combat State"), LabelWidth(150)]
        private CombatStateType _currentCombatState = CombatStateType.Idle;
        
        [ShowInInspector, ReadOnly]
        [LabelText("Previous Combat State"), LabelWidth(150)]
        private CombatStateType _previousCombatState = CombatStateType.Idle;
        
        [ShowInInspector, ReadOnly]
        [LabelText("Time in Current State"), LabelWidth(150)]
        private float _timeInCurrentState = 0f;
        
        [ShowInInspector, ReadOnly]
        [LabelText("Last State Change Time"), LabelWidth(150)]
        private float _lastStateChangeTime = 0f;

        #endregion

        #region Combat Conditions
        
        [TitleGroup("Combat Conditions")]
        [InfoBox("Current combat conditions affecting state transitions", InfoMessageType.None)]
        
        [ShowInInspector, ReadOnly]
        [LabelText("Has Enemies in Range"), LabelWidth(150)]
        private bool _hasEnemiesInRange = false;
        
        [ShowInInspector, ReadOnly]
        [LabelText("Is Under Attack"), LabelWidth(150)]
        private bool _isUnderAttack = false;
        
        [ShowInInspector, ReadOnly]
        [LabelText("Is Low Health"), LabelWidth(150)]
        private bool _isLowHealth = false;
        
        [ShowInInspector, ReadOnly]
        [LabelText("Is Exhausted"), LabelWidth(150)]
        private bool _isExhausted = false;
        
        [ShowInInspector, ReadOnly]
        [LabelText("Weapon Broken"), LabelWidth(150)]
        private bool _weaponBroken = false;

        #endregion

        #region Movement Information
        
        [TitleGroup("Movement Information")]
        [InfoBox("Movement state from NavigationComponent or fallback detection", InfoMessageType.Info)]
        
        [ShowInInspector, ReadOnly]
        [LabelText("Is Moving"), LabelWidth(150)]
        public bool IsMoving => GetIsMoving();
        
        [ShowInInspector, ReadOnly]
        [LabelText("Current Speed"), LabelWidth(150)]
        public float CurrentSpeed => GetCurrentSpeed();
        
        [ShowInInspector, ReadOnly]
        [LabelText("Movement Source"), LabelWidth(150)]
        private string MovementSource => GetMovementSource();
        
        // Internal movement tracking
        private bool _wasMovingLastFrame = false;

        #endregion

        #region State Priority System
        
        [TitleGroup("State Priority System")]
        [InfoBox("Priority-based state system ensures critical states take precedence.", InfoMessageType.Info)]
        
        [Tooltip("State priorities for intelligent transitions")]
        [SerializeField, DictionaryDrawerSettings(KeyLabel = "State Type", ValueLabel = "Priority")]
        private Dictionary<CombatStateType, int> _statePriorities = new Dictionary<CombatStateType, int>();
        
        [ShowInInspector, ReadOnly] 
        private Queue<StateTransition> _pendingTransitions = new Queue<StateTransition>();
        
        [Tooltip("Override current state only if new state has higher priority")]
        [SerializeField, ToggleLeft] private bool _respectStatePriority = true;

        #endregion

        #region Enhanced State Conditions
        
        [TitleGroup("Enhanced State Conditions")]
        [InfoBox("Configurable conditions for state transitions based on combat mechanics.", InfoMessageType.None)]
        
        [Tooltip("Health percentage threshold for retreat behavior")]
        [SerializeField, Range(10f, 50f)] private float _retreatHealthThreshold = 25f;
        
        [Tooltip("Stamina percentage threshold for exhaustion state")]
        [SerializeField, Range(10f, 30f)] private float _exhaustionStaminaThreshold = 20f;
        
        [Tooltip("Enemy detection range for combat state activation")]
        [SerializeField, Range(5f, 15f)] private float _combatDetectionRange = 10f;
        
        [Tooltip("Time without combat before returning to idle")]
        [SerializeField, Range(3f, 10f)] private float _combatCooldownTime = 5f;

        #endregion

        #region State Machine (Non-Serialized)
        
        // Pure C# state machine - cannot be serialized in Unity
        private StateMachineInGame stateMachineInGame;
        
        [ShowInInspector, ReadOnly]
        [TitleGroup("State Machine Info")]
        [LabelText("State Machine Status"), LabelWidth(150)]
        public string StateMachineStatus => stateMachineInGame?.GetDebugInfo() ?? "Not Initialized";
        
        [ShowInInspector, ReadOnly]
        [TitleGroup("State Machine Info")]
        [LabelText("Current State Name"), LabelWidth(150)]
        public string CurrentStateName => stateMachineInGame?.CurrentState?.GetType().Name ?? "None";

        #endregion

        #region Public Properties

        public IStateMachine StateMachineInGame => stateMachineInGame;
        public IState CurrentState => stateMachineInGame?.CurrentState;
        public CombatStateType CurrentCombatState => _currentCombatState;
        public CombatStateType PreviousCombatState => _previousCombatState;
        public float TimeInCurrentState => _timeInCurrentState;
        public bool IsInCombat => _currentCombatState != CombatStateType.Idle && _currentCombatState != CombatStateType.Patrolling;
        public bool CanTransitionToState(CombatStateType newState) => CheckStateTransitionValidity(newState);

        #endregion

        #region Events

        public event Action<CombatStateType, CombatStateType> OnCombatStateChanged;
        public event Action<CombatStateType> OnCombatStateEntered;
        public event Action<CombatStateType> OnCombatStateExited;
        public event Action<StateTransition> OnStateTransitionBlocked;
        public event Action OnCombatEngaged;
        public event Action OnCombatDisengaged;

        #endregion

        #region Unity Lifecycle

        public override void Initialize()
        {
            base.Initialize();
            
            ValidateComponentReferences();
            InitializeFallbackMovementDetection();
            InitializeStateMachine();
            InitializeStatePriorities();
            SubscribeToComponentEvents();
            
            Debug.Log($"StateComponent: Enhanced state system initialized for {Entity.Id}");
        }

        private void Update()
        {
            if (!IsActive || stateMachineInGame == null) return;
            
            UpdateFallbackMovementDetection();
            UpdateMovementTracking();
            UpdateStateTracking();
            UpdateCombatConditions();
            ProcessIntelligentTransitions();
            ProcessPendingTransitions();
            
            // Update the pure C# state machine
            stateMachineInGame.Update();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Validate component references are assigned
        /// </summary>
        private void ValidateComponentReferences()
        {
            bool hasErrors = false;
            
            // Validate required components
            if (_combatComponent == null)
            {
                Debug.LogError($"StateComponent: CombatComponent is REQUIRED for entity {Entity.Id}");
                hasErrors = true;
            }
            
            if (_healthComponent == null)
            {
                Debug.LogError($"StateComponent: HealthComponent is REQUIRED for entity {Entity.Id}");
                hasErrors = true;
            }
            
            if (_weaponComponent == null)
            {
                Debug.LogError($"StateComponent: WeaponComponent is REQUIRED for entity {Entity.Id}");
                hasErrors = true;
            }
            
            if (_aggroComponent == null)
            {
                Debug.LogError($"StateComponent: AggroDetectionComponent is REQUIRED for entity {Entity.Id}");
                hasErrors = true;
            }
            
            if (_navigationComponent == null)
            {
                Debug.LogError($"StateComponent: NavigationComponent is REQUIRED for entity {Entity.Id}");
                hasErrors = true;
            }
            else
            {
                // Check if NavigationComponent has required properties
                CheckNavigationComponentProperties();
            }
            
            if (!hasErrors)
            {
                Debug.Log($"StateComponent: All required components validated for entity {Entity.Id}");
            }
        }

        /// <summary>
        /// Check if NavigationComponent has IsMoving and CurrentSpeed properties
        /// </summary>
        private void CheckNavigationComponentProperties()
        {
            var navType = _navigationComponent.GetType();
            
            var isMovingProperty = navType.GetProperty("IsMoving");
            var currentSpeedProperty = navType.GetProperty("CurrentSpeed");
            
            if (isMovingProperty == null || currentSpeedProperty == null)
            {
                Debug.LogWarning($"StateComponent: NavigationComponent missing IsMoving or CurrentSpeed properties for entity {Entity.Id}. " +
                               "Enable fallback movement detection to continue.");
                _useFallbackMovementDetection = true;
            }
            else
            {
                Debug.Log($"StateComponent: NavigationComponent has required movement properties for entity {Entity.Id}");
            }
        }

        /// <summary>
        /// Initialize fallback movement detection
        /// </summary>
        private void InitializeFallbackMovementDetection()
        {
            _lastPosition = transform.position;
            _lastPositionTime = Time.time;
            _fallbackIsMoving = false;
            _fallbackCurrentSpeed = 0f;
            
            // Auto-assign Rigidbody if not set and fallback is enabled
            if (_useFallbackMovementDetection && _rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
                if (_rigidbody != null)
                {
                    Debug.LogWarning($"StateComponent: Auto-assigned Rigidbody for fallback movement detection on entity {Entity.Id}");
                }
            }
        }

        /// <summary>
        /// Initialize state machine as pure C# object
        /// </summary>
        private void InitializeStateMachine()
        {
            if (stateMachineInGame == null)
            {
                // Create pure C# state machine instance
                stateMachineInGame = new StateMachineInGame(Entity.Id);
                
                // Subscribe to state machine events
                stateMachineInGame.OnStateChanged += OnStateMachineStateChanged;
                stateMachineInGame.OnStateEntered += OnStateMachineStateEntered;
                stateMachineInGame.OnStateExited += OnStateMachineStateExited;
                
                RegisterEnhancedStates();
                
                Debug.Log($"StateComponent: Pure C# state machine created for entity {Entity.Id}");
            }
        }

        /// <summary>
        /// Register enhanced combat states
        /// </summary>
        private void RegisterEnhancedStates()
        {
            // Core states
            var idleState = new EnhancedIdleState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<EnhancedIdleState>(idleState);
            
            var aggroState = new EnhancedAggroState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<EnhancedAggroState>(aggroState);
            
            // Enhanced combat states
            var combatState = new CombatEngagedState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<CombatEngagedState>(combatState);
            
            var retreatState = new RetreatState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<RetreatState>(retreatState);
            
            var exhaustedState = new ExhaustedState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<ExhaustedState>(exhaustedState);
            
            var weaponBrokenState = new WeaponBrokenState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<WeaponBrokenState>(weaponBrokenState);
            
            var guardingState = new GuardingState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<GuardingState>(guardingState);
            
            var patrollingState = new PatrollingState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<PatrollingState>(patrollingState);
            
            // Status effect states
            var knockbackState = new KnockbackState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<KnockbackState>(knockbackState);
            
            var stunState = new StunState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<StunState>(stunState);
            
            // Set initial state
            stateMachineInGame.ChangeState(idleState);
            _currentCombatState = CombatStateType.Idle;
            
            Debug.Log($"StateComponent: Registered {stateMachineInGame.RegisteredStatesCount} states for entity {Entity.Id}");
        }

        /// <summary>
        /// Initialize state priorities for intelligent transitions
        /// </summary>
        private void InitializeStatePriorities()
        {
            _statePriorities[CombatStateType.Stunned] = 100;        // Highest priority
            _statePriorities[CombatStateType.Knockback] = 95;
            _statePriorities[CombatStateType.WeaponBroken] = 90;
            _statePriorities[CombatStateType.Retreat] = 85;
            _statePriorities[CombatStateType.Exhausted] = 80;
            _statePriorities[CombatStateType.CombatEngaged] = 70;
            _statePriorities[CombatStateType.Aggro] = 60;
            _statePriorities[CombatStateType.Guarding] = 50;
            _statePriorities[CombatStateType.Patrolling] = 30;
            _statePriorities[CombatStateType.Idle] = 10;           // Lowest priority
        }

        /// <summary>
        /// Subscribe to component events for reactive state changes
        /// </summary>
        private void SubscribeToComponentEvents()
        {
            // Health component events
            if (_healthComponent != null)
            {
                _healthComponent.OnDamageTaken += OnDamageTaken;
                _healthComponent.OnDeath += OnDeath;
                _healthComponent.OnStaminaChanged += OnStaminaChanged;
            }
            
            // Combat component events
            if (_combatComponent != null)
            {
                _combatComponent.OnCombatStateChanged += OnCombatStateChangedFunc;
                _combatComponent.OnDamageReceived += OnDamageReceived;
            }
            
            // Weapon component events
            if (_weaponComponent != null)
            {
                _weaponComponent.OnWeaponBroken += OnWeaponBroken;
                _weaponComponent.OnWeaponRepaired += OnWeaponRepaired;
            }
        }

        #endregion

        #region Movement Detection - Smart Approach

        /// <summary>
        /// Get IsMoving from NavigationComponent or fallback detection
        /// </summary>
        private bool GetIsMoving()
        {
            if (!_useFallbackMovementDetection && _navigationComponent != null)
            {
                // Try to get IsMoving from NavigationComponent using reflection
                var navType = _navigationComponent.GetType();
                var isMovingProperty = navType.GetProperty("IsMoving");
                
                if (isMovingProperty != null)
                {
                    return (bool)isMovingProperty.GetValue(_navigationComponent);
                }
            }
            
            // Use fallback detection
            return _fallbackIsMoving;
        }

        /// <summary>
        /// Get CurrentSpeed from NavigationComponent or fallback detection
        /// </summary>
        private float GetCurrentSpeed()
        {
            if (!_useFallbackMovementDetection && _navigationComponent != null)
            {
                // Try to get CurrentSpeed from NavigationComponent using reflection
                var navType = _navigationComponent.GetType();
                var currentSpeedProperty = navType.GetProperty("CurrentSpeed");
                
                if (currentSpeedProperty != null)
                {
                    return (float)currentSpeedProperty.GetValue(_navigationComponent);
                }
            }
            
            // Use fallback detection
            return _fallbackCurrentSpeed;
        }

        /// <summary>
        /// Get movement source for debugging
        /// </summary>
        private string GetMovementSource()
        {
            if (!_useFallbackMovementDetection && _navigationComponent != null)
            {
                var navType = _navigationComponent.GetType();
                var isMovingProperty = navType.GetProperty("IsMoving");
                var currentSpeedProperty = navType.GetProperty("CurrentSpeed");
                
                if (isMovingProperty != null && currentSpeedProperty != null)
                {
                    return "NavigationComponent";
                }
            }
            
            return "Fallback Detection";
        }

        /// <summary>
        /// Update fallback movement detection
        /// </summary>
        private void UpdateFallbackMovementDetection()
        {
            if (!_useFallbackMovementDetection) return;
            
            // Update every 0.1 seconds for better performance
            if (Time.time - _lastPositionTime > 0.1f)
            {
                var currentPosition = transform.position;
                var deltaTime = Time.time - _lastPositionTime;
                var distance = Vector3.Distance(currentPosition, _lastPosition);
                
                _fallbackCurrentSpeed = distance / deltaTime;
                _fallbackIsMoving = _fallbackCurrentSpeed > _movementThreshold;
                
                // Additional check with Rigidbody if available
                if (_rigidbody != null)
                {
                    var rigidBodySpeed = _rigidbody.linearVelocity.magnitude;
                    if (rigidBodySpeed > _movementThreshold)
                    {
                        _fallbackIsMoving = true;
                        _fallbackCurrentSpeed = Mathf.Max(_fallbackCurrentSpeed, rigidBodySpeed);
                    }
                }
                
                _lastPosition = currentPosition;
                _lastPositionTime = Time.time;
            }
        }

        #endregion

        #region Movement Tracking

        /// <summary>
        /// Update movement tracking and handle state transitions
        /// </summary>
        private void UpdateMovementTracking()
        {
            bool isCurrentlyMoving = IsMoving;
            bool wasMoving = _wasMovingLastFrame;
            
            // Movement state changed
            if (wasMoving != isCurrentlyMoving)
            {
                Debug.Log($"StateComponent: Entity {Entity.Id} movement changed - IsMoving: {isCurrentlyMoving} " +
                         $"(Speed: {CurrentSpeed:F2}, Source: {MovementSource})");
                
                HandleMovementStateChange(wasMoving, isCurrentlyMoving);
            }
            
            _wasMovingLastFrame = isCurrentlyMoving;
        }

        /// <summary>
        /// Handle movement state changes with intelligent state transitions
        /// </summary>
        private void HandleMovementStateChange(bool wasMoving, bool isMoving)
        {
            if (!wasMoving && isMoving)
            {
                // Started moving
                OnMovementStarted();
            }
            else if (wasMoving && !isMoving)
            {
                // Stopped moving
                OnMovementStopped();
            }
        }

        /// <summary>
        /// Handle movement started event
        /// </summary>
        private void OnMovementStarted()
        {
            Debug.Log($"StateComponent: Entity {Entity.Id} started moving");
            
            // Transition to patrolling if conditions are right
            if (!_hasEnemiesInRange && !IsInCombat && _currentCombatState == CombatStateType.Idle)
            {
                RequestStateTransition(CombatStateType.Patrolling, StateTransitionReason.MovementStarted);
            }
        }

        /// <summary>
        /// Handle movement stopped event
        /// </summary>
        private void OnMovementStopped()
        {
            Debug.Log($"StateComponent: Entity {Entity.Id} stopped moving");
            
            // Transition to idle if conditions are right
            if (!_hasEnemiesInRange && !IsInCombat && _currentCombatState == CombatStateType.Patrolling)
            {
                RequestStateTransition(CombatStateType.Idle, StateTransitionReason.MovementStopped);
            }
        }

        #endregion

        #region State Machine Event Handlers

        /// <summary>
        /// Handle state machine state changes
        /// </summary>
        private void OnStateMachineStateChanged(IState previousState, IState newState)
        {
            Debug.Log($"StateComponent: State machine changed from {previousState?.GetType().Name ?? "null"} to {newState?.GetType().Name ?? "null"} for entity {Entity.Id}");
        }

        /// <summary>
        /// Handle state entered events
        /// </summary>
        private void OnStateMachineStateEntered(IState state)
        {
            Debug.Log($"StateComponent: Entered state {state?.GetType().Name} for entity {Entity.Id}");
        }

        /// <summary>
        /// Handle state exited events
        /// </summary>
        private void OnStateMachineStateExited(IState state)
        {
            Debug.Log($"StateComponent: Exited state {state?.GetType().Name} for entity {Entity.Id}");
        }

        #endregion

        #region State Tracking and Updates

        /// <summary>
        /// Update state tracking information
        /// </summary>
        private void UpdateStateTracking()
        {
            _timeInCurrentState += Time.deltaTime;
            
            // Update current combat state based on active state machine state
            var newCombatState = DetermineCombatStateFromStateMachine();
            if (newCombatState != _currentCombatState)
            {
                ChangeCombatState(newCombatState);
            }
        }

        /// <summary>
        /// Update combat conditions for intelligent state transitions
        /// </summary>
        private void UpdateCombatConditions()
        {
            // Check enemy presence
            _hasEnemiesInRange = _aggroComponent != null && _aggroComponent.HasEnemyInRange();
            
            // Check if under attack (recently took damage)
            _isUnderAttack = _healthComponent != null && _healthComponent.TimeSinceLastDamage < 2f;
            
            // Check health status
            _isLowHealth = _healthComponent != null && _healthComponent.HealthPercentage < _retreatHealthThreshold;
            
            // Check stamina/exhaustion
            _isExhausted = _healthComponent != null && _healthComponent.StaminaPercentage < _exhaustionStaminaThreshold;
            
            // Check weapon status
            _weaponBroken = _weaponComponent != null && _weaponComponent.IsPrimaryWeaponBroken;
        }

        /// <summary>
        /// Process intelligent state transitions based on conditions
        /// </summary>
        private void ProcessIntelligentTransitions()
        {
            if (!_enableIntelligentTransitions) return;
            
            // Skip if recently changed state
            if (Time.time - _lastStateChangeTime < _stateTransitionDelay) return;
            
            CombatStateType recommendedState = DetermineRecommendedState();
            
            if (recommendedState != _currentCombatState && CanTransitionToState(recommendedState))
            {
                RequestStateTransition(recommendedState, StateTransitionReason.Intelligent);
            }
        }

        /// <summary>
        /// Determine recommended state based on current conditions including movement
        /// </summary>
        private CombatStateType DetermineRecommendedState()
        {
            // Priority-based state determination with movement consideration
            
            // Critical states (highest priority)
            if (_weaponBroken && _hasEnemiesInRange)
                return CombatStateType.WeaponBroken;
            
            if (_isLowHealth && _hasEnemiesInRange)
                return CombatStateType.Retreat;
            
            if (_isExhausted && _hasEnemiesInRange)
                return CombatStateType.Exhausted;
            
            // Combat states
            if (_isUnderAttack || (_hasEnemiesInRange && _combatComponent != null && _combatComponent.IsInCombat))
                return CombatStateType.CombatEngaged;
            
            if (_hasEnemiesInRange)
                return CombatStateType.Aggro;
            
            // Movement-based states
            if (IsMoving && !IsInCombat)
                return CombatStateType.Patrolling;
            
            // Passive states
            if (_healthComponent != null && _healthComponent.CurrentInjuryState != InjuryState.Healthy)
                return CombatStateType.Guarding;
            
            // Default states
            return CombatStateType.Idle;
        }

        #endregion

        #region State Transition Management

        /// <summary>
        /// Request a state transition with reason
        /// </summary>
        public bool RequestStateTransition(CombatStateType newState, StateTransitionReason reason)
        {
            var transition = new StateTransition(newState, reason, Time.time);
            
            if (!CheckStateTransitionValidity(newState))
            {
                OnStateTransitionBlocked?.Invoke(transition);
                return false;
            }
            
            if (_enableStatePriority && _respectStatePriority)
            {
                // Check priority
                int currentPriority = GetStatePriority(_currentCombatState);
                int newPriority = GetStatePriority(newState);
                
                if (newPriority <= currentPriority && reason != StateTransitionReason.Forced)
                {
                    // Queue for later if priority is lower
                    _pendingTransitions.Enqueue(transition);
                    return false;
                }
            }
            
            ExecuteStateTransition(newState, reason);
            return true;
        }

        /// <summary>
        /// Force a state transition regardless of conditions
        /// </summary>
        public void ForceStateTransition(CombatStateType newState)
        {
            ExecuteStateTransition(newState, StateTransitionReason.Forced);
        }

        /// <summary>
        /// Execute state transition
        /// </summary>
        private void ExecuteStateTransition(CombatStateType newState, StateTransitionReason reason)
        {
            var targetState = GetStateFromCombatStateType(newState);
            if (targetState != null)
            {
                stateMachineInGame.ChangeState(targetState);
                Debug.Log($"StateComponent: Transitioned to {newState} (Reason: {reason})");
            }
        }

        /// <summary>
        /// Process pending state transitions
        /// </summary>
        private void ProcessPendingTransitions()
        {
            if (_pendingTransitions.Count == 0) return;
            
            // Check if we can process pending transitions
            if (Time.time - _lastStateChangeTime < _stateTransitionDelay) return;
            
            while (_pendingTransitions.Count > 0)
            {
                var transition = _pendingTransitions.Dequeue();
                
                // Check if transition is still valid
                if (CheckStateTransitionValidity(transition.TargetState))
                {
                    ExecuteStateTransition(transition.TargetState, transition.Reason);
                    break; // Only process one transition per frame
                }
            }
        }

        /// <summary>
        /// Check if state transition is valid
        /// </summary>
        private bool CheckStateTransitionValidity(CombatStateType targetState)
        {
            // Current state restrictions
            switch (_currentCombatState)
            {
                case CombatStateType.Stunned:
                case CombatStateType.Knockback:
                    // Can only transition out of these states when they naturally end
                    return false;
                    
                case CombatStateType.WeaponBroken:
                    // Can only leave if weapon is repaired or forced
                    return !_weaponBroken || targetState == CombatStateType.Retreat;
            }
            
            // Target state requirements
            switch (targetState)
            {
                case CombatStateType.CombatEngaged:
                    return _hasEnemiesInRange && !_isExhausted && !_weaponBroken;
                    
                case CombatStateType.Retreat:
                    return _isLowHealth || _isExhausted || _weaponBroken;
                    
                case CombatStateType.Exhausted:
                    return _isExhausted;
                    
                case CombatStateType.WeaponBroken:
                    return _weaponBroken;
                    
                case CombatStateType.Patrolling:
                    return IsMoving && !IsInCombat;
            }
            
            return true; // Default to allowing transition
        }

        #endregion

        #region State Management Helpers

        /// <summary>
        /// Change combat state and trigger events
        /// </summary>
        private void ChangeCombatState(CombatStateType newState)
        {
            var oldState = _currentCombatState;
            _previousCombatState = oldState;
            _currentCombatState = newState;
            _timeInCurrentState = 0f;
            _lastStateChangeTime = Time.time;
            
            // Trigger events
            OnCombatStateExited?.Invoke(oldState);
            OnCombatStateChanged?.Invoke(oldState, newState);
            OnCombatStateEntered?.Invoke(newState);
            
            // Combat engagement tracking
            bool wasInCombat = IsCombatState(oldState);
            bool isInCombat = IsCombatState(newState);
            
            if (!wasInCombat && isInCombat)
            {
                OnCombatEngaged?.Invoke();
            }
            else if (wasInCombat && !isInCombat)
            {
                OnCombatDisengaged?.Invoke();
            }
            
            Debug.Log($"StateComponent: Combat state changed from {oldState} to {newState} for entity {Entity.Id}");
        }

        /// <summary>
        /// Determine combat state from current state machine state
        /// </summary>
        private CombatStateType DetermineCombatStateFromStateMachine()
        {
            if (stateMachineInGame?.CurrentState == null) return CombatStateType.Idle;
            
            return stateMachineInGame.CurrentState.GetType().Name switch
            {
                "EnhancedIdleState" => CombatStateType.Idle,
                "EnhancedAggroState" => CombatStateType.Aggro,
                "CombatEngagedState" => CombatStateType.CombatEngaged,
                "RetreatState" => CombatStateType.Retreat,
                "ExhaustedState" => CombatStateType.Exhausted,
                "WeaponBrokenState" => CombatStateType.WeaponBroken,
                "GuardingState" => CombatStateType.Guarding,
                "PatrollingState" => CombatStateType.Patrolling,
                "KnockbackState" => CombatStateType.Knockback,
                "StunState" => CombatStateType.Stunned,
                _ => _currentCombatState
            };
        }

        /// <summary>
        /// Get state instance from combat state type
        /// </summary>
        private IState GetStateFromCombatStateType(CombatStateType stateType)
        {
            return stateType switch
            {
                CombatStateType.Idle => stateMachineInGame.GetState<EnhancedIdleState>(),
                CombatStateType.Aggro => stateMachineInGame.GetState<EnhancedAggroState>(),
                CombatStateType.CombatEngaged => stateMachineInGame.GetState<CombatEngagedState>(),
                CombatStateType.Retreat => stateMachineInGame.GetState<RetreatState>(),
                CombatStateType.Exhausted => stateMachineInGame.GetState<ExhaustedState>(),
                CombatStateType.WeaponBroken => stateMachineInGame.GetState<WeaponBrokenState>(),
                CombatStateType.Guarding => stateMachineInGame.GetState<GuardingState>(),
                CombatStateType.Patrolling => stateMachineInGame.GetState<PatrollingState>(),
                CombatStateType.Knockback => stateMachineInGame.GetState<KnockbackState>(),
                CombatStateType.Stunned => stateMachineInGame.GetState<StunState>(),
                _ => null
            };
        }

        /// <summary>
        /// Get state priority
        /// </summary>
        private int GetStatePriority(CombatStateType stateType)
        {
            return _statePriorities.TryGetValue(stateType, out int priority) ? priority : 0;
        }

        /// <summary>
        /// Check if state type is a combat state
        /// </summary>
        private bool IsCombatState(CombatStateType stateType)
        {
            return stateType switch
            {
                CombatStateType.Aggro or 
                CombatStateType.CombatEngaged or 
                CombatStateType.Retreat => true,
                _ => false
            };
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle damage taken event
        /// </summary>
        private void OnDamageTaken(float amount, IEntity source)
        {
            // Trigger combat state if not already in combat
            if (!IsInCombat && _hasEnemiesInRange)
            {
                RequestStateTransition(CombatStateType.CombatEngaged, StateTransitionReason.DamageTaken);
            }
            
            // Check for retreat condition
            if (_isLowHealth)
            {
                RequestStateTransition(CombatStateType.Retreat, StateTransitionReason.LowHealth);
            }
        }

        /// <summary>
        /// Handle death event
        /// </summary>
        private void OnDeath()
        {
            Debug.Log($"StateComponent: Entity {Entity.Id} died in state {_currentCombatState}");
        }

        /// <summary>
        /// Handle stamina change event
        /// </summary>
        private void OnStaminaChanged(float newStamina)
        {
            if (_isExhausted && IsInCombat)
            {
                RequestStateTransition(CombatStateType.Exhausted, StateTransitionReason.Exhausted);
            }
        }

        /// <summary>
        /// Handle combat state change from combat component
        /// </summary>
        private void OnCombatStateChangedFunc(bool inCombat)
        {
            if (!inCombat && IsInCombat)
            {
                // Combat ended, return to appropriate state
                if (IsMoving)
                {
                    RequestStateTransition(CombatStateType.Patrolling, StateTransitionReason.CombatEnded);
                }
                else
                {
                    RequestStateTransition(CombatStateType.Idle, StateTransitionReason.CombatEnded);
                }
            }
        }

        /// <summary>
        /// Handle damage received event
        /// </summary>
        private void OnDamageReceived(DamageInfo damageInfo)
        {
            // React to specific damage types or amounts
            if (damageInfo.FinalDamage > _healthComponent.MaxHealth * 0.3f) // Heavy damage
            {
                // Potential stagger or knockback
                if (UnityEngine.Random.value < 0.3f) // 30% chance
                {
                    RequestStateTransition(CombatStateType.Knockback, StateTransitionReason.HeavyDamage);
                }
            }
        }

        /// <summary>
        /// Handle weapon broken event
        /// </summary>
        private void OnWeaponBroken(WeaponType weaponType)
        {
            RequestStateTransition(CombatStateType.WeaponBroken, StateTransitionReason.WeaponBroken);
        }

        /// <summary>
        /// Handle weapon repaired event
        /// </summary>
        private void OnWeaponRepaired(WeaponType weaponType)
        {
            if (_currentCombatState == CombatStateType.WeaponBroken)
            {
                if (IsMoving)
                {
                    RequestStateTransition(CombatStateType.Patrolling, StateTransitionReason.WeaponRepaired);
                }
                else
                {
                    RequestStateTransition(CombatStateType.Idle, StateTransitionReason.WeaponRepaired);
                }
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Get current state information
        /// </summary>
        public StateInfo GetCurrentStateInfo()
        {
            return new StateInfo
            {
                CombatState = _currentCombatState,
                TimeInState = _timeInCurrentState,
                IsInCombat = IsInCombat,
                CanTransition = Time.time - _lastStateChangeTime >= _stateTransitionDelay,
                HasEnemiesInRange = _hasEnemiesInRange,
                IsUnderAttack = _isUnderAttack,
                IsLowHealth = _isLowHealth,
                IsExhausted = _isExhausted,
                WeaponBroken = _weaponBroken,
                IsMoving = IsMoving
            };
        }

        /// <summary>
        /// Manually trigger state transition
        /// </summary>
        public bool TriggerStateTransition(CombatStateType targetState)
        {
            return RequestStateTransition(targetState, StateTransitionReason.Manual);
        }

        /// <summary>
        /// Get state machine debug information
        /// </summary>
        public string GetStateMachineDebugInfo()
        {
            return stateMachineInGame?.GetDebugInfo() ?? "StateMachine not initialized";
        }

        /// <summary>
        /// Pause the state machine
        /// </summary>
        public void PauseStateMachine()
        {
            stateMachineInGame?.Pause();
        }

        /// <summary>
        /// Resume the state machine
        /// </summary>
        public void ResumeStateMachine()
        {
            stateMachineInGame?.Resume();
        }

        #endregion

        #region Cleanup

        public override void Cleanup()
        {
            // Unsubscribe from component events
            if (_healthComponent != null)
            {
                _healthComponent.OnDamageTaken -= OnDamageTaken;
                _healthComponent.OnDeath -= OnDeath;
                _healthComponent.OnStaminaChanged -= OnStaminaChanged;
            }
            
            if (_combatComponent != null)
            {
                _combatComponent.OnCombatStateChanged -= OnCombatStateChangedFunc;
                _combatComponent.OnDamageReceived -= OnDamageReceived;
            }
            
            if (_weaponComponent != null)
            {
                _weaponComponent.OnWeaponBroken -= OnWeaponBroken;
                _weaponComponent.OnWeaponRepaired -= OnWeaponRepaired;
            }
            
            // Unsubscribe from state machine events
            if (stateMachineInGame != null)
            {
                stateMachineInGame.OnStateChanged -= OnStateMachineStateChanged;
                stateMachineInGame.OnStateEntered -= OnStateMachineStateEntered;
                stateMachineInGame.OnStateExited -= OnStateMachineStateExited;
                
                // Clean up the state machine
                stateMachineInGame.Cleanup();
                stateMachineInGame = null;
            }
            
            _pendingTransitions.Clear();
            
            base.Cleanup();
        }

        #endregion

        #region Debug Methods

        [Button("Show Complete State Info"), FoldoutGroup("Debug Tools")]
        [PropertySpace(SpaceBefore = 10)]
        private void ShowStateInfo()
        {
            var stateInfo = GetCurrentStateInfo();
            Debug.Log($"=== COMPLETE STATE INFORMATION ===");
            Debug.Log($"Current Combat State: {stateInfo.CombatState}");
            Debug.Log($"Current State Machine State: {CurrentStateName}");
            Debug.Log($"Time in State: {stateInfo.TimeInState:F1}s");
            Debug.Log($"Is in Combat: {stateInfo.IsInCombat}");
            Debug.Log($"Can Transition: {stateInfo.CanTransition}");
            Debug.Log($"=== MOVEMENT INFO ===");
            Debug.Log($"Is Moving: {IsMoving}");
            Debug.Log($"Current Speed: {CurrentSpeed:F2}");
            Debug.Log($"Movement Source: {MovementSource}");
            Debug.Log($"Use Fallback Detection: {_useFallbackMovementDetection}");
            Debug.Log($"=== COMBAT CONDITIONS ===");
            Debug.Log($"Enemies in Range: {stateInfo.HasEnemiesInRange}");
            Debug.Log($"Under Attack: {stateInfo.IsUnderAttack}");
            Debug.Log($"Low Health: {stateInfo.IsLowHealth}");
            Debug.Log($"Exhausted: {stateInfo.IsExhausted}");
            Debug.Log($"Weapon Broken: {stateInfo.WeaponBroken}");
            Debug.Log($"=== SYSTEM INFO ===");
            Debug.Log($"Pending Transitions: {_pendingTransitions.Count}");
            Debug.Log($"StateMachine Info: {GetStateMachineDebugInfo()}");
        }

        [Button("Test Movement Detection"), FoldoutGroup("Debug Tools")]
        private void TestMovementDetection()
        {
            Debug.Log($"=== MOVEMENT DETECTION TEST ===");
            Debug.Log($"StateComponent.IsMoving: {IsMoving}");
            Debug.Log($"StateComponent.CurrentSpeed: {CurrentSpeed:F2}");
            Debug.Log($"Movement Source: {MovementSource}");
            Debug.Log($"Use Fallback Detection: {_useFallbackMovementDetection}");
            
            if (_useFallbackMovementDetection)
            {
                Debug.Log($"Fallback IsMoving: {_fallbackIsMoving}");
                Debug.Log($"Fallback CurrentSpeed: {_fallbackCurrentSpeed:F2}");
            }
        }

        [Button("Force Combat State"), FoldoutGroup("Debug Tools")]
        private void ForceCombatState()
        {
            ForceStateTransition(CombatStateType.CombatEngaged);
        }

        [Button("Force Patrolling State"), FoldoutGroup("Debug Tools")]
        private void ForcePatrollingState()
        {
            ForceStateTransition(CombatStateType.Patrolling);
        }

        [Button("Toggle Fallback Detection"), FoldoutGroup("Debug Tools")]
        private void ToggleFallbackDetection()
        {
            _useFallbackMovementDetection = !_useFallbackMovementDetection;
            Debug.Log($"StateComponent: Fallback movement detection {(_useFallbackMovementDetection ? "enabled" : "disabled")}");
        }

        [Button("Show StateMachine Status"), FoldoutGroup("Debug Tools")]
        private void ShowStateMachineStatus()
        {
            stateMachineInGame?.LogStatus();
        }

        #endregion
    }
    
    // Supporting enums and structs
    public enum StateTransitionReason
    {
        Manual,
        Intelligent,
        Forced,
        DamageTaken,
        LowHealth,
        Exhausted,
        WeaponBroken,
        WeaponRepaired,
        CombatEnded,
        HeavyDamage,
        MovementStarted,
        MovementStopped
    }
    
    public struct StateTransition
    {
        public CombatStateType TargetState;
        public StateTransitionReason Reason;
        public float RequestTime;
        
        public StateTransition(CombatStateType targetState, StateTransitionReason reason, float requestTime)
        {
            TargetState = targetState;
            Reason = reason;
            RequestTime = requestTime;
        }
    }
    
    public struct StateInfo
    {
        public CombatStateType CombatState;
        public float TimeInState;
        public bool IsInCombat;
        public bool CanTransition;
        public bool HasEnemiesInRange;
        public bool IsUnderAttack;
        public bool IsLowHealth;
        public bool IsExhausted;
        public bool WeaponBroken;
        public bool IsMoving;
    }
}