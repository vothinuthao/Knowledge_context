namespace Troops.Config
{
    /// <summary>
    /// Enum for different types of steering behaviors
    /// </summary>
    public enum BehaviorType
    {
        None = 0,
        Seek,
        Flee,
        Arrival,
        Separation,
        Cohesion,
        Alignment,
        ObstacleAvoidance,
        PathFollowing
    }
}