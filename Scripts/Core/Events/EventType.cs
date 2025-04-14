namespace Core
{
    public enum EventType
    {
        TroopCreated,
        TroopDamaged,
        TroopDeath,
        TroopAttack,
        TroopFleeing,
        TroopStateChanged,
        SquadMoved,
        SquadFormationChanged,
        SquadCommandIssued,
        GameStateChanged,
        EnemySpawned,
        EnemyKilled
    }
}