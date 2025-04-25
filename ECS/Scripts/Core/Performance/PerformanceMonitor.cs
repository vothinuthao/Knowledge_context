using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Performance
{
    /// <summary>
    /// Monitor system performance
    /// </summary>
    public class PerformanceMonitor
    {
        private Dictionary<string, float> _timings;
        private Dictionary<string, float> _averageTimings;
        private Dictionary<string, int> _callCounts;
        private float _frameStartTime;
        private int _frameCount;
        private System.Diagnostics.Stopwatch _stopwatch;
        
        public PerformanceMonitor()
        {
            _timings = new Dictionary<string, float>();
            _averageTimings = new Dictionary<string, float>();
            _callCounts = new Dictionary<string, int>();
            _stopwatch = new System.Diagnostics.Stopwatch();
        }
        
        public void StartFrame()
        {
            _frameStartTime = Time.realtimeSinceStartup;
            _frameCount++;
        }
        
        public void EndFrame()
        {
            float frameTime = Time.realtimeSinceStartup - _frameStartTime;
            
            // Log if frame time exceeds target
            if (frameTime > 16.67f) // 60 FPS target
            {
                Debug.LogWarning($"Frame {_frameCount} took {frameTime:F2}ms");
            }
        }
        
        public void StartMeasure(string name)
        {
            _stopwatch.Restart();
        }
        
        public void EndMeasure(string name)
        {
            _stopwatch.Stop();
            float timeMs = (float)_stopwatch.Elapsed.TotalMilliseconds;
            
            if (!_timings.ContainsKey(name))
            {
                _timings[name] = timeMs;
                _averageTimings[name] = timeMs;
                _callCounts[name] = 1;
            }
            else
            {
                _timings[name] = timeMs;
                _callCounts[name]++;
                
                // Calculate running average
                float total = _averageTimings[name] * (_callCounts[name] - 1) + timeMs;
                _averageTimings[name] = total / _callCounts[name];
            }
            
            // Warning if operation takes too long
            if (timeMs > 1.0f)
            {
                Debug.LogWarning($"{name} took {timeMs:F2}ms");
            }
        }
        
        public PerformanceReport GetReport()
        {
            return new PerformanceReport
            {
                FrameCount = _frameCount,
                Timings = new Dictionary<string, float>(_timings),
                AverageTimings = new Dictionary<string, float>(_averageTimings),
                CallCounts = new Dictionary<string, int>(_callCounts)
            };
        }
    }
    
    public class PerformanceReport
    {
        public int FrameCount { get; set; }
        public Dictionary<string, float> Timings { get; set; }
        public Dictionary<string, float> AverageTimings { get; set; }
        public Dictionary<string, int> CallCounts { get; set; }
        
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Performance Report (Frame {FrameCount})");
            sb.AppendLine("----------------------------------------");
            
            foreach (var kvp in AverageTimings.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value:F2}ms avg ({CallCounts[kvp.Key]} calls)");
            }
            
            return sb.ToString();
        }
    }
}