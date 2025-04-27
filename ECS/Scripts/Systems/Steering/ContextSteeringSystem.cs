// ECS/Scripts/Systems/Steering/ContextSteeringSystem.cs
using Core.ECS;
using Movement;
using Steering;
using UnityEngine;
using System.Collections.Generic;
using Components;
using Components.Steering;

namespace Systems.Steering
{
    /// <summary>
    /// Hệ thống Context Steering - tiếp cận hiện đại hơn cho việc tính toán steering
    /// </summary>
    public class ContextSteeringSystem : ISystem
    {
        private World _world;
        
        // Số lượng phương hướng kiểm tra
        private const int DIRECTION_RESOLUTION = 16;
        
        // Thông số
        private const float INTEREST_WEIGHT = 1.0f;
        private const float DANGER_WEIGHT = 2.0f; // Danger quan trọng hơn interest
        
        // Mảng lưu trữ các hướng kiểm tra
        private Vector3[] _directions;
        
        public int Priority => 115; // Ưu tiên cao, thay thế steering system thông thường
        
        public void Initialize(World world)
        {
            _world = world;
            
            // Khởi tạo mảng các hướng kiểm tra
            GenerateDirections();
        }
        
        /// <summary>
        /// Khởi tạo mảng các hướng được kiểm tra
        /// </summary>
        private void GenerateDirections()
        {
            _directions = new Vector3[DIRECTION_RESOLUTION];
            
            // Tạo các hướng xung quanh theo góc
            float angleStep = 360.0f / DIRECTION_RESOLUTION;
            
            for (int i = 0; i < DIRECTION_RESOLUTION; i++)
            {
                float angle = i * angleStep;
                float rad = angle * Mathf.Deg2Rad;
                
                // Hướng trên mặt phẳng XZ (ngang)
                _directions[i] = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)).normalized;
            }
        }
        
        public void Update(float deltaTime)
        {
            // Xử lý cho mỗi entity có SteeringDataComponent và Context Steering
            foreach (var entity in _world.GetEntitiesWith<ContextSteeringComponent, SteeringDataComponent, PositionComponent>())
            {
                var contextComponent = entity.GetComponent<ContextSteeringComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!contextComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                // Tính toán steering mới bằng context steering
                Vector3 steeringForce = CalculateContextSteering(entity, contextComponent, steeringData, positionComponent);
                
                // Áp dụng lực
                steeringData.AddForce(steeringForce);
            }
        }
        
        /// <summary>
        /// Tính toán steering force dựa trên context (hướng quan tâm và nguy hiểm)
        /// </summary>
        private Vector3 CalculateContextSteering(
            Entity entity, 
            ContextSteeringComponent contextComponent, 
            SteeringDataComponent steeringData, 
            PositionComponent positionComponent)
        {
            // Mảng lưu mức độ quan tâm và nguy hiểm cho mỗi hướng
            float[] interest = new float[DIRECTION_RESOLUTION]; // Mức độ quan tâm
            float[] danger = new float[DIRECTION_RESOLUTION];   // Mức độ nguy hiểm
            
            // --- Xử lý mức độ quan tâm (di chuyển đến mục tiêu) ---
            
            // Nếu có target position, tăng interest cho các hướng gần với target
            if (steeringData.TargetPosition != Vector3.zero)
            {
                // Tính vector hướng từ entity đến target
                Vector3 toTarget = steeringData.TargetPosition - positionComponent.Position;
                toTarget.y = 0; // Giữ trên mặt phẳng ngang
                
                if (toTarget.magnitude > 0.1f)
                {
                    Vector3 directionToTarget = toTarget.normalized;
                    
                    // Tính interest cho mỗi hướng
                    for (int i = 0; i < DIRECTION_RESOLUTION; i++)
                    {
                        // Độ tương đồng giữa hướng kiểm tra và hướng đến target (dot product)
                        float dot = Vector3.Dot(_directions[i], directionToTarget);
                        
                        // Chuyển từ [-1, 1] sang [0, 1]
                        float scaledDot = (dot + 1) * 0.5f;
                        
                        // Tăng interest cho các hướng gần với target
                        interest[i] += scaledDot * INTEREST_WEIGHT;
                    }
                }
            }
            
            // --- Xử lý mức độ nguy hiểm (tránh chướng ngại vật và va chạm) ---
            
            // Tránh các entity khác
            foreach (var otherEntity in _world.GetEntitiesWith<PositionComponent>())
            {
                // Skip self
                if (otherEntity.Id == entity.Id)
                {
                    continue;
                }
                
                // Lấy vị trí entity khác
                Vector3 otherPosition = otherEntity.GetComponent<PositionComponent>().Position;
                
                // Tính vector từ entity đến other
                Vector3 toOther = otherPosition - positionComponent.Position;
                toOther.y = 0; // Giữ trên mặt phẳng ngang
                
                float distance = toOther.magnitude;
                
                // Chỉ quan tâm đến các entity trong phạm vi ảnh hưởng
                if (distance < contextComponent.AvoidRadius)
                {
                    Vector3 directionToOther = toOther / distance;
                    
                    // Tính danger cho mỗi hướng
                    for (int i = 0; i < DIRECTION_RESOLUTION; i++)
                    {
                        // Độ tương đồng giữa hướng kiểm tra và hướng đến other
                        float dot = Vector3.Dot(_directions[i], directionToOther);
                        
                        // Chỉ quan tâm đến hướng gần với other (dot > 0)
                        if (dot > 0)
                        {
                            // Tính mức độ nguy hiểm dựa trên khoảng cách và độ tương đồng
                            float dangerAmount = dot * (1.0f - distance / contextComponent.AvoidRadius);
                            
                            // Mức độ nguy hiểm càng lớn khi càng gần
                            danger[i] += dangerAmount * DANGER_WEIGHT;
                        }
                    }
                }
            }
            
            // --- Kết hợp interest và danger để tìm hướng tốt nhất ---
            
            // Tính mức độ thích hợp của mỗi hướng (interest - danger)
            float[] desirability = new float[DIRECTION_RESOLUTION];
            for (int i = 0; i < DIRECTION_RESOLUTION; i++)
            {
                desirability[i] = interest[i] * (1.0f - Mathf.Clamp01(danger[i]));
            }
            
            // Tìm hướng có mức độ thích hợp cao nhất
            int bestIndex = 0;
            float bestDesirability = desirability[0];
            
            for (int i = 1; i < DIRECTION_RESOLUTION; i++)
            {
                if (desirability[i] > bestDesirability)
                {
                    bestDesirability = desirability[i];
                    bestIndex = i;
                }
            }
            
            // Nếu không có hướng nào thích hợp, giữ nguyên hướng hiện tại
            if (bestDesirability < 0.01f)
            {
                return Vector3.zero;
            }
            
            // Tính lực steering để đi theo hướng tốt nhất
            Vector3 desiredDirection = _directions[bestIndex];
            
            // Tính vận tốc mong muốn
            float speed = 0;
            if (entity.HasComponent<VelocityComponent>())
            {
                speed = entity.GetComponent<VelocityComponent>().GetEffectiveMaxSpeed();
            }
            else
            {
                speed = 3.0f; // Default speed
            }
            
            Vector3 desiredVelocity = desiredDirection * speed;
            
            // Tính lực steering
            Vector3 steeringForce;
            if (entity.HasComponent<VelocityComponent>())
            {
                steeringForce = desiredVelocity - entity.GetComponent<VelocityComponent>().Velocity;
            }
            else
            {
                steeringForce = desiredVelocity;
            }
            
            // Áp dụng trọng số
            steeringForce *= contextComponent.Weight;
            
            return steeringForce;
        }
    }
    
    /// <summary>
    /// Component cho context steering
    /// </summary>
    public class ContextSteeringComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 1.0f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Bán kính tránh các entity khác
        public float AvoidRadius { get; set; } = 3.0f;
        
        public ContextSteeringComponent(float weight = 1.0f, float avoidRadius = 3.0f)
        {
            Weight = weight;
            AvoidRadius = avoidRadius;
        }
    }
}