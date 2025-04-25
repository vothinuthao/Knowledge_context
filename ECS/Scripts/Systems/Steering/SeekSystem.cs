// ECS/Scripts/Systems/Steering/SeekSystem.cs
using Core.ECS;
using Movement;
using Steering;
using UnityEngine;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes seek steering behavior (move toward target)
    /// </summary>
    public class SeekSystem : ISystem
    {
        private World _world;
        
        public int Priority => 98; // High priority
        
        // FIX: Thêm threshold để tránh xoay tròn
        private const float ARRIVAL_THRESHOLD = 0.2f; // Khoảng cách coi như đã đến
        private const float MIN_VELOCITY_THRESHOLD = 0.05f; // Vận tốc tối thiểu để di chuyển
        
        // FIX: Lưu trữ vị trí mục tiêu trước đó để tránh update liên tục
        private System.Collections.Generic.Dictionary<int, Vector3> _previousTargets = 
            new System.Collections.Generic.Dictionary<int, Vector3>();
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<SeekComponent, SteeringDataComponent, PositionComponent>())
            {
                var seekComponent = entity.GetComponent<SeekComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!seekComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                // Skip if no target position
                if (steeringData.TargetPosition == Vector3.zero)
                {
                    // FIX: Xóa target cũ nếu không còn mục tiêu
                    if (_previousTargets.ContainsKey(entity.Id))
                    {
                        _previousTargets.Remove(entity.Id);
                    }
                    continue;
                }
                
                // FIX: Kiểm tra xem target có thay đổi không
                bool targetChanged = false;
                Vector3 targetPosition = steeringData.TargetPosition;
                
                if (!_previousTargets.ContainsKey(entity.Id))
                {
                    // Lần đầu tiên có target
                    _previousTargets[entity.Id] = targetPosition;
                    targetChanged = true;
                }
                else if (Vector3.Distance(_previousTargets[entity.Id], targetPosition) > 0.1f)
                {
                    // Target đã thay đổi đáng kể
                    _previousTargets[entity.Id] = targetPosition;
                    targetChanged = true;
                }
                
                // Calculate direction to target
                Vector3 toTarget = targetPosition - positionComponent.Position;
                toTarget.y = 0; // Keep movement on the horizontal plane
                
                // Skip if already at target
                float distance = toTarget.magnitude;
                if (distance < ARRIVAL_THRESHOLD)
                {
                    // FIX: Nếu đã đến nơi, dừng hẳn
                    if (entity.HasComponent<VelocityComponent>())
                    {
                        entity.GetComponent<VelocityComponent>().Velocity = Vector3.zero;
                    }
                    continue;
                }
                
                // Calculate desired velocity
                float maxSpeed = 0f;
                if (entity.HasComponent<VelocityComponent>())
                {
                    maxSpeed = entity.GetComponent<VelocityComponent>().GetEffectiveMaxSpeed();
                }
                else
                {
                    maxSpeed = 3.0f; // Default speed
                }
                
                // FIX: Điều chỉnh tốc độ dựa trên khoảng cách
                float targetSpeed = maxSpeed;
                
                // FIX: Giảm tốc độ khi gần đến mục tiêu
                if (distance < 2.0f)
                {
                    targetSpeed = Mathf.Lerp(0.1f, maxSpeed, distance / 2.0f);
                }
                
                // FIX: Tránh tốc độ quá nhỏ
                if (targetSpeed < MIN_VELOCITY_THRESHOLD)
                {
                    targetSpeed = MIN_VELOCITY_THRESHOLD;
                }
                
                Vector3 desiredVelocity = toTarget.normalized * targetSpeed;
                
                // Calculate steering force
                Vector3 steeringForce;
                if (entity.HasComponent<VelocityComponent>())
                {
                    var velocityComponent = entity.GetComponent<VelocityComponent>();
                    
                    // FIX: Tính toán steering force khác nhau tùy thuộc vào trạng thái
                    if (targetChanged)
                    {
                        // Nếu vừa thay đổi target, set force mạnh để di chuyển ngay
                        steeringForce = desiredVelocity - velocityComponent.Velocity;
                    }
                    else
                    {
                        // FIX: Giảm lực khi đang đến gần mục tiêu để tránh overshooting
                        steeringForce = (desiredVelocity - velocityComponent.Velocity) * 
                            Mathf.Clamp01(distance / 3.0f + 0.5f);
                    }
                }
                else
                {
                    steeringForce = desiredVelocity;
                }
                
                // Apply weight
                steeringForce *= seekComponent.Weight;
                
                // FIX: Hạn chế lực quá mạnh
                float maxForce = 10.0f;
                if (steeringForce.magnitude > maxForce)
                {
                    steeringForce = steeringForce.normalized * maxForce;
                }
                
                // Add force to steering data
                steeringData.AddForce(steeringForce);
                
                // FIX: Thêm debug log để theo dõi
                if (steeringForce.magnitude > 5.0f)
                {
                    // Debug.Log($"Entity {entity.Id} seek force: {steeringForce.magnitude}, " +
                    //          $"distance to target: {distance}, " +
                    //          $"target pos: {targetPosition}, " +
                    //          $"current pos: {positionComponent.Position}");
                }
            }
        }
    }
}