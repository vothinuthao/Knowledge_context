// Separation behavior - giữ khoảng cách với các troop khác
using Troop;
using UnityEngine;

namespace SteeringBehavior
{
    public class SeparationBehavior : SteeringBehaviorBase
    {
        private float _separationRadius;
    
        public SeparationBehavior(float weight, float separationRadius) : base(weight, "Separation")
        {
            this._separationRadius = separationRadius;
        }
    
        public override Vector3 Execute(SteeringContext context)
        {
            if (context.TroopModel == null || context.NearbyAllies == null) return Vector3.zero;
        
            Vector3 steeringForce = Vector3.zero;
            int neighborCount = 0;
        
            // Kiểm tra tất cả đồng minh gần đó
            foreach (var ally in context.NearbyAllies)
            {
                if (ally == null) continue;
                if (!ally.GetComponent<TroopController>()) continue;
            
                Vector3 toNeighbor = context.TroopModel.Position - ally.GetPosition();
                float distance = toNeighbor.magnitude;
            
                if (distance > 0 && distance < _separationRadius)
                {
                    // Scale lực theo khoảng cách (càng gần càng mạnh)
                    Vector3 repulsionForce = toNeighbor.normalized / distance;
                    steeringForce += repulsionForce;
                    neighborCount++;
                }
            }
        
            // Tính trung bình các lực
            if (neighborCount > 0)
            {
                steeringForce /= neighborCount;
                steeringForce = steeringForce.normalized * context.TroopModel.MoveSpeed;
            }
        
            return steeringForce;
        }
    }
}