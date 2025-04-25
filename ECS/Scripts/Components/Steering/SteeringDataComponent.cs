// ECS/Scripts/Components/Steering/SteeringDataComponent.cs
using System.Collections.Generic;
using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for steering behavior data
    /// </summary>
    public class SteeringDataComponent : IComponent
    {
        // Final steering force calculated from all behaviors
        public Vector3 SteeringForce { get; set; } = Vector3.zero;
        
        // Target position for steering
        private Vector3 _targetPosition = Vector3.zero;
        
        // FIX: Thêm target smooth để tránh thay đổi đột ngột
        private Vector3 _smoothedTargetPosition = Vector3.zero;
        
        // FIX: Thêm tracking thay đổi target
        public bool TargetPositionChanged { get; private set; } = false;
        public float TimeAtCurrentTarget { get; private set; } = 0f;
        
        // FIX: Property với logic bổ sung khi set target position
        public Vector3 TargetPosition 
        { 
            get { return _targetPosition; }
            set 
            {
                // Kiểm tra xem target có thay đổi đáng kể không
                if (Vector3.Distance(_targetPosition, value) > 0.1f)
                {
                    TargetPositionChanged = true;
                    TimeAtCurrentTarget = 0f;
                    
                    // Lưu vị trí cũ trước khi cập nhật
                    PreviousTargetPosition = _targetPosition;
                }
                
                _targetPosition = value;
            }
        }
        
        // FIX: Thêm truy cập đến target position đã được làm mịn
        public Vector3 SmoothedTargetPosition 
        { 
            get { return _smoothedTargetPosition; }
        }
        
        // Position to avoid
        public Vector3 AvoidPosition { get; set; } = Vector3.zero;
        
        // FIX: Thêm tracking target position trước đó
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
        
        // FIX: Thêm các tham số steering behavior
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
        /// FIX: Cập nhật logic tracking và làm mịn target
        /// </summary>
        public void Update(float deltaTime)
        {
            // Cập nhật thời gian tại target hiện tại
            TimeAtCurrentTarget += deltaTime;
            
            // Sau một thời gian, reset flag thay đổi target
            if (TargetPositionChanged && TimeAtCurrentTarget > 0.5f)
            {
                TargetPositionChanged = false;
            }
            
            // Làm mịn target position để tránh thay đổi đột ngột
            _smoothedTargetPosition = Vector3.Lerp(_smoothedTargetPosition, _targetPosition, SmoothingFactor);
            
            // Nếu smoothed gần với target thực tế, set luôn bằng target để tránh sai số nhỏ
            if (Vector3.Distance(_smoothedTargetPosition, _targetPosition) < 0.05f)
            {
                _smoothedTargetPosition = _targetPosition;
            }
        }
        
        /// <summary>
        /// FIX: Kiểm tra xem đã đến đủ gần target chưa
        /// </summary>
        public bool HasReachedTarget(Vector3 currentPosition)
        {
            float distance = Vector3.Distance(currentPosition, _targetPosition);
            return distance <= ArrivalDistance;
        }
        
        /// <summary>
        /// FIX: Kiểm tra xem có đang trong vùng giảm tốc không
        /// </summary>
        public bool IsInSlowingZone(Vector3 currentPosition)
        {
            float distance = Vector3.Distance(currentPosition, _targetPosition);
            return distance <= SlowingDistance;
        }
        
        /// <summary>
        /// FIX: Tính hệ số giảm tốc dựa trên khoảng cách đến target
        /// </summary>
        public float GetSlowingFactor(Vector3 currentPosition)
        {
            if (!IsInSlowingZone(currentPosition))
            {
                return 1.0f; // Không giảm tốc
            }
            
            float distance = Vector3.Distance(currentPosition, _targetPosition);
            
            // Interpolate giữa 0.1 và 1.0 dựa trên khoảng cách
            return Mathf.Lerp(0.1f, 1.0f, distance / SlowingDistance);
        }
    }
}