using UnityEngine;

namespace Core.Performance
{
    /// <summary>
    /// Culls behaviors outside of camera view
    /// </summary>
    public class BehaviorCulling
    {
        private Camera _mainCamera;
        private float _cullDistance = 50.0f;
        private float _cullMargin = 5.0f;
        
        public BehaviorCulling(Camera mainCamera, float cullDistance = 50.0f)
        {
            _mainCamera = mainCamera;
            _cullDistance = cullDistance;
        }
        
        public bool ShouldCull(Vector3 position)
        {
            // Distance culling
            float distanceToCamera = Vector3.Distance(_mainCamera.transform.position, position);
            if (distanceToCamera > _cullDistance)
            {
                return true;
            }
            
            // Frustum culling with margin
            Vector3 viewportPoint = _mainCamera.WorldToViewportPoint(position);
            
            return viewportPoint.x < -_cullMargin || viewportPoint.x > 1.0f + _cullMargin ||
                   viewportPoint.y < -_cullMargin || viewportPoint.y > 1.0f + _cullMargin ||
                   viewportPoint.z < 0;
        }
    }
}