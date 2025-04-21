using UnityEngine;

// Cohesion behavior - di chuyển về phía trung tâm của nhóm
namespace SteeringBehavior
{
    public class CohesionBehavior : SteeringBehaviorBase
    {
        private float _cohesionRadius;
    
        public CohesionBehavior(float weight, float cohesionRadius) : base(weight, "Cohesion")
        {
            this._cohesionRadius = cohesionRadius;
        }
    
        public override Vector3 Execute(SteeringContext context)
        {
            if (context.TroopModel == null || context.NearbyAllies == null) return Vector3.zero;
        
            Vector3 centerOfMass = Vector3.zero;
            int neighborCount = 0;
        
            // Tính điểm trung tâm của nhóm
            foreach (var ally in context.NearbyAllies)
            {
                if (ally == null) continue;
            
                Vector3 toNeighbor = ally.GetPosition() - context.TroopModel.Position;
                float distance = toNeighbor.magnitude;
            
                if (distance > 0 && distance < _cohesionRadius)
                {
                    centerOfMass += ally.GetPosition();
                    neighborCount++;
                }
            }
        
            // Không có khứa nào trong phạm vi
            if (neighborCount == 0)
                return Vector3.zero;
            centerOfMass /= neighborCount;
            
            // Tính vector lực hướng về trung tâm
            Vector3 desiredVelocity = (centerOfMass - context.TroopModel.Position).normalized * context.TroopModel.MoveSpeed;
        
            // Tính và trả về lực lái
            return CalculateSteeringForce(desiredVelocity, context);
        }
    }
}