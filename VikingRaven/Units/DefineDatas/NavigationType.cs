namespace VikingRaven.Units
{
    public enum NavigationCommandPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }
    
    public enum MovementPhase
    {
        DirectMovement,
        MoveToLeader,
        MoveToFormation,
        InFormation
    }
}