using UnityEngine;

namespace RavenDeckbuilding.Core
{
    /// <summary>
    /// Central data structure for game state
    /// </summary>
    public struct GameContext
    {
        public Player Caster;
        public Player Target;
        public Vector3 TargetPosition;
        public Vector3 CastDirection;
        public float DeltaTime;
        public uint FrameNumber;
        
        public static GameContext Create(Player caster, Vector3 targetPos)
        {
            return new GameContext
            {
                Caster = caster,
                TargetPosition = targetPos,
                DeltaTime = Time.deltaTime,
                FrameNumber = (uint)Time.frameCount
            };
        }
        
        public bool IsValid => Caster != null;
    }
}