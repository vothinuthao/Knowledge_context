using System.Collections.Generic;
using Core.Patterns.StrategyPattern;
using UnityEngine;

namespace SteeringBehavior
{
    public class CompositeSteeringBehavior : CompositeStrategy<SteeringContext, Vector3>
    {
        public CompositeSteeringBehavior() : base()
        {
        }
    
        // Thực thi với logic kết hợp cụ thể
        public Vector3 Execute(SteeringContext context)
        {
            return Execute(context, CombineForces);
        }
    
        // Phương thức kết hợp các lực
        private Vector3 CombineForces(List<(Vector3, float)> weightedForces)
        {
            Vector3 totalForce = Vector3.zero;
        
            // Kết hợp các lực với trọng số tương ứng
            foreach (var (force, weight) in weightedForces)
            {
                totalForce += force * weight;
            }
        
            // Giới hạn lực tối đa nếu cần
            float maxForce = 10f;
            if (totalForce.magnitude > maxForce)
            {
                totalForce = totalForce.normalized * maxForce;
            }
        
            return totalForce;
        }
    }
}