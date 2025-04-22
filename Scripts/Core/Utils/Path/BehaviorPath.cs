using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public class BehaviorPath
    {
        private List<Vector3> _waypoints = new List<Vector3>();
        private int _currentWaypoint = 0;
        private bool _isLooped = false;
        
        public List<Vector3> Waypoints => _waypoints;
        public int CurrentWaypoint => CurrentWaypoint;
        public bool IsLooped => _isLooped;
        
        public Vector3 GetCurrentWaypoint()
        {
            if (_waypoints.Count == 0) return Vector3.zero;
            return _waypoints[CurrentWaypoint];
        }
        
        public bool AdvanceToNext()
        {
            if (_waypoints.Count == 0) return false;
            
            _currentWaypoint++;
            if (CurrentWaypoint >= _waypoints.Count)
            {
                if (IsLooped)
                {
                    _currentWaypoint = 0;
                    return true;
                }
                else
                {
                    _currentWaypoint = _waypoints.Count - 1;
                    return false;
                }
            }
            return true;
        }

        public void SetWaypoints(List<Vector3>  waypoints)
        {
            _waypoints = waypoints;
        }
        public void SetCurrentWaypoint(int currentWaypoint)
        {
            _currentWaypoint = currentWaypoint;
        }
        
        public void SetIsLooped(bool  isLooped)
        {
            _isLooped = isLooped;
        }
    }
}