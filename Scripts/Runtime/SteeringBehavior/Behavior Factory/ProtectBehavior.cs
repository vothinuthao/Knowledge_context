using System.Linq;
using Troop;
using UnityEngine;

namespace SteeringBehavior
{
    public class ProtectBehavior : SteeringBehaviorBase
    {
        private float _protectRadius;
        private float _positioningSpeed;
        private string[] _protectedTags; // Các tag cần ưu tiên bảo vệ

        public ProtectBehavior(float weight, float protectRadius, float positioningSpeed, string[] protectedTags = null) 
            : base(weight, "Protect", 2)
        {
            _protectRadius = protectRadius;
            _positioningSpeed = positioningSpeed;
            _protectedTags = protectedTags ?? new string[] { "Player" };
        }

        public override Vector3 Execute(SteeringContext context)
        {
            if (context.TroopModel == null || context.NearbyAllies == null || context.NearbyEnemies == null) 
                return Vector3.zero;

            // Nếu không có đồng minh hoặc kẻ địch, không cần bảo vệ
            if (context.NearbyAllies.Length == 0 || context.NearbyEnemies.Length == 0)
                return Vector3.zero;

            // Tìm đồng minh cần bảo vệ ưu tiên nhất
            TroopController allyToProtect = FindPriorityAlly(context);
            if (allyToProtect == null) return Vector3.zero;

            // Tìm kẻ địch gần đồng minh nhất
            TroopController nearestEnemy = FindNearestEnemyToAlly(context, allyToProtect);
            if (nearestEnemy == null) return Vector3.zero;

            // Tính toán vị trí lý tưởng để bảo vệ
            Vector3 allyPosition = allyToProtect.GetPosition();
            Vector3 enemyPosition = nearestEnemy.GetPosition();

            // Vị trí giữa đồng minh và kẻ địch, gần đồng minh hơn
            Vector3 direction = (enemyPosition - allyPosition).normalized;
            Vector3 idealPosition = allyPosition + direction * _protectRadius * 0.5f;

            // Tính toán lực di chuyển đến vị trí lý tưởng
            Vector3 desiredVelocity = (idealPosition - context.TroopModel.Position).normalized * _positioningSpeed;

            // Tính và trả về lực lái
            return CalculateSteeringForce(desiredVelocity, context);
        }

        private TroopController FindPriorityAlly(SteeringContext context)
        {
            // Ưu tiên các đồng minh có tag được chỉ định
            var priorityAllies = context.NearbyAllies
                .Where(a => a != null && _protectedTags.Contains(a.gameObject.tag))
                .ToList();

            // Nếu không có đồng minh ưu tiên, lấy đồng minh bất kỳ
            if (priorityAllies.Count == 0)
            {
                priorityAllies = context.NearbyAllies.Where(a => a != null).ToList();
            }

            if (priorityAllies.Count == 0) return null;

            // Tìm đồng minh có health thấp nhất
            TroopController weakestAlly = null;
            float lowestHealth = float.MaxValue;

            foreach (var ally in priorityAllies)
            {
                var allyModel = ally.GetModel();
                if (allyModel != null && allyModel.CurrentHealth < lowestHealth)
                {
                    lowestHealth = allyModel.CurrentHealth;
                    weakestAlly = ally;
                }
            }

            return weakestAlly ?? priorityAllies[0];
        }

        private TroopController FindNearestEnemyToAlly(SteeringContext context, TroopController ally)
        {
            if (ally == null) return null;

            TroopController nearestEnemy = null;
            float closestDistance = float.MaxValue;

            foreach (var enemy in context.NearbyEnemies)
            {
                if (enemy == null) continue;

                float distance = Vector3.Distance(ally.GetPosition(), enemy.GetPosition());
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