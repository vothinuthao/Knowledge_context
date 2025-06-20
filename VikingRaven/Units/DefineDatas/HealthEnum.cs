#region Supporting Enums and Classes

namespace VikingRaven.Units
{
    public enum InjuryState
    {
        Healthy,
        Light,
        Moderate,
        Severe
    }

    public enum RecoveryPhase
    {
        None,
        Initial,
        Stabilizing,
        Recovering
    }

    [System.Serializable]
    public class StatusEffect
    {
        public string Name;
        public float Duration;
        public float RemainingTime;
    }
    #endregion
}