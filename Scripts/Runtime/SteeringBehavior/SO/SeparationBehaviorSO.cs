using UnityEngine;
namespace SteeringBehavior
{
    [CreateAssetMenu(fileName = "Separation Behavior", menuName = "Wiking Raven/Behaviors/Separation")]
    public class SeparationBehaviorSO : SteeringBehaviorSO
    {
        [Tooltip("Khoảng cách tối đa mà troop sẽ tạo lực đẩy với troop khác")]
        public float separationRadius = 2f;
    
        public override ISteeringBehavior Create()
        {
            return new SeparationBehavior(weight, separationRadius);
        }
    }
}