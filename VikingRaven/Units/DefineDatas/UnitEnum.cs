#region Supporting Enums

namespace VikingRaven.Units
{
    public enum UnitType
    {
        Infantry,   // Balanced melee fighters
        Archer,     // Ranged units
        Pike        // Anti-cavalry spear units
    }

    /// <summary>
    /// Tactical roles for unit specialization
    /// </summary>
    public enum UnitRole
    {
        None,
        Frontline,        // Front line fighters
        HeavyFrontline,   // Heavy armored front line
        Ranged,          // Ranged attackers
        Support,         // Support units
        AntiCavalry,     // Anti-cavalry specialists
        Skirmisher       // Light mobile units
    }

    /// <summary>
    /// Tactical behavior preferences
    /// </summary>
    public enum TacticalPreference
    {
        Defensive,   // Prefer defensive tactics
        Balanced,    // Balanced approach
        Aggressive   // Prefer aggressive tactics
    }

    /// <summary>
    /// Formation preferences for unit types
    /// </summary>
    public enum FormationPreference
    {
        Normal,     // Standard formations
        Phalanx,    // Tight combat formations
        Loose       // Spread out formations
    }

    /// <summary>
    /// Preferred combat engagement ranges
    /// </summary>
    public enum CombatRange
    {
        Melee,      // Close combat
        Extended,   // Spear/pike range
        Ranged      // Bow/crossbow range
    }

    #endregion
}