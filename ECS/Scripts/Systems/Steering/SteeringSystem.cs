// ECS/Scripts/Systems/Steering/SteeringSystem.cs

using Components;
using Core.ECS;
using Movement;
using Steering;
using UnityEngine;

namespace Systems.Steering
{
    /// <summary>
    /// System that calculates and applies steering behaviors
    /// </summary>
    public class SteeringSystem : ISystem
    {
        private World _world;
        
        public int Priority => 110;
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            // FIX: Trước tiên, cập nhật các SteeringDataComponent
            UpdateSteeringData(deltaTime);
            
            // Reset all steering forces
            foreach (var entity in _world.GetEntitiesWith<SteeringDataComponent>())
            {
                entity.GetComponent<SteeringDataComponent>().Reset();
            }
            
            // Process seek behavior - đã được cập nhật trong SeekSystemFix
            // Process separation behavior - không cần thay đổi
            // Process other behaviors...
            
            // Apply calculated steering forces to acceleration
            ApplySteeringForces();
        }
        
        /// <summary>
        /// FIX: Cập nhật dữ liệu steering cho mỗi entity
        /// </summary>
        private void UpdateSteeringData(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<SteeringDataComponent>())
            {
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                
                // Update smoothing và tracking trong SteeringDataComponent
                steeringData.Update(deltaTime);
            }
        }
        
        /// <summary>
        /// Apply calculated steering forces to acceleration
        /// </summary>
        private void ApplySteeringForces()
        {
            foreach (var entity in _world.GetEntitiesWith<SteeringDataComponent, AccelerationComponent>())
            {
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var accelerationComponent = entity.GetComponent<AccelerationComponent>();
                
                // Skip if steering is disabled
                if (!steeringData.IsEnabled)
                {
                    continue;
                }
                
                // FIX: Kiểm tra xem entity đã đến đích chưa
                if (entity.HasComponent<PositionComponent>())
                {
                    var positionComponent = entity.GetComponent<PositionComponent>();
                    if (steeringData.HasReachedTarget(positionComponent.Position))
                    {
                        // Đã đến đích, dừng lại
                        if (entity.HasComponent<VelocityComponent>())
                        {
                            var velocityComponent = entity.GetComponent<VelocityComponent>();
                            
                            // Giảm dần vận tốc xuống 0 nếu đã đến đích
                            if (velocityComponent.Velocity.magnitude > 0.01f)
                            {
                                accelerationComponent.Acceleration = -velocityComponent.Velocity * 5.0f;
                            }
                            else
                            {
                                velocityComponent.Velocity = Vector3.zero;
                                accelerationComponent.Acceleration = Vector3.zero;
                            }
                        }
                        
                        continue;
                    }
                    
                    // FIX: Nếu đang trong vùng giảm tốc, điều chỉnh lực
                    if (steeringData.IsInSlowingZone(positionComponent.Position))
                    {
                        float slowingFactor = steeringData.GetSlowingFactor(positionComponent.Position);
                        
                        // Apply lực theo tỷ lệ giảm tốc
                        accelerationComponent.Acceleration += steeringData.SteeringForce * slowingFactor;
                    }
                    else
                    {
                        // Apply lực thông thường
                        accelerationComponent.Acceleration += steeringData.SteeringForce;
                    }
                }
                else
                {
                    // Không có PositionComponent, apply lực thông thường
                    accelerationComponent.Acceleration += steeringData.SteeringForce;
                }
                
                // Limit acceleration
                accelerationComponent.LimitAcceleration();
            }
        }
    }
}