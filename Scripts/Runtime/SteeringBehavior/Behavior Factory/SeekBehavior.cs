using UnityEngine;

namespace SteeringBehavior
{
    public class SeekBehavior : SteeringBehaviorBase
    {
        public SeekBehavior(float weight) : base(weight, "Seek")
        {
        }
        public override Vector3 Execute(SteeringContext context)
        {
            if (context.TroopModel == null) return Vector3.zero;
            Vector3 desiredVelocity = (context.TargetPosition - context.TroopModel.Position).normalized * context.TroopModel.MoveSpeed;
            return CalculateSteeringForce(desiredVelocity, context);
        }
    }
}