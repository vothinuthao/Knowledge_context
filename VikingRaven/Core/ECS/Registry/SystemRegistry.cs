using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VikingRaven.Core.ECS
{
    /// <summary>
    /// Optimized System Registry using manual registration instead of FindObjectsOfType
    /// Uses event-driven architecture and coroutine-based updates for better performance
    /// </summary>
    public class SystemRegistry : MonoBehaviour
    {
        #region System Registration Configuration
        
        [TitleGroup("System Registration")]
        [Tooltip("Manually register systems here instead of using FindObjectsOfType")]
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true, ShowPaging = true, NumberOfItemsPerPage = 10)]
        private List<MonoBehaviour> _registeredSystems = new List<MonoBehaviour>();
        
        [TitleGroup("System Registration")]
        [Tooltip("Auto-find systems in children on start (fallback option)")]
        [SerializeField, ToggleLeft]
        private bool _autoFindSystemsInChildren = true;
        
        [TitleGroup("System Registration")]
        [Tooltip("Parent objects containing systems")]
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true)]
        private List<Transform> _systemContainers = new List<Transform>();

        #endregion

        #region Update Configuration
        
        [TitleGroup("Update Configuration")]
        [Tooltip("Update frequency for different system types")]
        [SerializeField, Range(1, 120)]
        private int _highFrequencySystemsPerSecond = 60;
        
        [TitleGroup("Update Configuration")]
        [Tooltip("Update frequency for medium priority systems")]
        [SerializeField, Range(1, 60)]
        private int _mediumFrequencySystemsPerSecond = 30;
        
        [TitleGroup("Update Configuration")]
        [Tooltip("Update frequency for low priority systems")]
        [SerializeField, Range(1, 30)]
        private int _lowFrequencySystemsPerSecond = 10;
        
        [TitleGroup("Update Configuration")]
        [Tooltip("Enable time slicing for heavy operations")]
        [SerializeField, ToggleLeft]
        private bool _enableTimeSlicing = true;
        
        [TitleGroup("Update Configuration")]
        [Tooltip("Maximum milliseconds per frame for system updates")]
        [SerializeField, Range(1, 16)]
        private float _maxMillisecondsPerFrame = 8f;

        #endregion

        #region Runtime Data
        
        [TitleGroup("Runtime Information")]
        [ShowInInspector, ReadOnly]
        private Dictionary<SystemPriority, List<ISystem>> _systemsByPriority = new Dictionary<SystemPriority, List<ISystem>>();
        
        [ShowInInspector, ReadOnly]
        private int _totalRegisteredSystems = 0;
        
        [ShowInInspector, ReadOnly]
        private bool _isInitialized = false;
        
        [ShowInInspector, ReadOnly]
        private bool _isRunning = false;
        
        [ShowInInspector, ReadOnly, ProgressBar(0, 16)]
        private float _lastFrameExecutionTime = 0f;

        #endregion

        #region Private Fields
        
        // System tracking
        private readonly Dictionary<Type, ISystem> _systemsByType = new Dictionary<Type, ISystem>();
        private readonly List<ISystem> _allSystems = new List<ISystem>();
        
        // Coroutine tracking
        private Coroutine _highFrequencyUpdateCoroutine;
        private Coroutine _mediumFrequencyUpdateCoroutine;
        private Coroutine _lowFrequencyUpdateCoroutine;
        
        // Performance tracking
        private readonly Queue<float> _executionTimeHistory = new Queue<float>();
        private const int MAX_EXECUTION_HISTORY = 60;

        #endregion

        #region Events
        
        public event Action<ISystem> OnSystemRegistered;
        public event Action<ISystem> OnSystemUnregistered;
        public event Action OnAllSystemsInitialized;
        public event Action OnSystemUpdateCompleted;

        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializePriorityDictionary();
        }
        
        private void Start()
        {
            StartCoroutine(InitializeSystemsCoroutine());
        }
        
        private void OnDestroy()
        {
            StopAllUpdateCoroutines();
            CleanupAllSystems();
        }

        #endregion

        #region Initialization
        
        /// <summary>
        /// Initialize priority dictionary
        /// </summary>
        private void InitializePriorityDictionary()
        {
            _systemsByPriority[SystemPriority.Critical] = new List<ISystem>();
            _systemsByPriority[SystemPriority.High] = new List<ISystem>();
            _systemsByPriority[SystemPriority.Medium] = new List<ISystem>();
            _systemsByPriority[SystemPriority.Low] = new List<ISystem>();
        }
        
        /// <summary>
        /// Initialize systems using coroutine for better performance
        /// </summary>
        private IEnumerator InitializeSystemsCoroutine()
        {
            Debug.Log("OptimizedSystemRegistry: Starting system initialization...");
            
            // Register manually assigned systems first
            yield return StartCoroutine(RegisterManualSystemsCoroutine());
            
            // Auto-find systems in children if enabled
            if (_autoFindSystemsInChildren)
            {
                yield return StartCoroutine(RegisterSystemsInContainersCoroutine());
            }
            
            // Initialize all registered systems
            yield return StartCoroutine(InitializeAllSystemsCoroutine());
            
            // Start update coroutines
            StartUpdateCoroutines();
            
            _isInitialized = true;
            _isRunning = true;
            
            OnAllSystemsInitialized?.Invoke();
            Debug.Log($"OptimizedSystemRegistry: Initialized {_totalRegisteredSystems} systems successfully");
        }
        
        /// <summary>
        /// Register manually assigned systems
        /// </summary>
        private IEnumerator RegisterManualSystemsCoroutine()
        {
            int processedCount = 0;
            var startTime = Time.realtimeSinceStartup;
            
            foreach (var systemComponent in _registeredSystems)
            {
                if (systemComponent != null && systemComponent is ISystem system)
                {
                    RegisterSystemInternal(system);
                    processedCount++;
                    
                    // Time slicing: yield if we've taken too much time
                    if (_enableTimeSlicing && (Time.realtimeSinceStartup - startTime) * 1000 > _maxMillisecondsPerFrame)
                    {
                        yield return null;
                        startTime = Time.realtimeSinceStartup;
                    }
                }
            }
            
            Debug.Log($"OptimizedSystemRegistry: Registered {processedCount} manual systems");
        }
        
        /// <summary>
        /// Register systems in specified containers (optimized FindObjectsOfType alternative)
        /// </summary>
        private IEnumerator RegisterSystemsInContainersCoroutine()
        {
            var startTime = Time.realtimeSinceStartup;
            
            foreach (var container in _systemContainers)
            {
                if (!container) continue;
                var systemsInContainer = container.GetComponentsInChildren<MonoBehaviour>();
                
                foreach (var component in systemsInContainer)
                {
                    if (component is ISystem system && !_allSystems.Contains(system))
                    {
                        RegisterSystemInternal(system);
                        if (_enableTimeSlicing && (Time.realtimeSinceStartup - startTime) * 1000 > _maxMillisecondsPerFrame)
                        {
                            yield return null;
                            startTime = Time.realtimeSinceStartup;
                        }
                    }
                }
            }
            
        }
        
        /// <summary>
        /// Initialize all registered systems
        /// </summary>
        private IEnumerator InitializeAllSystemsCoroutine()
        {
            var startTime = Time.realtimeSinceStartup;
            
            foreach (var system in _allSystems)
            {
                try
                {
                    system.Initialize();
                    
                    if (_enableTimeSlicing && (Time.realtimeSinceStartup - startTime) * 1000 > _maxMillisecondsPerFrame)
                    {
                        startTime = Time.realtimeSinceStartup;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"OptimizedSystemRegistry: Failed to initialize system {system.GetType().Name}: {ex.Message}");
                }
            }
            yield break;
        }

        #endregion

        #region System Registration
        
        /// <summary>
        /// Manually register a system
        /// </summary>
        public void RegisterSystem(ISystem system)
        {
            if (system == null)
            {
                Debug.LogWarning("OptimizedSystemRegistry: Attempting to register null system");
                return;
            }
            
            RegisterSystemInternal(system);
            
            // Initialize immediately if registry is already initialized
            if (_isInitialized)
            {
                try
                {
                    system.Initialize();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"OptimizedSystemRegistry: Failed to initialize late-registered system {system.GetType().Name}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Internal system registration logic
        /// </summary>
        private void RegisterSystemInternal(ISystem system)
        {
            if (_allSystems.Contains(system))
            {
                Debug.LogWarning($"OptimizedSystemRegistry: System {system.GetType().Name} is already registered");
                return;
            }
            
            // Add to main collection
            _allSystems.Add(system);
            _systemsByType[system.GetType()] = system;
            
            // Add to priority-based collection
            SystemPriority priority = GetSystemPriority(system);
            _systemsByPriority[priority].Add(system);
            
            _totalRegisteredSystems++;
            
            OnSystemRegistered?.Invoke(system);
            Debug.Log($"OptimizedSystemRegistry: Registered system {system.GetType().Name} with priority {priority}");
        }
        
        /// <summary>
        /// Unregister a system
        /// </summary>
        public void UnregisterSystem(ISystem system)
        {
            if (system == null || !_allSystems.Contains(system)) return;
            
            // Remove from collections
            _allSystems.Remove(system);
            _systemsByType.Remove(system.GetType());
            
            // Remove from priority collections
            foreach (var priorityList in _systemsByPriority.Values)
            {
                priorityList.Remove(system);
            }
            
            _totalRegisteredSystems--;
            
            OnSystemUnregistered?.Invoke(system);
            Debug.Log($"OptimizedSystemRegistry: Unregistered system {system.GetType().Name}");
        }
        
        /// <summary>
        /// Determine system priority based on type and interfaces
        /// </summary>
        private SystemPriority GetSystemPriority(ISystem system)
        {
            // Check for priority attribute first
            var priorityAttribute = system.GetType().GetCustomAttributes(typeof(SystemPriorityAttribute), false)
                                         .FirstOrDefault() as SystemPriorityAttribute;
            
            if (priorityAttribute != null)
            {
                return priorityAttribute.Priority;
            }
            
            // Auto-determine priority based on system type name
            string typeName = system.GetType().Name.ToLower();
            
            if (typeName.Contains("critical") || typeName.Contains("input") || typeName.Contains("physics"))
                return SystemPriority.Critical;
            
            if (typeName.Contains("movement") || typeName.Contains("combat") || typeName.Contains("state"))
                return SystemPriority.High;
            
            if (typeName.Contains("ai") || typeName.Contains("behavior") || typeName.Contains("formation"))
                return SystemPriority.Medium;
            
            return SystemPriority.Low; // Default for UI, effects, etc.
        }

        #endregion

        #region Optimized Update System
        
        /// <summary>
        /// Start all update coroutines
        /// </summary>
        private void StartUpdateCoroutines()
        {
            StopAllUpdateCoroutines(); // Ensure we don't have duplicates
            
            _highFrequencyUpdateCoroutine = StartCoroutine(HighFrequencyUpdateCoroutine());
            _mediumFrequencyUpdateCoroutine = StartCoroutine(MediumFrequencyUpdateCoroutine());
            _lowFrequencyUpdateCoroutine = StartCoroutine(LowFrequencyUpdateCoroutine());
            
            Debug.Log("OptimizedSystemRegistry: Started all update coroutines");
        }
        
        /// <summary>
        /// Stop all update coroutines
        /// </summary>
        private void StopAllUpdateCoroutines()
        {
            if (_highFrequencyUpdateCoroutine != null)
            {
                StopCoroutine(_highFrequencyUpdateCoroutine);
                _highFrequencyUpdateCoroutine = null;
            }
            
            if (_mediumFrequencyUpdateCoroutine != null)
            {
                StopCoroutine(_mediumFrequencyUpdateCoroutine);
                _mediumFrequencyUpdateCoroutine = null;
            }
            
            if (_lowFrequencyUpdateCoroutine != null)
            {
                StopCoroutine(_lowFrequencyUpdateCoroutine);
                _lowFrequencyUpdateCoroutine = null;
            }
        }
        
        /// <summary>
        /// High frequency update for critical systems
        /// </summary>
        private IEnumerator HighFrequencyUpdateCoroutine()
        {
            var waitTime = new WaitForSeconds(1f / _highFrequencySystemsPerSecond);
            
            while (_isRunning)
            {
                var startTime = Time.realtimeSinceStartup;
                
                yield return StartCoroutine(ExecuteSystemsByPriority(SystemPriority.Critical));
                yield return StartCoroutine(ExecuteSystemsByPriority(SystemPriority.High));
                
                var executionTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                TrackExecutionTime(executionTime);
                
                yield return waitTime;
            }
        }
        
        /// <summary>
        /// Medium frequency update for normal systems
        /// </summary>
        private IEnumerator MediumFrequencyUpdateCoroutine()
        {
            var waitTime = new WaitForSeconds(1f / _mediumFrequencySystemsPerSecond);
            
            while (_isRunning)
            {
                yield return StartCoroutine(ExecuteSystemsByPriority(SystemPriority.Medium));
                yield return waitTime;
            }
        }
        
        /// <summary>
        /// Low frequency update for background systems
        /// </summary>
        private IEnumerator LowFrequencyUpdateCoroutine()
        {
            var waitTime = new WaitForSeconds(1f / _lowFrequencySystemsPerSecond);
            
            while (_isRunning)
            {
                yield return StartCoroutine(ExecuteSystemsByPriority(SystemPriority.Low));
                yield return waitTime;
            }
        }
        
        /// <summary>
        /// Execute systems by priority with time slicing
        /// </summary>
        private IEnumerator ExecuteSystemsByPriority(SystemPriority priority)
        {
            if (!_systemsByPriority.TryGetValue(priority, out var systems)) yield break;
            
            var startTime = Time.realtimeSinceStartup;
            
            foreach (var system in systems)
            {
                try
                {
                    system.Execute();
                    
                    if (_enableTimeSlicing && (Time.realtimeSinceStartup - startTime) * 1000 > _maxMillisecondsPerFrame)
                    {
                        // yield return null;
                        startTime = Time.realtimeSinceStartup;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"OptimizedSystemRegistry: Error executing system {system.GetType().Name}: {ex.Message}");
                }
            }
        }

        #endregion

        #region Performance Tracking
        
        /// <summary>
        /// Track execution time for performance monitoring
        /// </summary>
        private void TrackExecutionTime(float executionTime)
        {
            _lastFrameExecutionTime = executionTime;
            
            _executionTimeHistory.Enqueue(executionTime);
            if (_executionTimeHistory.Count > MAX_EXECUTION_HISTORY)
            {
                _executionTimeHistory.Dequeue();
            }
        }
        
        /// <summary>
        /// Get average execution time
        /// </summary>
        [ShowInInspector, ReadOnly]
        [TitleGroup("Performance Metrics")]
        public float AverageExecutionTime
        {
            get
            {
                if (_executionTimeHistory.Count == 0) return 0f;
                return _executionTimeHistory.Average();
            }
        }
        
        /// <summary>
        /// Get maximum execution time
        /// </summary>
        [ShowInInspector, ReadOnly]
        [TitleGroup("Performance Metrics")]
        public float MaxExecutionTime
        {
            get
            {
                if (_executionTimeHistory.Count == 0) return 0f;
                return _executionTimeHistory.Max();
            }
        }

        #endregion

        #region Public Interface
        
        /// <summary>
        /// Get system by type
        /// </summary>
        public T GetSystem<T>() where T : class, ISystem
        {
            if (_systemsByType.TryGetValue(typeof(T), out var system))
            {
                return system as T;
            }
            return null;
        }
        
        /// <summary>
        /// Check if system is registered
        /// </summary>
        public bool HasSystem<T>() where T : class, ISystem
        {
            return _systemsByType.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// Get all systems of specific priority
        /// </summary>
        public List<ISystem> GetSystemsByPriority(SystemPriority priority)
        {
            return new List<ISystem>(_systemsByPriority[priority]);
        }
        
        /// <summary>
        /// Pause all system updates
        /// </summary>
        public void PauseUpdates()
        {
            _isRunning = false;
            Debug.Log("OptimizedSystemRegistry: Paused all system updates");
        }
        
        /// <summary>
        /// Resume all system updates
        /// </summary>
        public void ResumeUpdates()
        {
            if (!_isRunning && _isInitialized)
            {
                _isRunning = true;
                StartUpdateCoroutines();
                Debug.Log("OptimizedSystemRegistry: Resumed all system updates");
            }
        }

        #endregion

        #region Cleanup
        
        /// <summary>
        /// Cleanup all systems
        /// </summary>
        private void CleanupAllSystems()
        {
            foreach (var system in _allSystems)
            {
                try
                {
                    system.Cleanup();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"OptimizedSystemRegistry: Error cleaning up system {system.GetType().Name}: {ex.Message}");
                }
            }
            
            _allSystems.Clear();
            _systemsByType.Clear();
            foreach (var priorityList in _systemsByPriority.Values)
            {
                priorityList.Clear();
            }
            
            _totalRegisteredSystems = 0;
            _isInitialized = false;
            _isRunning = false;
        }

        #endregion

        #region Debug Tools
        
        [Button("Force Refresh Systems"), TitleGroup("Debug Tools")]
        public void ForceRefreshSystems()
        {
            if (!Application.isPlaying) return;
            
            StopAllUpdateCoroutines();
            CleanupAllSystems();
            StartCoroutine(InitializeSystemsCoroutine());
        }
        
        [Button("Show System Statistics"), TitleGroup("Debug Tools")]
        public void ShowSystemStatistics()
        {
            string stats = "=== System Registry Statistics ===\n";
            stats += $"Total Systems: {_totalRegisteredSystems}\n";
            stats += $"Is Initialized: {_isInitialized}\n";
            stats += $"Is Running: {_isRunning}\n";
            stats += $"Average Execution Time: {AverageExecutionTime:F2}ms\n";
            stats += $"Max Execution Time: {MaxExecutionTime:F2}ms\n\n";
            
            stats += "Systems by Priority:\n";
            foreach (var kvp in _systemsByPriority)
            {
                stats += $"  {kvp.Key}: {kvp.Value.Count} systems\n";
                foreach (var system in kvp.Value)
                {
                    stats += $"    - {system.GetType().Name}\n";
                }
            }
            
            Debug.Log(stats);
        }

        #endregion
    }
    
    /// <summary>
    /// System priority levels for update frequency
    /// </summary>
    public enum SystemPriority
    {
        Critical,   // 60 FPS - Input, Physics, Critical gameplay
        High,       // 60 FPS - Movement, Combat, State changes
        Medium,     // 30 FPS - AI, Behavior, Formation management
        Low         // 10 FPS - UI updates, Effects, Background tasks
    }
    
    /// <summary>
    /// Attribute to specify system priority
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SystemPriorityAttribute : Attribute
    {
        public SystemPriority Priority { get; }
        
        public SystemPriorityAttribute(SystemPriority priority)
        {
            Priority = priority;
        }
    }
}