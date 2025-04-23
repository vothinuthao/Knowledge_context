using SteeringBehavior;
using UnityEngine;

[CreateAssetMenu(fileName = "Ambush Move Behavior", menuName = "Wiking Raven/Behaviors/Special/Ambush Move")]
public class AmbushMoveBehaviorSO : SteeringBehaviorSO
{
    [Tooltip("Hệ số tốc độ di chuyển khi ambush (thấp hơn 1)")]
    [Range(0.1f, 1f)]
    public float moveSpeedMultiplier = 0.5f;
        
    [Tooltip("Hệ số giảm phạm vi phát hiện của kẻ địch (thấp hơn 1)")]
    [Range(0.1f, 1f)]
    public float detectionRadiusMultiplier = 0.5f;
        
    [HideInInspector]
    public int priority = 1;
        
    public override ISteeringBehavior Create()
    {
        return new AmbushMoveBehavior(weight, moveSpeedMultiplier, detectionRadiusMultiplier);
    }
}
