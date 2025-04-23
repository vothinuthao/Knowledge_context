using UnityEngine;

namespace SteeringBehavior
{
    public class FleeBehavior : SteeringBehaviorBase
    {
        private float _panicDistance;
    
        public FleeBehavior(float weight, float panicDistance) : base(weight, "Flee")
        {
            this._panicDistance = panicDistance;
        }
    
        public override Vector3 Execute(SteeringContext context)
        {
            if (context.TroopModel == null) return Vector3.zero;
        
            // Chỉ bỏ chạy khi trong khoảng cách nguy hiểm
            float distanceToTarget = Vector3.Distance(context.TroopModel.Position, context.AvoidPosition);
            if (distanceToTarget > _panicDistance)
            {
                return Vector3.zero;
            }
        
            // Tính toán vận tốc mong muốn (ngược hướng với đối tượng cần tránh)
            Vector3 desiredVelocity = (context.TroopModel.Position - context.AvoidPosition).normalized * context.TroopModel.MoveSpeed;
        
            // Tăng cường độ khi khoảng cách rất gần
            float intensityFactor = 1.0f - (distanceToTarget / _panicDistance);
            desiredVelocity *= (1.0f + intensityFactor);
        
            // Tính và trả về lực lái
            return CalculateSteeringForce(desiredVelocity, context);
        }
    }
}