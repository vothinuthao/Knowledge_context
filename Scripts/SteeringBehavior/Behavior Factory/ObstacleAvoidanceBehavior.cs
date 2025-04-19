// Obstacle Avoidance behavior - tránh chướng ngại vật
using UnityEngine;

namespace SteeringBehavior
{
    public class ObstacleAvoidanceBehavior : SteeringBehaviorBase
    {
        private float _avoidDistance;
        private float _lookAheadDistance;
    
        public ObstacleAvoidanceBehavior(float weight, float avoidDistance, float lookAheadDistance) 
            : base(weight, "Obstacle Avoidance")
        {
            this._avoidDistance = avoidDistance;
            this._lookAheadDistance = lookAheadDistance;
        }
    
        public override Vector3 Execute(SteeringContext context)
        {
            if (context.TroopModel == null || context.Obstacles == null) return Vector3.zero;
    
            // Tạo ray hướng về phía di chuyển
            Vector3 ahead = context.TroopModel.Position + context.TroopModel.Velocity.normalized * _lookAheadDistance;
    
            // Tìm chướng ngại vật gần nhất va chạm với ray
            Transform closestObstacle = null;
            float closestDistance = float.MaxValue;
    
            foreach (var obstacle in context.Obstacles)
            {
                if (obstacle == null) continue;
        
                // Lấy collider từ obstacle
                Collider obstacleCollider = obstacle.GetComponent<Collider>();
                if (obstacleCollider == null) continue;
        
                // Tạo ray từ vị trí troop đến ahead point
                Ray ray = new Ray(context.TroopModel.Position, (ahead - context.TroopModel.Position).normalized);
                RaycastHit hit;
        
                // Raycast với collider của obstacle
                if (obstacleCollider.Raycast(ray, out hit, _lookAheadDistance))
                {
                    float distanceToObstacle = hit.distance;
            
                    if (distanceToObstacle < closestDistance)
                    {
                        closestObstacle = obstacle;
                        closestDistance = distanceToObstacle;
                    }
                }
            }
    
            // Không có chướng ngại vật nào gần
            if (closestObstacle == null)
                return Vector3.zero;
    
            // Tính lực tránh
            Vector3 avoidanceForce = ahead - closestObstacle.position;
            avoidanceForce.y = 0; // Keep on same plane
            avoidanceForce = avoidanceForce.normalized * context.TroopModel.MoveSpeed;
    
            return avoidanceForce;
        }
    }
}