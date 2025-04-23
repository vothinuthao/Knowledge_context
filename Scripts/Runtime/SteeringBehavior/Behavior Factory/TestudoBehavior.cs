using System.Collections.Generic;
using System.Linq;
using Troop;
using UnityEngine;

namespace SteeringBehavior
{
    public class TestudoBehavior : SteeringBehaviorBase
    {
        private float _formationSpacing;
        private float _movementSpeedMultiplier;
        private float _knockbackResistanceBonus;
        private float _rangedDefenseBonus;

        public TestudoBehavior(float weight, float formationSpacing, float movementSpeedMultiplier, 
            float knockbackResistanceBonus, float rangedDefenseBonus) 
            : base(weight, "Testudo", 3)
        {
            _formationSpacing = formationSpacing;
            _movementSpeedMultiplier = movementSpeedMultiplier;
            _knockbackResistanceBonus = knockbackResistanceBonus;
            _rangedDefenseBonus = rangedDefenseBonus;
        }

        public override Vector3 Execute(SteeringContext context)
        {
            if (context.TroopModel == null || context.NearbyAllies == null) 
                return Vector3.zero;

            // Phải có ít nhất 5 đồng minh để tạo thành testudo
            var allies = context.NearbyAllies.Where(a => a != null).ToList();
            if (allies.Count < 5) return Vector3.zero;

            // Xác định hướng di chuyển chung như trong Phalanx
            Vector3 moveDirection = DetermineMovementDirection(context, allies);

            // Tính vị trí trong đội hình testudo
            Vector3 formationPosition = CalculateTestudoPosition(context, allies, moveDirection);

            // Tính lực di chuyển đến vị trí đội hình
            Vector3 desiredVelocity = (formationPosition - context.TroopModel.Position).normalized 
                                      * context.TroopModel.MoveSpeed * _movementSpeedMultiplier;

            // Áp dụng bonus cho troops trong testudo
            ApplyTestudoBonuses(context);

            // Tính và trả về lực lái
            return CalculateSteeringForce(desiredVelocity, context);
        }

        private Vector3 DetermineMovementDirection(SteeringContext context, List<TroopController> allies)
        {
            Vector3 moveDirection = Vector3.zero;
            int movingCount = 0;

            foreach (var ally in allies)
            {
                Vector3 velocity = ally.GetModel()?.Velocity ?? Vector3.zero;
                if (velocity.magnitude > 0.1f)
                {
                    moveDirection += velocity.normalized;
                    movingCount++;
                }
            }

            if (movingCount == 0)
            {
                // Sử dụng hướng mặt trung bình
                foreach (var ally in allies)
                {
                    moveDirection += ally.transform.forward;
                }
            }

            moveDirection.Normalize();

            // Fallback to target direction
            if (moveDirection == Vector3.zero)
            {
                moveDirection = (context.TargetPosition - context.TroopModel.Position).normalized;
                
                // Final fallback
                if (moveDirection == Vector3.zero)
                {
                    moveDirection = context.GetTransformGameObject().forward;
                }
            }

            return moveDirection;
        }

        private Vector3 CalculateTestudoPosition(SteeringContext context, List<TroopController> allies, Vector3 moveDirection)
        {
            // Tìm center của squad
            Vector3 squadCenter = Vector3.zero;
            foreach (var ally in allies)
            {
                squadCenter += ally.GetPosition();
            }
            squadCenter /= allies.Count;

            // Tạo các vector trực giao với hướng di chuyển
            Vector3 right = Vector3.Cross(Vector3.up, moveDirection).normalized;
            
            // Sort troops theo khoảng cách đến center
            allies.Sort((a, b) => 
                Vector3.Distance(a.GetPosition(), squadCenter).CompareTo(
                    Vector3.Distance(b.GetPosition(), squadCenter)));

            // Tìm index của troop hiện tại
            int troopIndex = allies.FindIndex(a => a.gameObject == context.TroopModel.GameObject);
            if (troopIndex == -1) troopIndex = allies.Count - 1;

            // Tính toán vị trí trong đội hình testudo (hình vuông/hình chữ nhật)
            int totalTroops = allies.Count;
            int sideLength = Mathf.CeilToInt(Mathf.Sqrt(totalTroops));
            
            int row = troopIndex / sideLength;
            int col = troopIndex % sideLength;

            // Testudo có thể nghiêng về phía trước để tạo hình chữ nhật
            Vector3 position = squadCenter;
            
            // Đặt troops thành hình chữ nhật/vuông
            position += moveDirection * (row - sideLength/2f) * _formationSpacing;
            position += right * (col - sideLength/2f) * _formationSpacing;

            // Troops ở ngoài cùng quay mặt ra ngoài để bảo vệ
            if (row == 0 || row == sideLength-1 || col == 0 || col == sideLength-1)
            {
                // Rotate troop to face outward (trong thực tế, đây sẽ là thông tin cho TroopView)
                // Implement rotation logic if needed
            }

            return position;
        }

        private void ApplyTestudoBonuses(SteeringContext context)
        {
            GameObject troopObj = context.TroopModel.GameObject;
            if (troopObj == null) return;

            TroopController troopController = troopObj.GetComponent<TroopController>();
            if (troopController == null) return;

            var troopModel = troopController.GetModel();
            if (troopModel != null)
            {
            }
        }
    }
}