using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    /// <summary>
    /// Enhanced Animation System with intelligent state management and performance optimization
    /// </summary>
    public class AnimationSystem : BaseSystem
    {
        #region System Configuration
        
        [Title("Animation System Settings")]
        [Tooltip("Enable automatic movement-based animation switching")]
        [SerializeField] private bool _enableMovementAnimations = true;
        
        [Tooltip("Movement speed threshold for walk/run transition")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _runSpeedThreshold = 3f;
        
        [Tooltip("Minimum movement distance to trigger moving animation")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _movementThreshold = 0.1f;
        
        [Tooltip("Time to wait before switching to idle after stopping")]
        [Range(0f, 2f)]
        [SerializeField] private float _idleTransitionDelay = 0.2f;
        
        [Title("Performance Settings")]
        [Tooltip("Maximum entities to process per frame")]
        [Range(10, 200)]
        [SerializeField] private int _maxEntitiesPerFrame = 50;
        
        [Tooltip("Enable animation culling for off-screen entities")]
        [SerializeField] private bool _enableAnimationCulling = true;
        
        [ShowIf("_enableAnimationCulling")]
        [Tooltip("Distance from camera to start culling animations")]
        [Range(10f, 100f)]
        [SerializeField] private float _cullingDistance = 30f;
        
        #endregion

        #region Private Fields
        
        private Dictionary<IEntity, AnimationStateTracker> _entityTrackers = new Dictionary<IEntity, AnimationStateTracker>();
        private Queue<IEntity> _processingQueue = new Queue<IEntity>();
        private Camera _mainCamera;
        private int _processedThisFrame = 0;
        private float _systemDeltaTime;
        
        /// <summary>
        /// Tracks animation state and movement for each entity
        /// </summary>
        private class AnimationStateTracker
        {
            public Vector3 lastPosition;
            public float lastMovementTime;
            public float lastSpeedValue;
            public AnimationComponent.AnimationState lastAnimationState;
            public bool wasMoving;
            public float distanceTraveled;
            public bool isInCullingRange;
            
            public AnimationStateTracker(Vector3 position)
            {
                lastPosition = position;
                lastMovementTime = Time.time;
                lastSpeedValue = 0f;
                lastAnimationState = AnimationComponent.AnimationState.None;
                wasMoving = false;
                distanceTraveled = 0f;
                isInCullingRange = true;
            }
        }
        
        #endregion

        #region System Lifecycle
        
        public override void Initialize()
        {
            base.Initialize();
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = FindObjectOfType<Camera>();
            }
            
            Debug.Log("AnimationSystem initialized successfully");
        }
        
        public override void Execute()
        {
            _systemDeltaTime = Time.deltaTime;
            _processedThisFrame = 0;
            
            // Get all entities with animation components
            var entities = EntityRegistry.GetEntitiesWithComponent<AnimationComponent>();
            
            // Clear and refill processing queue
            _processingQueue.Clear();
            foreach (var entity in entities)
            {
                _processingQueue.Enqueue(entity);
            }
            
            // Process entities with frame rate limiting
            ProcessEntitiesThisFrame();
            
            // Clean up removed entities
            CleanupRemovedEntities(entities);
        }
        
        #endregion

        #region Entity Processing
        
        private void ProcessEntitiesThisFrame()
        {
            while (_processingQueue.Count > 0 && _processedThisFrame < _maxEntitiesPerFrame)
            {
                var entity = _processingQueue.Dequeue();
                ProcessEntityAnimation(entity);
                _processedThisFrame++;
            }
            
            // If we have remaining entities, they'll be processed next frame
            if (_processingQueue.Count > 0)
            {
                Debug.Log($"AnimationSystem: {_processingQueue.Count} entities deferred to next frame");
            }
        }
        
        private void ProcessEntityAnimation(IEntity entity)
        {
            if (entity == null || !entity.IsActive)
                return;
                
            // Get required components
            var animationComponent = entity.GetComponent<AnimationComponent>();
            var transformComponent = entity.GetComponent<TransformComponent>();
            
            if (animationComponent == null || transformComponent == null)
                return;
                
            // Get or create tracker for this entity
            if (!_entityTrackers.TryGetValue(entity, out AnimationStateTracker tracker))
            {
                tracker = new AnimationStateTracker(transformComponent.Position);
                _entityTrackers[entity] = tracker;
            }
            
            // Check if entity should be culled
            if (_enableAnimationCulling && ShouldCullEntity(entity, transformComponent, tracker))
            {
                return;
            }
            
            // Process movement-based animations
            if (_enableMovementAnimations)
            {
                ProcessMovementAnimations(entity, animationComponent, transformComponent, tracker);
            }
            
            // Update navigation-based parameters
            UpdateNavigationParameters(entity, animationComponent, tracker);
            
            // Update tracker
            UpdateTracker(tracker, transformComponent);
        }
        
        #endregion

        #region Movement Animation Processing
        
        private void ProcessMovementAnimations(IEntity entity, AnimationComponent animationComponent, 
                                               TransformComponent transformComponent, AnimationStateTracker tracker)
        {
            // Calculate movement
            float distanceMoved = Vector3.Distance(transformComponent.Position, tracker.lastPosition);
            tracker.distanceTraveled += distanceMoved;
            
            bool isMoving = distanceMoved > _movementThreshold * _systemDeltaTime;
            float currentSpeed = distanceMoved / _systemDeltaTime;
            
            // Determine target animation state based on movement
            var targetState = DetermineMovementAnimationState(entity, currentSpeed, isMoving, tracker);
            
            // Apply animation state change if needed
            if (targetState != tracker.lastAnimationState && ShouldChangeAnimation(targetState, tracker))
            {
                // Check if entity is not in combat or other high-priority states
                if (CanChangeToMovementAnimation(animationComponent, targetState))
                {
                    animationComponent.PlayAnimation(targetState);
                    tracker.lastAnimationState = targetState;
                }
            }
            
            // Update movement speed parameter
            animationComponent.SetMovementSpeed(currentSpeed);
            tracker.lastSpeedValue = currentSpeed;
            
            // Update movement timing
            if (isMoving)
            {
                tracker.lastMovementTime = Time.time;
                tracker.wasMoving = true;
            }
            else if (tracker.wasMoving && Time.time - tracker.lastMovementTime > _idleTransitionDelay)
            {
                tracker.wasMoving = false;
            }
        }
        
        private AnimationComponent.AnimationState DetermineMovementAnimationState(IEntity entity, float speed, bool isMoving, AnimationStateTracker tracker)
        {
            // Check if entity has navigation component
            var navigationComponent = entity.GetComponent<NavigationComponent>();
            
            if (isMoving && navigationComponent != null)
            {
                // Determine if running or walking based on speed
                if (speed >= _runSpeedThreshold)
                {
                    return AnimationComponent.AnimationState.Moving; // Could be "Run" if you have separate states
                }
                else if (speed > _movementThreshold)
                {
                    return AnimationComponent.AnimationState.Moving; // Walking
                }
            }
            
            // If not moving and transition delay has passed, return to idle
            if (!tracker.wasMoving || Time.time - tracker.lastMovementTime > _idleTransitionDelay)
            {
                return AnimationComponent.AnimationState.Idle;
            }
            
            // Keep current state if transitioning
            return tracker.lastAnimationState;
        }
        
        private bool ShouldChangeAnimation(AnimationComponent.AnimationState targetState, AnimationStateTracker tracker)
        {
            // Don't change if target is the same as current
            if (targetState == tracker.lastAnimationState)
                return false;
                
            // Always allow transition to idle
            if (targetState == AnimationComponent.AnimationState.Idle)
                return true;
                
            // Allow movement state changes
            if (targetState == AnimationComponent.AnimationState.Moving && 
                (tracker.lastAnimationState == AnimationComponent.AnimationState.Idle || 
                 tracker.lastAnimationState == AnimationComponent.AnimationState.Moving))
                return true;
                
            return false;
        }
        
        private bool CanChangeToMovementAnimation(AnimationComponent animationComponent, AnimationComponent.AnimationState targetState)
        {
            var currentState = animationComponent.CurrentState;
            
            // Don't interrupt high-priority animations
            switch (currentState)
            {
                case AnimationComponent.AnimationState.Attack:
                case AnimationComponent.AnimationState.Death:
                case AnimationComponent.AnimationState.SpecialAttack:
                    return false;
                    
                case AnimationComponent.AnimationState.Aggro:
                case AnimationComponent.AnimationState.Stun:
                case AnimationComponent.AnimationState.Knockback:
                    return targetState == AnimationComponent.AnimationState.Idle; // Only allow idle
                    
                default:
                    return true;
            }
        }
        
        #endregion

        #region Navigation Parameter Updates
        
        private void UpdateNavigationParameters(IEntity entity, AnimationComponent animationComponent, AnimationStateTracker tracker)
        {
            var navigationComponent = entity.GetComponent<NavigationComponent>();
            if (navigationComponent == null)
                return;
                
            bool hasDestination = !navigationComponent.HasReachedDestination;
            animationComponent.SetBool("HasDestination", hasDestination);
            
            // if (navigationComponent.Agent != null)
            // {
            //     float angularSpeed = navigationComponent.Agent.angularSpeed;
            //     animationComponent.SetFloat("TurnSpeed", angularSpeed);
            // }
        }
        
        #endregion

        #region Animation Culling
        
        private bool ShouldCullEntity(IEntity entity, TransformComponent transformComponent, AnimationStateTracker tracker)
        {
            if (_mainCamera == null || !_enableAnimationCulling)
            {
                tracker.isInCullingRange = true;
                return false;
            }
                
            float distanceToCamera = Vector3.Distance(_mainCamera.transform.position, transformComponent.Position);
            tracker.isInCullingRange = distanceToCamera <= _cullingDistance;
            
            // Cull if too far from camera
            return distanceToCamera > _cullingDistance;
        }
        
        #endregion

        #region Helper Methods
        
        private void UpdateTracker(AnimationStateTracker tracker, TransformComponent transformComponent)
        {
            tracker.lastPosition = transformComponent.Position;
        }
        
        private void CleanupRemovedEntities(IEnumerable<IEntity> currentEntities)
        {
            var entitiesToRemove = new List<IEntity>();
            
            foreach (var trackedEntity in _entityTrackers.Keys)
            {
                bool stillExists = false;
                foreach (var currentEntity in currentEntities)
                {
                    if (currentEntity == trackedEntity)
                    {
                        stillExists = true;
                        break;
                    }
                }
                
                if (!stillExists)
                {
                    entitiesToRemove.Add(trackedEntity);
                }
            }
            
            foreach (var entityToRemove in entitiesToRemove)
            {
                _entityTrackers.Remove(entityToRemove);
            }
        }
        
        #endregion

        #region Debug Methods
        
        [Title("Debug Tools")]
        [Button("Debug Animation System State"), ButtonGroup("Debug")]
        private void DebugSystemState()
        {
            Debug.Log("=== ANIMATION SYSTEM DEBUG ===");
            Debug.Log($"Tracked Entities: {_entityTrackers.Count}");
            Debug.Log($"Processing Queue: {_processingQueue.Count}");
            Debug.Log($"Processed This Frame: {_processedThisFrame}");
            Debug.Log($"Max Entities Per Frame: {_maxEntitiesPerFrame}");
            Debug.Log($"Movement Animations Enabled: {_enableMovementAnimations}");
            Debug.Log($"Animation Culling Enabled: {_enableAnimationCulling}");
            
            int culledEntities = 0;
            foreach (var tracker in _entityTrackers.Values)
            {
                if (!tracker.isInCullingRange)
                    culledEntities++;
            }
            Debug.Log($"Currently Culled Entities: {culledEntities}");
        }
        
        [Button("Debug Entity Trackers"), ButtonGroup("Debug")]
        private void DebugEntityTrackers()
        {
            Debug.Log("=== ENTITY TRACKER DEBUG ===");
            int index = 0;
            foreach (var kvp in _entityTrackers)
            {
                var entity = kvp.Key;
                var tracker = kvp.Value;
                
                Debug.Log($"Entity {index}: {entity.Id}");
                Debug.Log($"  Last Animation State: {tracker.lastAnimationState}");
                Debug.Log($"  Last Speed: {tracker.lastSpeedValue:F2}");
                Debug.Log($"  Distance Traveled: {tracker.distanceTraveled:F2}");
                Debug.Log($"  Was Moving: {tracker.wasMoving}");
                Debug.Log($"  In Culling Range: {tracker.isInCullingRange}");
                
                index++;
                if (index >= 5) // Limit output for readability
                {
                    Debug.Log($"... and {_entityTrackers.Count - 5} more entities");
                    break;
                }
            }
        }
        
        [Button("Force Process All Entities"), ButtonGroup("Testing")]
        private void ForceProcessAllEntities()
        {
            _maxEntitiesPerFrame = 1000; // Temporarily increase limit
            Debug.Log("Forcing processing of all entities this frame");
        }
        
        [Button("Clear Entity Trackers"), ButtonGroup("Testing")]
        private void ClearEntityTrackers()
        {
            _entityTrackers.Clear();
            Debug.Log("All entity trackers cleared");
        }
        
        #endregion
    }
}