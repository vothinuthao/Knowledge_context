using System.Collections.Generic;
using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Manages and blends multiple steering behaviors
    /// </summary>
    public class SteeringManager : MonoBehaviour
    {
        [SerializeField] protected float maxSpeed = 5.0f;
        [SerializeField] protected float maxForce = 10.0f;
        [SerializeField] protected Transform target;
        [SerializeField] protected LayerMask obstacleLayer;
        
        [Header("Radius Settings")]
        [SerializeField] private float slowingRadius = 3.0f;
        [SerializeField] private float arrivalRadius = 0.5f;
        [SerializeField] private float separationRadius = 2.0f;
        [SerializeField] private float neighborRadius = 5.0f;
        [SerializeField] private float fleeRadius = 5.0f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        private List<ISteeringComponent> _steeringBehaviors = new List<ISteeringComponent>();
        private SteeringContext _context = new SteeringContext();
        private Vector3 _velocity = Vector3.zero;
        
        protected virtual void Start()
        {
            InitializeContext();
        }

        protected virtual void Update()
        {
            UpdateContext();
            Vector3 steeringForce = CalculateTotalSteering();
            _velocity += steeringForce * Time.deltaTime;
            
            _velocity = Vector3.ClampMagnitude(_velocity, maxSpeed);
            
            transform.position += _velocity * Time.deltaTime;
            
            if (_velocity.magnitude > 0.1f)
            {
                transform.forward = _velocity.normalized;
            }
            
            if (showDebugInfo)
            {
                Debug.DrawRay(transform.position, _velocity, Color.blue);
                Debug.DrawRay(transform.position, steeringForce, Color.red);
            }
        }
        
        /// <summary>
        /// Initialize the steering context with current values
        /// </summary>
        private void InitializeContext()
        {
            _context.MaxSpeed = maxSpeed;
            _context.MaxForce = maxForce;
            _context.Target = target;
            _context.ObstacleLayer = obstacleLayer;
            _context.SlowingRadius = slowingRadius;
            _context.ArrivalRadius = arrivalRadius;
            _context.SeparationRadius = separationRadius;
            _context.NeighborRadius = neighborRadius;
            _context.FleeRadius = fleeRadius;
        }
        
        /// <summary>
        /// Update the steering context with current values
        /// </summary>
        private void UpdateContext()
        {
            _context.Position = transform.position;
            _context.Velocity = _velocity;
            _context.Forward = transform.forward;
            _context.Target = target;
        }
        
        /// <summary>
        /// Calculate total steering force from all behaviors
        /// </summary>
        private Vector3 CalculateTotalSteering()
        {
            Vector3 totalForce = Vector3.zero;

            foreach (var behavior in _steeringBehaviors)
            {
                if (Random.value <= behavior.GetProbability())
                {
                    Vector3 force = behavior.CalculateForce(_context);
                    totalForce += force * behavior.GetWeight();
                    
                    if (showDebugInfo)
                    {
                        Debug.DrawRay(transform.position, force.normalized * behavior.GetWeight(), Color.yellow);
                    }
                }
            }
            return Vector3.ClampMagnitude(totalForce, maxForce);
        }
        
        /// <summary>
        /// Add a steering behavior to the manager
        /// </summary>
        public virtual void AddBehavior(ISteeringComponent behavior)
        {
            if (!_steeringBehaviors.Contains(behavior))
            {
                _steeringBehaviors.Add(behavior);
            }
        }
        
        /// <summary>
        /// Remove a steering behavior from the manager
        /// </summary>
        public virtual void RemoveBehavior(ISteeringComponent behavior)
        {
            if (_steeringBehaviors.Contains(behavior))
            {
                _steeringBehaviors.Remove(behavior);
            }
        }
        
        /// <summary>
        /// Remove all steering behaviors
        /// </summary>
        public virtual void ClearBehaviors()
        {
            _steeringBehaviors.Clear();
        }
        
        /// <summary>
        /// Set the target for steering behaviors
        /// </summary>
        public virtual void SetTarget(Transform newTarget)
        {
            target = newTarget;
            _context.Target = newTarget;
        }
    }
}