using UnityEngine;

namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Alignment Behavior", menuName = "Wiking Raven/Behaviors/Alignment")]
    public class AlignmentBehaviorSO : SteeringBehaviorSO
    {
        [Tooltip("Khoảng cách tối đa mà troop sẽ xem xét khi tính alignment")]
        public float alignmentRadius = 5f;
    
        public override ISteeringBehavior Create()
        {
            return new AlignmentBehavior(weight, alignmentRadius);
        }
    }
}