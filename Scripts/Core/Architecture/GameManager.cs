using System.Collections.Generic;
using UnityEngine;
using RavenDeckbuilding.Core;
using RavenDeckbuilding.Core.Architecture.Singleton;

namespace RavenDeckbuilding
{
    /// <summary>
    /// Main game manager - coordinates all systems
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Debug")]
        [SerializeField] private bool enableDebugMode = true;
        [SerializeField] private bool showPerformanceStats = true;
        
        private List<IGameSystem> _gameSystems;
        private bool _isInitialized = false;
        
        // Performance tracking
        private float _lastFrameTime;
        private float _averageFrameTime;
        private int _frameCount;
        
        private void InitializeGameSystems()
        {
            _gameSystems = new List<IGameSystem>();
            
            // TODO: Add systems in dependency order
            // gameSystems.Add(inputSystem);
            // gameSystems.Add(commandSystem);
            // gameSystems.Add(cardSystem);
            // gameSystems.Add(eventSystem);
            
            foreach (var system in _gameSystems)
            {
                try
                {
                    system.Initialize();
                    Debug.Log($"[GameManager] Initialized: {system.GetType().Name}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameManager] Failed to initialize {system.GetType().Name}: {ex.Message}");
                }
            }
            
            _isInitialized = true;
        }
        
        void Update()
        {
            if (!_isInitialized) return;
            
            float deltaTime = Time.deltaTime;
            
            // Update all systems
            foreach (var system in _gameSystems)
            {
                try
                {
                    system.Update(deltaTime);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameManager] System update error {system.GetType().Name}: {ex.Message}");
                }
            }
            
            // Performance tracking
            if (showPerformanceStats)
            {
                UpdatePerformanceStats(deltaTime);
            }
        }
        
        private void UpdatePerformanceStats(float deltaTime)
        {
            _frameCount++;
            float currentFrameTime = deltaTime * 1000f; // Convert to ms
            
            if (_frameCount == 1)
            {
                _averageFrameTime = currentFrameTime;
            }
            else
            {
                _averageFrameTime = (_averageFrameTime * (_frameCount - 1) + currentFrameTime) / _frameCount;
            }
            
            // Warning for performance issues
            if (currentFrameTime > 16.67f) // 60 FPS threshold
            {
                Debug.LogWarning($"[Performance] Frame spike: {currentFrameTime:F2}ms (avg: {_averageFrameTime:F2}ms)");
            }
        }
        
        void OnDestroy()
        {
            if (_gameSystems != null)
            {
                foreach (var system in _gameSystems)
                {
                    try
                    {
                        system.Shutdown();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[GameManager] System shutdown error: {ex.Message}");
                    }
                }
            }
        }
        
        // Public API
        public T GetSystem<T>() where T : class, IGameSystem
        {
            foreach (var system in _gameSystems)
            {
                if (system is T targetSystem)
                    return targetSystem;
            }
            return null;
        }
        
        public bool IsSystemInitialized<T>() where T : class, IGameSystem
        {
            var system = GetSystem<T>();
            return system?.IsInitialized ?? false;
        }
    }
}