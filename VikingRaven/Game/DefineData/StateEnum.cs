namespace VikingRaven.Game.DefineData
{
    /// <summary>
    /// Enum defining different game states
    /// </summary>
    public enum GameState
    {
        NotInitialized,
        Initializing,
        Initialized,
        LoadingData,
        DataLoaded,
        SpawningSquads,
        SquadsSpawned,
        Playing,
        Paused,
        Completed,
        Failed
    }
}