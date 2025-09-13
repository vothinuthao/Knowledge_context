using UnityEngine;

namespace RavenDeckbuilding.Utilities
{
    [CreateAssetMenu(fileName = "Debug Settings", menuName = "RavenHouse/Debug Settings")]
    public class DebugSettings : ScriptableObject
    {
        [Header("Performance")]
        public bool showFPS = true;
        public bool showMemoryUsage = true;
        public bool logPerformanceWarnings = true;
        
        [Header("Gameplay")]
        public bool enableGodMode = false;
        public bool showCardStates = false;
        public bool logInputEvents = false;
        
        [Header("Visual")]
        public bool showColliders = false;
        public bool showCommandQueue = false;
        public bool enableWireframe = false;
    }
}