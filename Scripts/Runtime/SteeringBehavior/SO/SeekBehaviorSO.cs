using UnityEngine;

namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Seek Behavior", menuName = "Wiking Raven/Behaviors/Seek")]
    public class SeekBehaviorSO : SteeringBehaviorSO
    {
        public override ISteeringBehavior Create()
        {
            return new SeekBehavior(weight);
        }
    }
}