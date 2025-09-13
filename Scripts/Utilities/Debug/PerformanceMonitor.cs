using System;
using UnityEngine;
using UnityEngine.Profiling;
using NUnit.Framework;

namespace RavenDeckbuilding.Utilities
{
    public class PerformanceMonitor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private float updateInterval = 1f;
        
        [Header("Display")]
        [SerializeField] private bool showOnScreenStats = true;
        [SerializeField] private Vector2 displayPosition = new Vector2(10, 10);
        
        // Performance metrics
        private float fps;
        private long memoryUsage;
        private float frameTime;
        
        // Update tracking
        private float timer;
        private int frameCount;
        private float deltaTimeSum;
        
        [Obsolete("Obsolete")]
        void Update()
        {
            if (!enableMonitoring) return;
            
            frameCount++;
            deltaTimeSum += Time.unscaledDeltaTime;
            timer += Time.unscaledDeltaTime;
            
            if (timer >= updateInterval)
            {
                // Calculate FPS
                fps = frameCount / timer;
                
                // Calculate average frame time
                frameTime = (deltaTimeSum / frameCount) * 1000f; // Convert to ms
                
                // Get memory usage
                memoryUsage = Profiler.GetTotalAllocatedMemory() / (1024 * 1024); // Convert to MB
                
                // Reset counters
                timer = 0;
                frameCount = 0;
                deltaTimeSum = 0;
                
                // Log warnings
                if (fps < 50f)
                {
                    Debug.LogWarning($"[Performance] Low FPS: {fps:F1}");
                }
                
                if (frameTime > 20f)
                {
                    Debug.LogWarning($"[Performance] High frame time: {frameTime:F2}ms");
                }
            }
        }
        
        void OnGUI()
        {
            if (!enableMonitoring || !showOnScreenStats) return;
            
            GUI.color = fps < 50 ? Color.red : Color.green;
            
            string stats = $"FPS: {fps:F1}\n" +
                          $"Frame Time: {frameTime:F2}ms\n" +
                          $"Memory: {memoryUsage}MB";
            
            GUI.Label(new Rect(displayPosition.x, displayPosition.y, 200, 100), stats);
        }
    }
}