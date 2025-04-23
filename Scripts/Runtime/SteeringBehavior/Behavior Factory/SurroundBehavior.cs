using Troop;
using UnityEngine;

namespace SteeringBehavior
{
    public class SurroundBehavior : SteeringBehaviorBase
    {
        private float _surroundRadius;
        private float _surroundSpeed;
        
        public SurroundBehavior(float weight, float surroundRadius, float surroundSpeed) 
            : base(weight, "Surround", 1)
        {
            _surroundRadius = surroundRadius;
            _surroundSpeed = surroundSpeed;
        }
        
        public override Vector3 Execute(SteeringContext context)
        {
            if (context.TroopModel == null || context.NearbyEnemies == null || context.NearbyEnemies.Length == 0) return Vector3.zero;
            
            // Tìm kẻ địch gần nhất
            TroopController nearestEnemy = null;
            float closestDistance = float.MaxValue;
            
            foreach (var enemy in context.NearbyEnemies)
            {
                if (enemy == null) continue;
                
                float distance = Vector3.Distance(context.TroopModel.Position, enemy.GetPosition());
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
            
            if (nearestEnemy == null) return Vector3.zero;
            
            Vector3 enemyPos = nearestEnemy.GetPosition();
            
            // Tính vị trí bao vây
            // Xác định các vị trí xung quanh kẻ địch
            Vector3 toEnemy = context.TroopModel.Position - enemyPos;
            toEnemy.y = 0;
            
            // Lấy khoảng cách đến kẻ địch
            float distanceToEnemy = toEnemy.magnitude;
            
            // Nếu chúng ta quá gần, di chuyển ra xa một chút
            if (distanceToEnemy < _surroundRadius * 0.8f)
            {
                Vector3 awayFromEnemy = toEnemy.normalized * _surroundRadius;
                Vector3 targetPos = enemyPos + awayFromEnemy;
                
                Vector3 desiredVelocity = (targetPos - context.TroopModel.Position).normalized * _surroundSpeed;
                return CalculateSteeringForce(desiredVelocity, context);
            }
            
            // Nếu chúng ta quá xa, di chuyển lại gần
            if (distanceToEnemy > _surroundRadius * 1.2f)
            {
                Vector3 towardsEnemy = -toEnemy.normalized * _surroundRadius;
                Vector3 targetPos = enemyPos + towardsEnemy;
                
                Vector3 desiredVelocity = (targetPos - context.TroopModel.Position).normalized * _surroundSpeed;
                return CalculateSteeringForce(desiredVelocity, context);
            }
            
            // Nếu ở khoảng cách đúng, di chuyển theo đường tròn quanh kẻ địch
            Vector3 circlePos = toEnemy.normalized * _surroundRadius;
            Vector3 tangent = new Vector3(-circlePos.z, 0, circlePos.x).normalized;
            
            Vector3 targetPosition = enemyPos + circlePos + tangent * 2f;
            Vector3 desiredVel = (targetPosition - context.TroopModel.Position).normalized * _surroundSpeed;
            
            return CalculateSteeringForce(desiredVel, context);
        }
    }
}