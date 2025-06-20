using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VikingRaven.Units.Components
{
    public class StateMonitor : MonoBehaviour
    {
        #region Debug Configuration
        
        [TitleGroup("Debug Configuration")]
        [InfoBox("Real-time state monitoring system. Enable visual debugging để hiển thị thông tin trong Scene view.", InfoMessageType.Info)]
        
        [Tooltip("Enable visual debugging trong Scene view")]
        [SerializeField, ToggleLeft] private bool _enableVisualDebug = true;
        
        [Tooltip("Enable console logging cho state transitions")]
        [SerializeField, ToggleLeft] private bool _enableConsoleLogging = true;
        
        [Tooltip("Enable state history tracking")]
        [SerializeField, ToggleLeft] private bool _enableStateHistory = true;
        
        [Tooltip("Maximum number of state history entries")]
        [SerializeField, Range(10, 100)] private int _maxHistoryEntries = 50;
        
        [Tooltip("Size of debug text trong Scene view")]
        [SerializeField, Range(12, 24)] private int _debugTextSize = 16;

        #endregion

        #region Real-time State Display
        
        [TitleGroup("Real-time State Information")]
        [InfoBox("Thông tin state hiện tại được cập nhật real-time", InfoMessageType.Warning)]
        
        [ShowInInspector, ReadOnly]
        [LabelText("Current State"), LabelWidth(120)]
        [PropertySpace(SpaceBefore = 5)]
        public string CurrentStateName => _stateComponent?.CurrentState?.GetType().Name ?? "None";
        
        [ShowInInspector, ReadOnly]
        [LabelText("Combat State"), LabelWidth(120)]
        [PropertyOrder(1)]
        private string CurrentCombatState => _stateComponent?.CurrentCombatState.ToString() ?? "Unknown";
        
        [ShowInInspector, ReadOnly]
        [LabelText("Time in State"), LabelWidth(120)]
        [PropertyOrder(2)]
        private string TimeInCurrentState => $"{_stateComponent?.TimeInCurrentState:F1}s";
        
        [ShowInInspector, ReadOnly]
        [LabelText("Is In Combat"), LabelWidth(120)]
        [PropertyOrder(3)]
        private bool IsInCombat => _stateComponent?.IsInCombat ?? false;
        
        [ShowInInspector, ReadOnly]
        [LabelText("Is Moving"), LabelWidth(120)]
        [PropertyOrder(4)]
        private bool IsMoving => _stateComponent?.IsMoving ?? false;

        #endregion

        #region State Conditions
        
        [TitleGroup("State Conditions")]
        [InfoBox("Các điều kiện quyết định state transitions", InfoMessageType.None)]
        
        [ShowInInspector, ReadOnly]
        [LabelText("Has Enemies"), LabelWidth(120)]
        [PropertySpace(SpaceBefore = 5)]
        private bool HasEnemiesInRange => GetStateInfo()?.HasEnemiesInRange ?? false;
        
        [ShowInInspector, ReadOnly]
        [LabelText("Under Attack"), LabelWidth(120)]
        private bool IsUnderAttack => GetStateInfo()?.IsUnderAttack ?? false;
        
        [ShowInInspector, ReadOnly]
        [LabelText("Low Health"), LabelWidth(120)]
        private bool IsLowHealth => GetStateInfo()?.IsLowHealth ?? false;
        
        [ShowInInspector, ReadOnly]
        [LabelText("Exhausted"), LabelWidth(120)]
        private bool IsExhausted => GetStateInfo()?.IsExhausted ?? false;
        
        [ShowInInspector, ReadOnly]
        [LabelText("Weapon Broken"), LabelWidth(120)]
        private bool WeaponBroken => GetStateInfo()?.WeaponBroken ?? false;

        #endregion

        #region State History
        
        [TitleGroup("State History")]
        [InfoBox("Lịch sử các state transitions để debug và analyze", InfoMessageType.Info)]
        
        [ShowInInspector, ReadOnly]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = false, HideAddButton = true, HideRemoveButton = true)]
        [PropertySpace(SpaceBefore = 10)]
        private List<StateHistoryEntry> _stateHistory = new List<StateHistoryEntry>();
        
        [ShowInInspector, ReadOnly]
        [LabelText("Total Transitions"), LabelWidth(150)]
        private int TotalTransitions => _stateHistory.Count;
        
        [ShowInInspector, ReadOnly]
        [LabelText("Avg Time per State"), LabelWidth(150)]
        private string AverageTimePerState => _stateHistory.Count > 0 ? 
            $"{_stateHistory.Average(h => h.Duration):F1}s" : "N/A";

        #endregion

        #region State Validation
        
        [TitleGroup("State Validation")]
        [InfoBox("Kiểm tra tính hợp lệ của state hiện tại", InfoMessageType.Warning)]
        
        [ShowInInspector, ReadOnly]
        [LabelText("State Valid"), LabelWidth(120)]
        [PropertySpace(SpaceBefore = 5)]
        private bool IsCurrentStateValid => ValidateCurrentState();
        
        [ShowInInspector, ReadOnly]
        [LabelText("Validation Issues"), LabelWidth(120)]
        [MultiLineProperty(3)]
        private string ValidationIssues => GetValidationIssues();
        
        [Button("Validate State Now", ButtonSizes.Medium)]
        [PropertyOrder(100)]
        public void ValidateStateNow()
        {
            var issues = GetValidationIssues();
            if (string.IsNullOrEmpty(issues))
            {
                Debug.Log($"✅ State Validation PASSED for {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ State Validation FAILED for {gameObject.name}:\n{issues}");
            }
        }

        #endregion

        #region Component References & Runtime Data
        
        private StateComponent _stateComponent;
        private Camera _sceneCamera;
        private GUIStyle _debugTextStyle;
        
        // State tracking
        private CombatStateType _lastTrackedState;
        private float _lastStateChangeTime;

        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _stateComponent = GetComponent<StateComponent>();
            _sceneCamera = Camera.main;
            
            InitializeDebugStyle();
        }
        
        private void Start()
        {
            if (_stateComponent != null)
            {
                // Subscribe to state change events
                _stateComponent.OnCombatStateChanged += OnStateChanged;
                _stateComponent.OnCombatStateEntered += OnStateEntered;
                _stateComponent.OnCombatStateExited += OnStateExited;
                
                // Initialize tracking
                _lastTrackedState = _stateComponent.CurrentCombatState;
                _lastStateChangeTime = Time.time;
                
                if (_enableConsoleLogging)
                {
                    Debug.Log($"🔍 StateMonitor: Initialized for {gameObject.name}");
                }
            }
        }
        
        private void Update()
        {
            UpdateStateTracking();
        }
        
        private void OnDestroy()
        {
            if (_stateComponent != null)
            {
                _stateComponent.OnCombatStateChanged -= OnStateChanged;
                _stateComponent.OnCombatStateEntered -= OnStateEntered;
                _stateComponent.OnCombatStateExited -= OnStateExited;
            }
        }

        #endregion

        #region State Tracking & History
        
        private void UpdateStateTracking()
        {
            if (_stateComponent == null) return;
            
            // Check for state changes
            var currentState = _stateComponent.CurrentCombatState;
            if (currentState != _lastTrackedState)
            {
                RecordStateChange(_lastTrackedState, currentState);
                _lastTrackedState = currentState;
                _lastStateChangeTime = Time.time;
            }
        }
        
        private void RecordStateChange(CombatStateType fromState, CombatStateType toState)
        {
            if (!_enableStateHistory) return;
            
            // Calculate duration in previous state
            float duration = Time.time - _lastStateChangeTime;
            
            // Add history entry
            var historyEntry = new StateHistoryEntry
            {
                FromState = fromState,
                ToState = toState,
                Timestamp = Time.time,
                Duration = duration,
                GameTime = DateTime.Now.ToString("HH:mm:ss"),
                Conditions = GetCurrentConditionsSnapshot()
            };
            
            _stateHistory.Add(historyEntry);
            
            // Limit history size
            if (_stateHistory.Count > _maxHistoryEntries)
            {
                _stateHistory.RemoveAt(0);
            }
        }
        
        private string GetCurrentConditionsSnapshot()
        {
            var info = GetStateInfo();
            if (info == null) return "No data";
            
            var conditions = new List<string>();
            if (info.Value.HasEnemiesInRange) conditions.Add("Enemies");
            if (info.Value.IsUnderAttack) conditions.Add("Under Attack");
            if (info.Value.IsLowHealth) conditions.Add("Low HP");
            if (info.Value.IsExhausted) conditions.Add("Exhausted");
            if (info.Value.WeaponBroken) conditions.Add("Weapon Broken");
            if (info.Value.IsMoving) conditions.Add("Moving");
            
            return conditions.Count > 0 ? string.Join(", ", conditions) : "Normal";
        }

        #endregion

        #region Event Handlers
        
        private void OnStateChanged(CombatStateType oldState, CombatStateType newState)
        {
            if (_enableConsoleLogging)
            {
                Debug.Log($"🔄 StateMonitor [{gameObject.name}]: {oldState} → {newState} " +
                         $"(Conditions: {GetCurrentConditionsSnapshot()})");
            }
        }
        
        private void OnStateEntered(CombatStateType state)
        {
            if (_enableConsoleLogging)
            {
                Debug.Log($"➡️ StateMonitor [{gameObject.name}]: Entered {state}");
            }
        }
        
        private void OnStateExited(CombatStateType state)
        {
            if (_enableConsoleLogging)
            {
                Debug.Log($"⬅️ StateMonitor [{gameObject.name}]: Exited {state}");
            }
        }

        #endregion

        #region State Validation
        
        private bool ValidateCurrentState()
        {
            if (_stateComponent == null) return false;
            
            var info = GetStateInfo();
            if (!info.HasValue) return false;
            
            var currentState = _stateComponent.CurrentCombatState;
            
            // Validate state logic
            switch (currentState)
            {
                case CombatStateType.CombatEngaged:
                    return info.Value.HasEnemiesInRange && !info.Value.IsExhausted && !info.Value.WeaponBroken;
                    
                case CombatStateType.Retreat:
                    return info.Value.IsLowHealth || info.Value.IsExhausted || info.Value.WeaponBroken;
                    
                case CombatStateType.Exhausted:
                    return info.Value.IsExhausted;
                    
                case CombatStateType.WeaponBroken:
                    return info.Value.WeaponBroken;
                    
                case CombatStateType.Patrolling:
                    return info.Value.IsMoving && !info.Value.IsInCombat;
                    
                case CombatStateType.Idle:
                    return !info.Value.IsMoving && !info.Value.IsInCombat && !info.Value.HasEnemiesInRange;
                    
                default:
                    return true; // Default states are always valid
            }
        }
        
        private string GetValidationIssues()
        {
            if (_stateComponent == null) return "StateComponent missing";
            
            var info = GetStateInfo();
            if (!info.HasValue) return "Cannot get state info";
            
            var currentState = _stateComponent.CurrentCombatState;
            var issues = new List<string>();
            
            // Check for contradictions
            switch (currentState)
            {
                case CombatStateType.CombatEngaged:
                    if (!info.Value.HasEnemiesInRange)
                        issues.Add("In combat but no enemies detected");
                    if (info.Value.IsExhausted)
                        issues.Add("In combat while exhausted");
                    if (info.Value.WeaponBroken)
                        issues.Add("In combat with broken weapon");
                    break;
                    
                case CombatStateType.Retreat:
                    if (!info.Value.IsLowHealth && !info.Value.IsExhausted && !info.Value.WeaponBroken)
                        issues.Add("Retreating without valid reason");
                    break;
                    
                case CombatStateType.Idle:
                    if (info.Value.HasEnemiesInRange)
                        issues.Add("Idle with enemies nearby");
                    if (info.Value.IsMoving)
                        issues.Add("Idle while moving");
                    break;
                    
                case CombatStateType.Patrolling:
                    if (!info.Value.IsMoving)
                        issues.Add("Patrolling while not moving");
                    if (info.Value.IsInCombat)
                        issues.Add("Patrolling while in combat");
                    break;
            }
            
            return issues.Count > 0 ? string.Join("\n", issues) : "";
        }

        #endregion

        #region Visual Debug (Scene View)
        
        private void OnDrawGizmos()
        {
            if (!_enableVisualDebug || _stateComponent == null) return;
            
            DrawStateGizmo();
            DrawStateText();
        }
        
        private void DrawStateGizmo()
        {
            // Color based on state
            var color = GetStateColor(_stateComponent.CurrentCombatState);
            Gizmos.color = color;
            
            // Draw state indicator sphere
            var position = transform.position + Vector3.up * 2f;
            Gizmos.DrawWireSphere(position, 0.5f);
            
            // Draw movement indicator
            if (IsMoving)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f, Vector3.one * 0.3f);
            }
            
            // Draw combat indicator
            if (IsInCombat)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 0.3f);
            }
        }
        
        private void DrawStateText()
        {
            if (_sceneCamera == null) return;
            
            var worldPosition = transform.position + Vector3.up * 3.5f;
            var screenPosition = _sceneCamera.WorldToScreenPoint(worldPosition);
            
            if (screenPosition.z > 0)
            {
                var rect = new Rect(screenPosition.x - 100, Screen.height - screenPosition.y - 60, 200, 60);
                
                var text = $"{CurrentStateName}\n{CurrentCombatState}\n{TimeInCurrentState}";
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(worldPosition, text, _debugTextStyle);
                #endif
            }
        }
        
        private Color GetStateColor(CombatStateType state)
        {
            return state switch
            {
                CombatStateType.Idle => Color.green,
                CombatStateType.Patrolling => Color.blue,
                CombatStateType.Aggro => Color.yellow,
                CombatStateType.CombatEngaged => Color.red,
                CombatStateType.Retreat => Color.magenta,
                CombatStateType.Exhausted => Color.gray,
                CombatStateType.WeaponBroken => Color.black,
                CombatStateType.Stunned => Color.white,
                CombatStateType.Knockback => Color.cyan,
                _ => Color.gray
            };
        }
        
        private void InitializeDebugStyle()
        {
            _debugTextStyle = new GUIStyle();
            _debugTextStyle.fontSize = _debugTextSize;
            _debugTextStyle.normal.textColor = Color.white;
            _debugTextStyle.alignment = TextAnchor.MiddleCenter;
            _debugTextStyle.fontStyle = FontStyle.Bold;
        }

        #endregion

        #region Helper Methods
        
        private StateInfo? GetStateInfo()
        {
            return _stateComponent?.GetCurrentStateInfo();
        }

        #endregion

        #region Debug Controls
        
        [TitleGroup("Debug Controls")]
        [Button("Clear History", ButtonSizes.Medium)]
        [PropertySpace(SpaceBefore = 10)]
        public void ClearHistory()
        {
            _stateHistory.Clear();
            Debug.Log($"🗑️ StateMonitor: History cleared for {gameObject.name}");
        }
        
        [Button("Force State Validation", ButtonSizes.Medium)]
        public void ForceValidation()
        {
            ValidateStateNow();
        }
        
        [Button("Log Complete State Report", ButtonSizes.Large)]
        [PropertySpace(SpaceAfter = 10)]
        public void LogCompleteReport()
        {
            var report = GenerateCompleteStateReport();
            Debug.Log(report);
        }
        
        private string GenerateCompleteStateReport()
        {
            var report = $"=== COMPLETE STATE REPORT for {gameObject.name} ===\n\n";
            
            // Current state info
            report += "CURRENT STATE:\n";
            report += $"  State Machine: {CurrentStateName}\n";
            report += $"  Combat State: {CurrentCombatState}\n";
            report += $"  Time in State: {TimeInCurrentState}\n";
            report += $"  In Combat: {IsInCombat}\n";
            report += $"  Is Moving: {IsMoving}\n\n";
            
            // Conditions
            report += "CONDITIONS:\n";
            report += $"  Has Enemies: {HasEnemiesInRange}\n";
            report += $"  Under Attack: {IsUnderAttack}\n";
            report += $"  Low Health: {IsLowHealth}\n";
            report += $"  Exhausted: {IsExhausted}\n";
            report += $"  Weapon Broken: {WeaponBroken}\n\n";
            
            // Validation
            report += "VALIDATION:\n";
            report += $"  State Valid: {IsCurrentStateValid}\n";
            var issues = GetValidationIssues();
            report += $"  Issues: {(string.IsNullOrEmpty(issues) ? "None" : issues)}\n\n";
            
            // History summary
            report += "HISTORY SUMMARY:\n";
            report += $"  Total Transitions: {TotalTransitions}\n";
            report += $"  Average Time per State: {AverageTimePerState}\n";
            
            if (_stateHistory.Count > 0)
            {
                report += "  Recent Transitions:\n";
                var recent = _stateHistory.TakeLast(5);
                foreach (var entry in recent)
                {
                    report += $"    {entry.GameTime}: {entry.FromState} → {entry.ToState} ({entry.Duration:F1}s)\n";
                }
            }
            
            return report;
        }

        #endregion
    }
    
    /// <summary>
    /// State history entry cho tracking state transitions
    /// </summary>
    [System.Serializable]
    public class StateHistoryEntry
    {
        [LabelText("From → To"), LabelWidth(80)]
        public string Transition => $"{FromState} → {ToState}";
        
        [LabelText("Duration"), LabelWidth(80)]
        public float Duration;
        
        [LabelText("Time"), LabelWidth(80)]
        public string GameTime;
        
        [LabelText("Conditions"), LabelWidth(80)]
        [MultiLineProperty(2)]
        public string Conditions;
        
        [HideInInspector]
        public CombatStateType FromState;
        
        [HideInInspector]
        public CombatStateType ToState;
        
        [HideInInspector]
        public float Timestamp;
    }
}