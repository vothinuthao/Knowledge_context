namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Simplified formation types focusing on 3 core formations
    /// Reduced from 7 to 3 formations for better focus and stability
    /// </summary>
    public enum FormationType
    {
        /// <summary>
        /// Standard 3x3 grid formation - balanced for movement and combat
        /// Units arranged in a 3x3 grid with even spacing
        /// </summary>
        Normal = 0,
        
        /// <summary>
        /// Phalanx formation - tight combat grid optimized for melee
        /// Units arranged in close formation for maximum combat effectiveness
        /// </summary>
        Phalanx = 1,
        
        /// <summary>
        /// Testudo formation - very tight defensive formation
        /// Units arranged in extremely close formation for maximum defense
        /// </summary>
        Testudo = 2
    }
}