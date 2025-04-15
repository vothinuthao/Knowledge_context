using System.Collections.Generic;
using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Enhanced steering manager that uses context-based steering for more natural movements
    /// </summary>
    public class ContextSteeringManager : SteeringManager
    {
        [Header("Context Steering Settings")]
        [SerializeField] private int directionCount = 16;
        [SerializeField] private float memoryDuration = 3.0f;
        [SerializeField] private float perceptionRadius = 10f;
        [SerializeField] private LayerMask perceptionLayers;
        [SerializeField] private bool debugDrawMaps = false;
        [SerializeField] private float debugDrawScale = 5f;
        private Vector3 _velocity = Vector3.zero;
        private SteeringContext _extendedContext = new SteeringContext();
        
        // Reference to the formation position target
        private Transform formationPositionTarget;
        
        // Reference to our own list of behaviors
        private List<ISteeringComponent> _steeringBehaviors = new List<ISteeringComponent>();
        
        // Override the Start method
        protected override void Start()
        {
            // Call original implementation
            base.Start();
            
            // Initialize extended context
            _extendedContext.DirectionCount = directionCount;
            _extendedContext.InitializeMaps();
            
            // Sync velocity with base class using properties if available
            _velocity = GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;
        }
        
        // We need to override AddBehavior to track our own list
        public override void AddBehavior(ISteeringComponent behavior)
        {
            base.AddBehavior(behavior);
            
            if (!_steeringBehaviors.Contains(behavior))
            {
                _steeringBehaviors.Add(behavior);
            }
        }
        
        // Override RemoveBehavior to keep our list in sync
        public override void RemoveBehavior(ISteeringComponent behavior)
        {
            base.RemoveBehavior(behavior);
            
            if (_steeringBehaviors.Contains(behavior))
            {
                _steeringBehaviors.Remove(behavior);
            }
        }
        
        // Override ClearBehaviors
        public override void ClearBehaviors()
        {
            base.ClearBehaviors();
            _steeringBehaviors.Clear();
        }
        
        // Override SetTarget
        public override void SetTarget(Transform newTarget)
        {
            base.SetTarget(newTarget);
            
            // Keep track of formation position target
            if (newTarget != null && newTarget.CompareTag("FormationPosition"))
            {
                formationPositionTarget = newTarget;
            }
            
            _extendedContext.Target = newTarget;
        }
        
        // Override the Update method to implement context steering
        protected override void Update()
        {
            // Don't call base.Update() as we're replacing it completely
            
            // Update perception data
            UpdatePerception();
            
            // Update memory (diminish or remove old memories)
            UpdateMemory();
            
            // Update dynamic weights based on situation
            UpdateDynamicWeights();
            
            // Clear maps before each update
            ClearMaps();
            
            // Let each behavior fill the maps
            FillContextMaps();
            
            // Calculate steering based on maps
            Vector3 steeringForce = CalculateContextSteering();
            
            // Apply steering force similar to base class
            _velocity += steeringForce * Time.deltaTime;
            _velocity = Vector3.ClampMagnitude(_velocity, maxSpeed);
            
            transform.position += _velocity * Time.deltaTime;
            
            if (_velocity.magnitude > 0.1f)
            {
                transform.forward = _velocity.normalized;
            }
            
            // Debug visualization
            if (debugDrawMaps)
            {
                DrawContextMaps();
            }
        }
        
        /// <summary>
        /// Update perception data by detecting nearby entities
        /// </summary>
        private void UpdatePerception()
        {
            // Clear previous data
            _extendedContext.VisibleNeighbors.Clear();
            _extendedContext.VisibleEnemies.Clear();
            _extendedContext.VisibleObstacles.Clear();
            
            // Perform detection
            Collider[] hits = Physics.OverlapSphere(transform.position, perceptionRadius, perceptionLayers);
            
            foreach (var hit in hits)
            {
                // Skip self
                if (hit.transform == transform)
                    continue;
                
                // Categorize based on layers or tags
                if (hit.CompareTag("Troop"))
                {
                    _extendedContext.VisibleNeighbors.Add(hit.transform);
                    
                    // Update memory
                    _extendedContext.MemoryEntities[hit.transform] = memoryDuration;
                }
                else if (hit.CompareTag("Enemy"))
                {
                    _extendedContext.VisibleEnemies.Add(hit.transform);
                    
                    // Update memory
                    _extendedContext.MemoryEntities[hit.transform] = memoryDuration;
                }
                else if (hit.CompareTag("Obstacle") || hit.CompareTag("Terrain"))
                {
                    _extendedContext.VisibleObstacles.Add(hit.transform);
                }
            }
            
            // Update standard context with new data
            UpdateContextData();
        }
        
        /// <summary>
        /// Update entity memory, reducing time or removing old memories
        /// </summary>
        private void UpdateMemory()
        {
            List<Transform> keysToRemove = new List<Transform>();
            
            foreach (var pair in _extendedContext.MemoryEntities)
            {
                // Reduce memory duration
                float newDuration = pair.Value - Time.deltaTime;
                
                if (newDuration <= 0)
                {
                    // Forget this entity
                    keysToRemove.Add(pair.Key);
                }
                else
                {
                    // Update memory duration
                    _extendedContext.MemoryEntities[pair.Key] = newDuration;
                }
            }
            
            // Remove forgotten entities
            foreach (var key in keysToRemove)
            {
                _extendedContext.MemoryEntities.Remove(key);
            }
        }
        
        /// <summary>
        /// Update the context data from base properties
        /// </summary>
        private void UpdateContextData()
        {
            _extendedContext.Position = transform.position;
            _extendedContext.Velocity = _velocity;
            _extendedContext.Forward = transform.forward;
            _extendedContext.MaxSpeed = maxSpeed;
            _extendedContext.MaxForce = maxForce;
        }
        
        /// <summary>
        /// Update dynamic weights based on current situation
        /// </summary>
        private void UpdateDynamicWeights()
        {
            // Example: Increase separation weight when neighbors are too close
            int closeNeighbors = 0;
            foreach (var neighbor in _extendedContext.VisibleNeighbors)
            {
                if (Vector3.Distance(transform.position, neighbor.position) < _extendedContext.SeparationRadius)
                {
                    closeNeighbors++;
                }
            }
            
            // Scale separation weight based on number of close neighbors
            _extendedContext.DynamicSeparationWeight = 1.0f + (closeNeighbors * 0.2f);
            
            // Example: Adjust cohesion weight based on squad needs
            bool isInFormation = formationPositionTarget != null && 
                                Vector3.Distance(transform.position, formationPositionTarget.position) < 2.0f;
            _extendedContext.DynamicCohesionWeight = isInFormation ? 0.5f : 1.5f;
            
            // Can add more dynamic weight adjustments based on other factors
        }
        
        /// <summary>
        /// Clear interest and danger maps
        /// </summary>
        private void ClearMaps()
        {
            for (int i = 0; i < _extendedContext.DirectionCount; i++)
            {
                _extendedContext.InterestMap[i] = 0;
                _extendedContext.DangerMap[i] = 0;
            }
        }
        
        /// <summary>
        /// Let each behavior fill the interest and danger maps
        /// </summary>
        private void FillContextMaps()
        {
            foreach (var behavior in _steeringBehaviors)
            {
                if (Random.value <= behavior.GetProbability())
                {
                    // Check if behavior supports context maps
                    IContextSteeringBehavior contextBehavior = behavior as IContextSteeringBehavior;
                    if (contextBehavior != null)
                    {
                        contextBehavior.FillContextMaps(_extendedContext, behavior.GetWeight());
                    }
                    else
                    {
                        // For legacy behaviors, convert vector to map contribution
                        Vector3 force = behavior.CalculateForce(_extendedContext);
                        if (force.magnitude > 0.01f)
                        {
                            int dirIndex = _extendedContext.GetIndexFromDirection(force);
                            _extendedContext.InterestMap[dirIndex] += force.magnitude * behavior.GetWeight();
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Calculate final steering direction based on interest and danger maps
        /// </summary>
        private Vector3 CalculateContextSteering()
        {
            // Create a combined map (interest - danger)
            float[] combinedMap = new float[_extendedContext.DirectionCount];
            
            for (int i = 0; i < _extendedContext.DirectionCount; i++)
            {
                combinedMap[i] = _extendedContext.InterestMap[i] - _extendedContext.DangerMap[i];
            }
            
            // Find best direction
            int bestIndex = 0;
            float bestValue = combinedMap[0];
            
            for (int i = 1; i < _extendedContext.DirectionCount; i++)
            {
                if (combinedMap[i] > bestValue)
                {
                    bestValue = combinedMap[i];
                    bestIndex = i;
                }
            }
            
            // If all directions are equally bad/good, maintain current velocity
            if (Mathf.Approximately(bestValue, 0))
            {
                return Vector3.zero;
            }
            
            // Get the vector for the best direction
            Vector3 desiredDirection = _extendedContext.GetDirectionFromIndex(bestIndex);
            
            // Calculate desired velocity
            Vector3 desiredVelocity = desiredDirection * maxSpeed;
            
            // Calculate and return steering force
            return Vector3.ClampMagnitude(desiredVelocity - _velocity, maxForce);
        }
        
        /// <summary>
        /// Draw debug visualization of the context maps
        /// </summary>
        private void DrawContextMaps()
        {
            for (int i = 0; i < _extendedContext.DirectionCount; i++)
            {
                Vector3 direction = _extendedContext.GetDirectionFromIndex(i);
                
                // Draw interest as green rays
                if (_extendedContext.InterestMap[i] > 0)
                {
                    float length = _extendedContext.InterestMap[i] * debugDrawScale;
                    Debug.DrawRay(transform.position, direction * length, Color.green);
                }
                
                // Draw danger as red rays
                if (_extendedContext.DangerMap[i] > 0)
                {
                    float length = _extendedContext.DangerMap[i] * debugDrawScale;
                    Debug.DrawRay(transform.position, direction * length, Color.red);
                }
                
                // Draw combined value as blue rays
                float combined = _extendedContext.InterestMap[i] - _extendedContext.DangerMap[i];
                if (combined > 0)
                {
                    float length = combined * debugDrawScale;
                    Debug.DrawRay(transform.position, direction * length, Color.blue);
                }
            }
        }
    }
}