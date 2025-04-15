using System.Collections.Generic;
using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Extended version of SteeringContext with support for context steering
    /// This partial class adds to the existing SteeringContext
    /// </summary>
    public partial class SteeringContext
    {
        // Perception data
        public List<Transform> VisibleNeighbors { get; set; } = new List<Transform>();
        public List<Transform> VisibleEnemies { get; set; } = new List<Transform>();
        public List<Transform> VisibleObstacles { get; set; } = new List<Transform>();
        public Dictionary<Transform, float> MemoryEntities { get; set; } = new Dictionary<Transform, float>();
        public float[] InterestMap { get; set; }
        public float[] DangerMap { get; set; }
        public int DirectionCount { get; set; } = 16;
        public float DynamicSeparationWeight { get; set; } = 1.0f;
        public float DynamicCohesionWeight { get; set; } = 1.0f;
        public float DynamicAlignmentWeight { get; set; } = 1.0f;
        public Dictionary<string, object> Blackboard { get; set; } = new Dictionary<string, object>();
        
        public void SetBlackboardValue<T>(string key, T value)
        {
            Blackboard[key] = value;
        }
        
        public T GetBlackboardValue<T>(string key, T defaultValue = default)
        {
            if (Blackboard.TryGetValue(key, out object value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        public void InitializeMaps()
        {
            InterestMap = new float[DirectionCount];
            DangerMap = new float[DirectionCount];
        }
        public Vector3 GetDirectionFromIndex(int index)
        {
            float angle = (index / (float)DirectionCount) * 360f;
            return Quaternion.Euler(0, angle, 0) * Vector3.forward;
        }
        public int GetIndexFromDirection(Vector3 direction)
        {
            direction = direction.normalized;
            float angle = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up);
            if (angle < 0) angle += 360f;
            
            int index = Mathf.RoundToInt((angle / 360f) * DirectionCount) % DirectionCount;
            return index;
        }
    }
}