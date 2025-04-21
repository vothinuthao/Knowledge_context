using UnityEngine;

namespace SteeringBehavior
{
    public abstract class SteeringBehaviorSO : ScriptableObject, ISteeringBehaviorFactory
    {
        [Tooltip("Trọng số của behavior trong phép tính tổng hợp")]
        [Range(0f, 10f)]
        public float weight = 1.0f;
        
        [Tooltip("Mô tả về behavior này")]
        [TextArea(1, 3)]
        public string description;
        public abstract ISteeringBehavior Create();
    }
}