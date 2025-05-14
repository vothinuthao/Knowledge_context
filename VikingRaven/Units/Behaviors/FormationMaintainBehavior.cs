using UnityEngine;
using VikingRaven.Core.Behavior;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Behaviors
{
    /// <summary>
    /// Behavior responsible for maintaining formation during squad movement
    /// New behavior to improve formation integrity
    /// </summary>
    public class FormationMaintainBehavior : BaseBehavior
    {
        private Vector3 _squadCenter;
        private Quaternion _squadRotation;
        private float _baseWeight = 3.0f;
        private float _minDistance = 0.5f;
        private float _targetPositionUpdateInterval = 0.2f;
        private float _lastUpdateTime = 0f;
        
        // Formation discipline parameters
        private float _formationTolerance = 0.5f;
        private float _maxDistanceMultiplier = 2.0f;
        private float _cohesionStrength = 0.8f;
        
        // Current target position in formation
        private Vector3 _currentTargetPosition;
        private bool _hasTargetPosition = false;
        
        // Obstacle avoidance parameters
        private bool _isObstacleDetected = false;
        private float _obstacleAvoidanceTimer = 0f;
        private Vector3 _obstacleAvoidanceDirection = Vector3.zero;
        
        // Debug flag
        private bool _debugMode = false;

        public FormationMaintainBehavior(IEntity entity) : base("FormationMaintain", entity)
        {
        }

        /// <summary>
        /// Set current squad information
        /// </summary>
        public void SetSquadInfo(Vector3 center, Quaternion rotation)
        {
            _squadCenter = center;
            _squadRotation = rotation;
        }

        /// <summary>
        /// Calculate behavior weight based on formation position and state
        /// </summary>
        public override float CalculateWeight()
        {
            // Get required components
            var formationComponent = _entity.GetComponent<FormationComponent>();
            var transformComponent = _entity.GetComponent<TransformComponent>();
            var navigationComponent = _entity.GetComponent<NavigationComponent>();
            
            if (formationComponent == null || transformComponent == null || navigationComponent == null)
                return 0f;
                
            // Don't maintain formation if navigation is deactivated
            if (!navigationComponent.IsPathfindingActive)
                return 0f;
                
            // Get formation parameters
            Vector3 formationOffset = formationComponent.FormationOffset;
            
            // Calculate target position in world space
            Vector3 targetPosition = _squadCenter + (_squadRotation * formationOffset);
            
            // Store target position for later use
            if (Time.time - _lastUpdateTime > _targetPositionUpdateInterval)
            {
                _currentTargetPosition = targetPosition;
                _hasTargetPosition = true;
                _lastUpdateTime = Time.time;
            }
            
            // Calculate distance to target position
            float distanceToTarget = Vector3.Distance(transformComponent.Position, targetPosition);
            
            // Get formation discipline multiplier
            float disciplineMultiplier = formationComponent.GetFormationDisciplineMultiplier();
            
            // Weight is based on distance from formation position with discipline factor
            _weight = _baseWeight * Mathf.Clamp(distanceToTarget / _formationTolerance, 0.1f, _maxDistanceMultiplier);
            _weight *= disciplineMultiplier;
            
            // Reduce weight if unit is stuck or avoiding obstacles
            if (_isObstacleDetected)
            {
                _weight *= 0.7f;
            }
            
            // Reduce weight if unit has a high priority command
            if (navigationComponent.CurrentCommandPriority >= NavigationCommandPriority.High)
            {
                _weight *= 0.5f;
            }
            
            // Check if unit is at its target position
            if (distanceToTarget < _minDistance && navigationComponent.HasReachedDestination)
            {
                _weight = 0.1f; // Very low weight when in position
            }
            
            return _weight;
        }

        /// <summary>
        /// Execute the formation maintenance behavior
        /// </summary>
        public override void Execute()
        {
            // Get required components
            var transformComponent = _entity.GetComponent<TransformComponent>();
            var navigationComponent = _entity.GetComponent<NavigationComponent>();
            var formationComponent = _entity.GetComponent<FormationComponent>();
            
            if (transformComponent == null || navigationComponent == null || formationComponent == null)
                return;
                
            // If no target position, calculate it now
            if (!_hasTargetPosition)
            {
                Vector3 formationOffset = formationComponent.FormationOffset;
                _currentTargetPosition = _squadCenter + (_squadRotation * formationOffset);
                _hasTargetPosition = true;
            }
            
            // Check for obstacles
            DetectObstacles();
            
            // Formation behavior depends on distance to target
            float distanceToTarget = Vector3.Distance(transformComponent.Position, _currentTargetPosition);
            
            if (_isObstacleDetected)
            {
                // If obstacle detected, apply obstacle avoidance while trying to maintain formation
                HandleObstacleAvoidance(transformComponent, navigationComponent);
            }
            else if (distanceToTarget > _formationTolerance)
            {
                // If far from formation position, move towards it
                navigationComponent.SetDestination(_currentTargetPosition, NavigationCommandPriority.Normal);
                
                if (_debugMode)
                {
                    Debug.DrawLine(transformComponent.Position, _currentTargetPosition, Color.yellow);
                }
            }
            else if (distanceToTarget > _minDistance)
            {
                // If near formation position, do fine adjustment
                transformComponent.Move((_currentTargetPosition - transformComponent.Position).normalized * 
                    Time.deltaTime * _cohesionStrength);
                
                if (_debugMode)
                {
                    Debug.DrawLine(transformComponent.Position, _currentTargetPosition, Color.green);
                }
            }
            else
            {
                // At formation position, just look in the right direction
                Vector3 lookDirection = _squadRotation * Vector3.forward;
                transformComponent.LookAt(transformComponent.Position + lookDirection);
                
                if (_debugMode)
                {
                    Debug.DrawRay(transformComponent.Position, lookDirection, Color.blue);
                }
            }
        }
        
        /// <summary>
        /// Detect obstacles in the path
        /// </summary>
        private void DetectObstacles()
        {
            var transformComponent = _entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                return;
                
            // Check if target position exists
            if (!_hasTargetPosition)
                return;
                
            // Calculate direction to target
            Vector3 directionToTarget = (_currentTargetPosition - transformComponent.Position).normalized;
            
            if (directionToTarget == Vector3.zero)
                return;
                
            // Cast ray to detect obstacles
            float rayDistance = 1.0f;
            Ray ray = new Ray(transformComponent.Position + Vector3.up * 0.5f, directionToTarget);
            
            _isObstacleDetected = Physics.SphereCast(
                ray, 0.3f, out RaycastHit hit, 
                rayDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                
            if (_isObstacleDetected)
            {
                // Store obstacle information
                _obstacleAvoidanceTimer = 0.5f;
                
                // Calculate avoidance direction - perpendicular to obstacle normal
                Vector3 normal = hit.normal;
                normal.y = 0; // Keep movement on horizontal plane
                
                // Cross product to get perpendicular vector
                _obstacleAvoidanceDirection = Vector3.Cross(normal, Vector3.up).normalized;
                
                // Randomly flip direction
                if (Random.value > 0.5f)
                {
                    _obstacleAvoidanceDirection = -_obstacleAvoidanceDirection;
                }
                
                if (_debugMode)
                {
                    Debug.DrawRay(hit.point, normal, Color.red);
                    Debug.DrawRay(hit.point, _obstacleAvoidanceDirection, Color.cyan);
                }
            }
            else
            {
                // Decrease timer
                _obstacleAvoidanceTimer -= Time.deltaTime;
                if (_obstacleAvoidanceTimer <= 0)
                {
                    _obstacleAvoidanceTimer = 0;
                    _obstacleAvoidanceDirection = Vector3.zero;
                }
            }
        }
        
        /// <summary>
        /// Handle obstacle avoidance while maintaining formation
        /// </summary>
        private void HandleObstacleAvoidance(TransformComponent transformComponent, NavigationComponent navigationComponent)
        {
            if (_obstacleAvoidanceDirection == Vector3.zero)
                return;
                
            // Calculate a point to move around the obstacle
            Vector3 avoidanceTarget = transformComponent.Position + _obstacleAvoidanceDirection * 2.0f;
            
            // Blend avoidance direction with target direction
            Vector3 targetDirection = (_currentTargetPosition - transformComponent.Position).normalized;
            Vector3 blendedDirection = Vector3.Lerp(_obstacleAvoidanceDirection, targetDirection, 0.3f).normalized;
            
            // Set new destination to avoid obstacle
            Vector3 newTarget = transformComponent.Position + blendedDirection * 2.0f;
            navigationComponent.SetDestination(newTarget, NavigationCommandPriority.High);
            
            if (_debugMode)
            {
                Debug.DrawLine(transformComponent.Position, newTarget, Color.red);
            }
        }
    }
}