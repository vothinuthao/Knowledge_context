using UnityEngine;

namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Surround Behavior", menuName = "Wiking Raven/Behaviors/Special/Surround")]
    public class SurroundBehaviorSO : SteeringBehaviorSO
    {
        [Tooltip("Bán kính bao vây quanh mục tiêu")]
        public float surroundRadius = 3f;
        
        [Tooltip("Tốc độ di chuyển khi bao vây")]
        public float surroundSpeed = 2f;
        
        [HideInInspector]
        public int priority = 1;
        
        public override ISteeringBehavior Create()
        {
            return new SurroundBehavior(weight, surroundRadius, surroundSpeed);
        }
    }
}