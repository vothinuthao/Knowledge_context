// Alignment behavior - di chuyển theo hướng trung bình của nhóm

using Troop;
using UnityEngine;

namespace SteeringBehavior
{
    public class AlignmentBehavior : SteeringBehaviorBase
    {
        private float alignmentRadius;
    
        public AlignmentBehavior(float weight, float alignmentRadius) : base(weight, "Alignment")
        {
            this.alignmentRadius = alignmentRadius;
        }
    
        public override Vector3 Execute(SteeringContext context)
        {
            if (context.TroopModel == null || context.NearbyAllies == null) return Vector3.zero;
        
            Vector3 averageHeading = Vector3.zero;
            int neighborCount = 0;
        
            // Tính hướng trung bình của nhóm
            foreach (var ally in context.NearbyAllies)
            {
                if (!ally) continue;
            
                TroopController allyController = ally.GetComponent<TroopController>();
                if (!allyController) continue;
            
                Vector3 toNeighbor = ally.GetPosition() - context.TroopModel.Position;
                float distance = toNeighbor.magnitude;
            
                if (distance > 0 && distance < alignmentRadius)
                {
                    // Sử dụng vận tốc của đồng minh làm hướng
                    var allyModel = ally.GetComponent<TroopView>()?.GetComponentInParent<TroopController>();
                    if (allyModel)
                    {
                        averageHeading += ally.transform.forward; // Sử dụng forward direction thay cho velocity
                        neighborCount++;
                    }
                }
            }
        
            // Không có láng giềng trong phạm vi
            if (neighborCount == 0)
                return Vector3.zero;
        
            // Tính trung bình hướng
            averageHeading /= neighborCount;
        
            // Tạo lực hướng theo nhóm
            Vector3 desiredVelocity = averageHeading.normalized * context.TroopModel.MoveSpeed;
        
            // Tính và trả về lực lái
            return CalculateSteeringForce(desiredVelocity, context);
        }
    }
}