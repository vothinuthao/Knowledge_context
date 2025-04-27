// ECS/Scripts/Components/Steering/SteeringDataComponent.cs

using System.Collections.Generic;
using Core.ECS;
using UnityEngine;

namespace Components.Steering
{
    /// <summary>
    /// Component for steering behavior data
    /// </summary>
    public class SteeringDataComponent : IComponent
    {
        public Vector3 SteeringForce { get; set; } = Vector3.zero;
        
        // Target position for steering
        private Vector3 _targetPosition = Vector3.zero;
        
        private Vector3 _smoothedTargetPosition = Vector3.zero;
        
        public bool TargetPositionChanged { get; private set; } = false;
        public float TimeAtCurrentTarget { get; private set; } = 0f;
        
        public Vector3 TargetPosition 
        { 
            get { return _targetPosition; }
            set 
            {
                // Check if target has changed significantly
                if (Vector3.Distance(_targetPosition, value) > 0.1f)
                {
                    TargetPositionChanged = true;
                    TimeAtCurrentTarget = 0f;
                    
                    // Save old position before updating
                    PreviousTargetPosition = _targetPosition;
                }
                
                _targetPosition = value;
            }
        }
        
        // FIX: Add access to smoothed target position
        public Vector3 SmoothedTargetPosition 
        { 
            get { return _smoothedTargetPosition; }
        }
        
        // Position to avoid
        public Vector3 AvoidPosition { get; set; } = Vector3.zero;
        
        // FIX: Add tracking target position trước đó
        public Vector3 PreviousTargetPosition { get; private set; } = Vector3.zero;
        
        // Nearby entities for flocking behaviors
        public List<int> NearbyAlliesIds { get; set; } = new List<int>();
        public List<int> NearbyEnemiesIds { get; set; } = new List<int>();
        
        // Maximum steering force
        public float MaxForce { get; set; } = 10.0f;
        
        // Whether steering is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Whether entity is in danger
        public bool IsInDanger { get; set; } = false;
        
        // FIX: Add steering behavior parameters
        public float ArrivalDistance { get; set; } = 0.5f;
        public float SlowingDistance { get; set; } = 2.0f;
        public float SmoothingFactor { get; set; } = 0.2f; // Hệ số làm mịn (0-1)
        
        public SteeringDataComponent()
        {
        }
        
        /// <summary>
        /// Add accumulated steering force, respecting max force
        /// </summary>
        public void AddForce(Vector3 force)
        {
            SteeringForce += force;
            
            if (SteeringForce.magnitude > MaxForce)
            {
                SteeringForce = SteeringForce.normalized * MaxForce;
            }
        }
        
        /// <summary>
        /// Reset all forces to zero
        /// </summary>
        public void Reset()
        {
            SteeringForce = Vector3.zero;
        }
        
        /// <summary>
        /// FIX: Update tracking and smoothing target
        /// </summary>
        public void Update(float deltaTime)
        {
            // Update time at current target
            TimeAtCurrentTarget += deltaTime;
            
            // After a while, reset target changed flag
            if (TargetPositionChanged && TimeAtCurrentTarget > 0.5f)
            {
                TargetPositionChanged = false;
            }
            
            // Smooth target position to avoid sudden changes
            _smoothedTargetPosition = Vector3.Lerp(_smoothedTargetPosition, _targetPosition, SmoothingFactor);
            
            // If smoothed is close enough to actual target, set it exactly
            if (Vector3.Distance(_smoothedTargetPosition, _targetPosition) < 0.05f)
            {
                _smoothedTargetPosition = _targetPosition;
            }
        }
        
        /// <summary>
        /// FIX: Check if reached target
        /// </summary>
        public bool HasReachedTarget(Vector3 currentPosition)
        {
            float distance = Vector3.Distance(currentPosition, _targetPosition);
            return distance <= ArrivalDistance;
        }
        
        /// <summary>
        /// FIX: Check if in slowing zone
        /// </summary>
        public bool IsInSlowingZone(Vector3 currentPosition)
        {
            float distance = Vector3.Distance(currentPosition, _targetPosition);
            return distance <= SlowingDistance;
        }
        
        /// <summary>
        /// FIX: Calculate slowing factor based on distance to target
        /// </summary>
        public float GetSlowingFactor(Vector3 currentPosition)
        {
            if (!IsInSlowingZone(currentPosition))
            {
                return 1.0f; // No slowing
            }
            
            float distance = Vector3.Distance(currentPosition, _targetPosition);
            
            // Interpolate between 0.1 and 1.0 based on distance
            return Mathf.Lerp(0.1f, 1.0f, distance / SlowingDistance);
        }
    }
}