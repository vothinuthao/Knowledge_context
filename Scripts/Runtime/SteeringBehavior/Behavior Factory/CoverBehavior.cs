using Troop;
using UnityEngine;

namespace SteeringBehavior
{
    public class CoverBehavior : SteeringBehaviorBase
    {
        private float _coverDistance;
        private float _positioningSpeed;

        public CoverBehavior(float weight, float coverDistance, float positioningSpeed) 
            : base(weight, "Cover", 1)
        {
            _coverDistance = coverDistance;
            _positioningSpeed = positioningSpeed;
        }

        public override Vector3 Execute(SteeringContext context)
        {
            if (context.TroopModel == null || context.NearbyAllies == null || context.NearbyEnemies == null) 
                return Vector3.zero;

            // Nếu không có đồng minh hoặc kẻ địch, không cần cover
            if (context.NearbyAllies.Length == 0 || context.NearbyEnemies.Length == 0)
                return Vector3.zero;

            // Tìm đồng minh có Protect behavior
            TroopController protector = FindProtector(context);
            if (protector == null) return Vector3.zero;

            // Tìm kẻ địch gần nhất
            TroopController nearestEnemy = FindNearestEnemy(context);
            if (nearestEnemy == null) return Vector3.zero;

            // Tính toán vị trí cover phía sau protector
            Vector3 protectorPosition = protector.GetPosition();
            Vector3 enemyPosition = nearestEnemy.GetPosition();

            // Tính hướng từ kẻ địch đến protector
            Vector3 direction = (protectorPosition - enemyPosition).normalized;
            
            // Vị trí cover là phía sau protector theo hướng ngược với kẻ địch
            Vector3 idealPosition = protectorPosition + direction * _coverDistance;

            // Tính toán lực di chuyển đến vị trí lý tưởng
            Vector3 desiredVelocity = (idealPosition - context.TroopModel.Position).normalized * _positioningSpeed;

            // Tính và trả về lực lái
            return CalculateSteeringForce(desiredVelocity, context);
        }

        private TroopController FindProtector(SteeringContext context)
        {
            // Tìm troop có Protect behavior
            foreach (var ally in context.NearbyAllies)
            {
                if (ally == null) continue;

                // Kiểm tra nếu ally có Protect behavior
                // Cách đơn giản là kiểm tra tên behavior, trong triển khai thực tế
                // bạn có thể sử dụng cách khác để xác định
                if (ally.IsBehaviorEnabled("Protect"))
                {
                    return ally;
                }
            }

            // Nếu không tìm thấy, trả về ally gần nhất
            TroopController nearestAlly = null;
            float closestDistance = float.MaxValue;

            foreach (var ally in context.NearbyAllies)
            {
                if (ally == null) continue;

                float distance = Vector3.Distance(context.TroopModel.Position, ally.GetPosition());
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestAlly = ally;
                }
            }

            return nearestAlly;
        }

        private TroopController FindNearestEnemy(SteeringContext context)
        {
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

            return nearestEnemy;
        }
    }
}