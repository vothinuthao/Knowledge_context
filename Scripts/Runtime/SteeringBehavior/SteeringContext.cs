using Troop;
using UnityEngine;
using Utils;

namespace SteeringBehavior
{
    public class SteeringContext
    {
        public TroopModel TroopModel { get; set; }
        public float DeltaTime { get; set; }
        public Vector3 TargetPosition { get; set; }
        public Vector3 AvoidPosition { get; set; }
        public Transform[] Obstacles { get; set; }
        public TroopController[] NearbyAllies { get; set; }
        public TroopController[] NearbyEnemies { get; set; }
        public Vector3 SquadCenterPosition { get; set; }
        public Vector3 DesiredSquadPosition { get; set; }
        public BehaviorPath CurrentPath { get; set; }
        public bool IsInDanger { get; set; }
        public float SlowingDistance { get; set; } = 3f;
        public float SeparationRadius { get; set; } = 2f;
    
        public SteeringContext()
        {
            TargetPosition = Vector3.zero;
            AvoidPosition = Vector3.zero;
            SquadCenterPosition = Vector3.zero;
            DesiredSquadPosition = Vector3.zero;
            IsInDanger = false;
        }
    
        public Vector3 GetDirectionToTarget()
        {
            if (TroopModel == null) return Vector3.zero;
            return (TargetPosition - TroopModel.Position).normalized;
        }
    
        public float GetDistanceToTarget()
        {
            if (TroopModel == null) return 0f;
            return Vector3.Distance(TroopModel.Position, TargetPosition);
        }

        public Transform GetTransformGameObject()
        {
            return TroopModel.GetComponent<Transform>();
        }
    }
}