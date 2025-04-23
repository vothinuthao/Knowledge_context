using UnityEngine;

namespace SteeringBehavior
{
    public class AmbushMoveBehavior : SteeringBehaviorBase
    {
        private float _moveSpeedMultiplier;
        private float _detectionRadiusMultiplier;

        public AmbushMoveBehavior(float weight, float moveSpeedMultiplier, float detectionRadiusMultiplier)
            : base(weight, "Ambush Move", 1)
        {
            _moveSpeedMultiplier = moveSpeedMultiplier;
            _detectionRadiusMultiplier = detectionRadiusMultiplier;
        }

        public override Vector3 Execute(SteeringContext context)
        {
            if (context.TroopModel == null) return Vector3.zero;

            // Nếu đã bị attack thì không còn ambush nữa
            if (context.IsInDanger)
            {
                return Vector3.zero;
            }

            // Tính toán vận tốc mong muốn - chậm hơn
            Vector3 desiredVelocity = (context.TargetPosition - context.TroopModel.Position).normalized
                                      * (context.TroopModel.MoveSpeed * _moveSpeedMultiplier);

            // Tính và trả về lực lái
            return CalculateSteeringForce(desiredVelocity, context);
        }
    }
}