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
    /// Enhanced State Component with combat-specific states and intelligent state transitions
    /// Phase 1 Enhancement: Comprehensive state management for realistic combat behavior
    /// </summary>
    public class StateComponent : BaseComponent
    {
        #region State Configuration
        
        [TitleGroup("State Configuration")]
        [InfoBox("Enhanced state management with combat-specific states and intelligent transitions.", InfoMessageType.Info)]
        
        [SerializeField] private StateMachineInGame stateMachineInGame;
        
        [Tooltip("Enable intelligent state transitions based on combat conditions")]
        [SerializeField, ToggleLeft] private bool _enableIntelligentTransitions = true;
        
        [Tooltip("State transition delay for smoother behavior")]
        [SerializeField, Range(0.1f, 2f)] private float _stateTransitionDelay = 0.3f;
        
        [Tooltip("Enable state priority system")]
        [SerializeField, ToggleLeft] private bool _enableStatePriority = true;

        #endregion

        #region Combat State Tracking
        
        [TitleGroup("Combat State Tracking")]
        [InfoBox("Real-time tracking of combat states and conditions for intelligent behavior.", InfoMessageType.Warning)]
        
        [ShowInInspector, ReadOnly] private CombatStateType _currentCombatState = CombatStateType.Idle;
        [ShowInInspector, ReadOnly] private CombatStateType _previousCombatState = CombatStateType.Idle;
        [ShowInInspector, ReadOnly] private float _timeInCurrentState = 0f;
        [ShowInInspector, ReadOnly] private float _lastStateChangeTime = 0f;
        
        // Combat conditions tracking
        [ShowInInspector, ReadOnly] private bool _hasEnemiesInRange = false;
        [ShowInInspector, ReadOnly] private bool _isUnderAttack = false;
        [ShowInInspector, ReadOnly] private bool _isLowHealth = false;
        [ShowInInspector, ReadOnly] private bool _isExhausted = false;
        [ShowInInspector, ReadOnly] private bool _weaponBroken = false;

        #endregion

        #region State Priority System
        
        [TitleGroup("State Priority System")]
        [InfoBox("Priority-based state system ensures critical states take precedence.", InfoMessageType.Info)]
        
        [Tooltip("State priorities for intelligent transitions")]
        [SerializeField, DictionaryDrawerSettings(KeyLabel = "State Type", ValueLabel = "Priority")]
        private Dictionary<CombatStateType, int> _statePriorities = new Dictionary<CombatStateType, int>();
        
        [ShowInInspector, ReadOnly] private Queue<StateTransition> _pendingTransitions = new Queue<StateTransition>();
        
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

        #region Component References
        
        private CombatComponent _combatComponent;
        private HealthComponent _healthComponent;
        private WeaponComponent _weaponComponent;
        private AggroDetectionComponent _aggroComponent;
        private NavigationComponent _navigationComponent;

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
            
            CacheComponentReferences();
            InitializeStateMachine();
            InitializeStatePriorities();
            SubscribeToComponentEvents();
            
            Debug.Log($"StateComponent: Enhanced state system initialized for {Entity.Id}");
        }

        private void Update()
        {
            if (!IsActive || stateMachineInGame == null) return;
            
            UpdateStateTracking();
            UpdateCombatConditions();
            ProcessIntelligentTransitions();
            ProcessPendingTransitions();
            
            stateMachineInGame.Update();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Cache references to other components
        /// </summary>
        private void CacheComponentReferences()
        {
            _combatComponent = Entity.GetComponent<CombatComponent>();
            _healthComponent = Entity.GetComponent<HealthComponent>();
            _weaponComponent = Entity.GetComponent<WeaponComponent>();
            _aggroComponent = Entity.GetComponent<AggroDetectionComponent>();
            _navigationComponent = Entity.GetComponent<NavigationComponent>();
        }

        /// <summary>
        /// Initialize state machine with enhanced combat states
        /// </summary>
        private void InitializeStateMachine()
        {
            if (stateMachineInGame == null)
            {
                GameObject machineObject = new GameObject($"EnhancedStateMachine_{Entity.Id}");
                machineObject.transform.SetParent(transform);
                
                stateMachineInGame = machineObject.AddComponent<StateMachineInGame>();
                
                RegisterEnhancedStates();
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
            
            // Existing enhanced states
            var knockbackState = new KnockbackState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<KnockbackState>(knockbackState);
            
            var stunState = new StunState(Entity, stateMachineInGame);
            stateMachineInGame.RegisterState<StunState>(stunState);
            
            // Set initial state
            stateMachineInGame.ChangeState(idleState);
            _currentCombatState = CombatStateType.Idle;
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
            if (_healthComponent != null)
            {
                _healthComponent.OnDamageTaken += OnDamageTaken;
                _healthComponent.OnDeath += OnDeath;
                _healthComponent.OnStaminaChanged += OnStaminaChanged;
            }
            
            if (_combatComponent != null)
            {
                _combatComponent.OnCombatStateChanged += OnCombatStateChangedFunc;
                _combatComponent.OnDamageReceived += OnDamageReceived;
            }
            
            if (_weaponComponent != null)
            {
                _weaponComponent.OnWeaponBroken += OnWeaponBroken;
                _weaponComponent.OnWeaponRepaired += OnWeaponRepaired;
            }
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
        /// Determine recommended state based on current conditions
        /// </summary>
        private CombatStateType DetermineRecommendedState()
        {
            // Priority-based state determination
            
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
            // Death should be handled by a separate death state if needed
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
                RequestStateTransition(CombatStateType.Idle, StateTransitionReason.CombatEnded);
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
                RequestStateTransition(CombatStateType.Idle, StateTransitionReason.WeaponRepaired);
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
                WeaponBroken = _weaponBroken
            };
        }

        /// <summary>
        /// Manually trigger state transition
        /// </summary>
        public bool TriggerStateTransition(CombatStateType targetState)
        {
            return RequestStateTransition(targetState, StateTransitionReason.Manual);
        }

        #endregion

        #region Cleanup

        public override void Cleanup()
        {
            // Unsubscribe from events
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
            
            _pendingTransitions.Clear();
            
            base.Cleanup();
        }

        #endregion

        #region Debug Methods

        [Button("Show State Info"), FoldoutGroup("Debug Tools")]
        private void ShowStateInfo()
        {
            var stateInfo = GetCurrentStateInfo();
            Debug.Log($"=== State Information ===");
            Debug.Log($"Current Combat State: {stateInfo.CombatState}");
            Debug.Log($"Time in State: {stateInfo.TimeInState:F1}s");
            Debug.Log($"Is in Combat: {stateInfo.IsInCombat}");
            Debug.Log($"Can Transition: {stateInfo.CanTransition}");
            Debug.Log($"Enemies in Range: {stateInfo.HasEnemiesInRange}");
            Debug.Log($"Under Attack: {stateInfo.IsUnderAttack}");
            Debug.Log($"Low Health: {stateInfo.IsLowHealth}");
            Debug.Log($"Exhausted: {stateInfo.IsExhausted}");
            Debug.Log($"Weapon Broken: {stateInfo.WeaponBroken}");
            Debug.Log($"Pending Transitions: {_pendingTransitions.Count}");
        }

        [Button("Force Combat State"), FoldoutGroup("Debug Tools")]
        private void ForceCombatState()
        {
            ForceStateTransition(CombatStateType.CombatEngaged);
        }

        [Button("Force Retreat State"), FoldoutGroup("Debug Tools")]
        private void ForceRetreatState()
        {
            ForceStateTransition(CombatStateType.Retreat);
        }

        #endregion
    }

    #region Supporting Data Structures

    /// <summary>
    /// Enhanced combat state types
    /// </summary>
    public enum CombatStateType
    {
        Idle,
        Patrolling,
        Guarding,
        Aggro,
        CombatEngaged,
        Retreat,
        Exhausted,
        WeaponBroken,
        Knockback,
        Stunned
    }

    /// <summary>
    /// State transition reasons for debugging and analytics
    /// </summary>
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
        EnemyDetected,
        HeavyDamage,
        StatusEffect
    }

    /// <summary>
    /// State transition information
    /// </summary>
    [Serializable]
    public struct StateTransition
    {
        public CombatStateType TargetState;
        public StateTransitionReason Reason;
        public float Timestamp;
        
        public StateTransition(CombatStateType targetState, StateTransitionReason reason, float timestamp)
        {
            TargetState = targetState;
            Reason = reason;
            Timestamp = timestamp;
        }
    }

    /// <summary>
    /// Current state information structure
    /// </summary>
    [Serializable]
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
    }

    #endregion
}