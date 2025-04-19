// Arrival behavior - di chuyển đến mục tiêu và giảm tốc khi đến gần
using SteeringBehavior;
using UnityEngine;

public class ArrivalBehavior : SteeringBehaviorBase
{
    private float slowingDistance;
    
    public ArrivalBehavior(float weight, float slowingDistance) : base(weight, "Arrival")
    {
        this.slowingDistance = slowingDistance;
    }
    
    public override Vector3 Execute(SteeringContext context)
    {
        if (context.TroopModel == null) return Vector3.zero;
        
        Vector3 toTarget = context.TargetPosition - context.TroopModel.Position;
        float distance = toTarget.magnitude;
        
        // Tính toán vận tốc mong muốn
        Vector3 desiredVelocity;
        if (distance < slowingDistance)
        {
            // Trong khu vực giảm tốc - scale tốc độ
            desiredVelocity = toTarget.normalized * (context.TroopModel.MoveSpeed * (distance / slowingDistance));
        }
        else
        {
            // Ngoài khu vực giảm tốc - tốc độ tối đa
            desiredVelocity = toTarget.normalized * context.TroopModel.MoveSpeed;
        }
        
        // Tính và trả về lực lái
        return CalculateSteeringForce(desiredVelocity, context);
    }
}