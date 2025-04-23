// Path Following behavior - theo đường dẫn

using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Utils;

namespace SteeringBehavior
{
    
    public class PathFollowingBehavior : SteeringBehaviorBase
    {
        private float _pathRadius;
        private float _arrivalDistance;
        public PathFollowingBehavior(float weight, float pathRadius, float arrivalDistance) 
            : base(weight, "Path Following")
        {
            this._pathRadius = pathRadius;
            this._arrivalDistance = arrivalDistance;
        }
    
        public override Vector3 Execute(SteeringContext context)
        {
            BehaviorPath behaviorPath = context.CurrentPath;
            if (behaviorPath == null || behaviorPath.Waypoints.Count == 0) return Vector3.zero;
            Vector3 currentWaypoint = behaviorPath.GetCurrentWaypoint();
        
            Vector3 toWaypoint = currentWaypoint - context.TroopModel.Position;
            float distance = toWaypoint.magnitude;
            if (distance < _arrivalDistance)
            {
                // Đến waypoint tiếp theo
                if (behaviorPath.AdvanceToNext())
                {
                    // Lấy waypoint tiếp theo
                    currentWaypoint = behaviorPath.GetCurrentWaypoint();
                    toWaypoint = currentWaypoint - context.TroopModel.Position;
                    distance = toWaypoint.magnitude;
                }
            }
        
            // Tính toán vận tốc mong muốn
            Vector3 desiredVelocity;
            if (distance < _pathRadius)
            {
                // Trong khu vực đường dẫn - scale tốc độ
                desiredVelocity = toWaypoint.normalized * (context.TroopModel.MoveSpeed * (distance / _pathRadius));
            }
            else
            {
                // Ngoài khu vực đường dẫn - tốc độ tối đa
                desiredVelocity = toWaypoint.normalized * context.TroopModel.MoveSpeed;
            }
        
            // Tính và trả về lực lái
            return CalculateSteeringForce(desiredVelocity, context);
        }
    }
}