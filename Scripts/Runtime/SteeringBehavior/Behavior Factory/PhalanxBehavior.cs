using System.Collections.Generic;
using System.Linq;
using Troop;
using UnityEngine;

namespace SteeringBehavior
{
    public class PhalanxBehavior : SteeringBehaviorBase
    {
        private float _formationSpacing;
        private float _movementSpeedMultiplier;
        private int _maxRowsInFormation;

        public PhalanxBehavior(float weight, float formationSpacing, float movementSpeedMultiplier, int maxRowsInFormation) 
            : base(weight, "Phalanx", 2)
        {
            _formationSpacing = formationSpacing;
            _movementSpeedMultiplier = movementSpeedMultiplier;
            _maxRowsInFormation = maxRowsInFormation;
        }

        public override Vector3 Execute(SteeringContext context)
        {
            if (context.TroopModel == null || context.NearbyAllies == null) 
                return Vector3.zero;

            // Phải có ít nhất 3 đồng minh để tạo thành phalanx
            var allies = context.NearbyAllies.Where(a => a != null).ToList();
            if (allies.Count < 3) return Vector3.zero;

            // Xác định hướng di chuyển chung
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

            // Nếu không ai di chuyển, sử dụng hướng mặt hiện tại của squad
            if (movingCount == 0)
            {
                // Sử dụng hướng mặt trung bình của squad
                foreach (var ally in allies)
                {
                    moveDirection += ally.transform.forward;
                }
            }

            moveDirection.Normalize();

            // Nếu vẫn không xác định được hướng, sử dụng hướng về target
            if (moveDirection == Vector3.zero)
            {
                moveDirection = (context.TargetPosition - context.TroopModel.Position).normalized;
                
                // Nếu vẫn không có, sử dụng hướng mặt của troop
                if (moveDirection == Vector3.zero)
                {
                    moveDirection = context.GetTransformGameObject().forward;
                }
            }

            // Tính vị trí của troop trong đội hình phalanx
            Vector3 formationPosition = CalculateFormationPosition(context, allies, moveDirection);

            // Tính lực di chuyển đến vị trí đội hình
            Vector3 desiredVelocity = (formationPosition - context.TroopModel.Position).normalized 
                                      * context.TroopModel.MoveSpeed * _movementSpeedMultiplier;

            // Kết hợp với di chuyển đến target
            if (context.TargetPosition != Vector3.zero)
            {
                Vector3 toTarget = (context.TargetPosition - context.TroopModel.Position).normalized 
                                   * context.TroopModel.MoveSpeed * _movementSpeedMultiplier * 0.5f;
                
                desiredVelocity = Vector3.Lerp(desiredVelocity, toTarget, 0.3f);
            }

            // Tính và trả về lực lái
            return CalculateSteeringForce(desiredVelocity, context);
        }

        private Vector3 CalculateFormationPosition(SteeringContext context, List<TroopController> allies, Vector3 moveDirection)
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

            // Index của troop hiện tại trong danh sách đã sort
            int troopIndex = allies.FindIndex(a => a.gameObject == context.TroopModel.GameObject);
            
            // Nếu không tìm thấy, đặt ở cuối
            if (troopIndex == -1) troopIndex = allies.Count - 1;

            // Tính vị trí trong đội hình
            int row = Mathf.Min(troopIndex / 2, _maxRowsInFormation - 1);
            int col = troopIndex % 2;
            int offset = (row == 0) ? 0 : 1; // Vị trí đầu tiên không lệch

            // Tạo đội hình hình tam giác/mũi nhọn
            Vector3 position = squadCenter + moveDirection * row * _formationSpacing;
            
            // Offset sang phải hoặc trái tùy vào column
            if (col == 0)
                position += right * ((row * offset) * _formationSpacing * 0.5f);
            else
                position -= right * ((row * offset) * _formationSpacing * 0.5f);

            return position;
        }
    }
}