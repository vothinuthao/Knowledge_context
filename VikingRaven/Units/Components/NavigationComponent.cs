using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class NavigationComponent : BaseComponent
    {
        [SerializeField] private NavMeshAgent _navMeshAgent;
        [SerializeField] private float _stoppingDistance = 0.1f;
        [SerializeField] private bool _isPathfindingActive = true;
        
        private Vector3 _destination;
        private List<Vector3> _currentPath = new List<Vector3>();
        private int _currentWaypointIndex = 0;
        
        public Vector3 Destination => _destination;
        public bool IsPathfindingActive => _isPathfindingActive;
        public bool HasReachedDestination => Vector3.Distance(transform.position, _destination) <= _stoppingDistance;

        public override void Initialize()
        {
            if (_navMeshAgent == null)
            {
                _navMeshAgent = GetComponent<NavMeshAgent>();
                
                if (_navMeshAgent == null)
                {
                    _navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
                }
            }
            
            // Configure NavMeshAgent
            _navMeshAgent.stoppingDistance = _stoppingDistance;
            _navMeshAgent.updateRotation = false; // We'll handle rotation separately
        }

        public void SetDestination(Vector3 destination)
        {
            _destination = destination;
            _currentPath.Clear();
            _currentWaypointIndex = 0;
            
            if (_isPathfindingActive && _navMeshAgent != null && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.SetDestination(destination);
            }
        }

        public void UpdatePathfinding()
        {
            if (!IsActive || !_isPathfindingActive || _navMeshAgent == null)
                return;
                
            // If we've reached the destination, stop
            if (HasReachedDestination)
            {
                _navMeshAgent.isStopped = true;
                return;
            }
            
            // Otherwise, ensure the agent is moving
            _navMeshAgent.isStopped = false;
        }

        public void EnablePathfinding()
        {
            _isPathfindingActive = true;
            
            if (_navMeshAgent != null)
            {
                _navMeshAgent.isStopped = false;
            }
        }

        public void DisablePathfinding()
        {
            _isPathfindingActive = false;
            
            if (_navMeshAgent != null)
            {
                _navMeshAgent.isStopped = true;
            }
        }

        public List<Vector3> GetCurrentPath()
        {
            if (_navMeshAgent == null || !_navMeshAgent.hasPath)
                return _currentPath;
                
            // Convert NavMeshAgent path to a list of points
            NavMeshPath path = new NavMeshPath();
            _navMeshAgent.CalculatePath(_destination, path);
            
            _currentPath.Clear();
            foreach (var corner in path.corners)
            {
                _currentPath.Add(corner);
            }
            
            return _currentPath;
        }

        public override void Cleanup()
        {
            if (_navMeshAgent != null)
            {
                _navMeshAgent.isStopped = true;
            }
        }
    }
}