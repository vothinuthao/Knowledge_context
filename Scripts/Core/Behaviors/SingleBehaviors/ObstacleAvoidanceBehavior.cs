using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Implementation of Obstacle Avoidance steering behavior
    /// Steers to avoid obstacles in the path
    /// </summary>
    [System.Serializable]
    public class ObstacleAvoidanceBehavior : SteeringBehaviorBase
    {
        [SerializeField] private float lookAheadDistance = 2.0f;
        [SerializeField] private float avoidanceForce = 2.0f;
        [SerializeField] private float rayCount = 5;
        
        public ObstacleAvoidanceBehavior()
        {
            behaviorName = "ObstacleAvoidance";
        }
        
        protected override Vector3 CalculateSteeringForce(SteeringContext context)
        {
            Vector3 steeringForce = Vector3.zero;
            
            // Cast multiple feeler rays
            float startAngle = -45f;
            float angleStep = 90f / (rayCount - 1);
            
            for (int i = 0; i < rayCount; i++)
            {
                // Calculate ray direction
                float angle = startAngle + i * angleStep;
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 rayDirection = rotation * context.Forward;
                
                // Cast ray to detect obstacles
                RaycastHit hit;
                Ray ray = new Ray(context.Position, rayDirection);
                Debug.DrawRay(context.Position, rayDirection * lookAheadDistance, Color.yellow);
                
                if (Physics.Raycast(ray, out hit, lookAheadDistance, context.ObstacleLayer))
                {
                    // Calculate avoidance force
                    // The closer the obstacle, the stronger the avoidance force
                    float weight = 1.0f - (hit.distance / lookAheadDistance);
                    
                    // Calculate vector perpendicular to hit normal (tangent to surface)
                    Vector3 avoidDirection = Vector3.Cross(hit.normal, Vector3.up).normalized;
                    
                    // Check if this direction points in the general desired direction
                    if (Vector3.Dot(avoidDirection, context.Forward) < 0)
                        avoidDirection = -avoidDirection;
                    
                    // Add weighted avoidance force
                    steeringForce += avoidDirection * weight * avoidanceForce;
                    
                    // Draw debug info
                    Debug.DrawRay(hit.point, hit.normal, Color.red);
                    Debug.DrawRay(hit.point, avoidDirection, Color.green);
                }
            }
            
            return steeringForce;
        }
    }
}