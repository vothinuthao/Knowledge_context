using UnityEngine;

namespace RavenDeckbuilding.Core
{
    /// <summary>
    /// Represents a single input event from player
    /// </summary>
    public struct InputEvent
    {
        public float Timestamp;
        public InputType InputType;
        public Vector2 Position;
        public Vector2 Direction;
        public int CardIndex;
        public uint SequenceId;
        
        public static InputEvent Create(InputType type, Vector2 pos, int cardIdx = -1)
        {
            return new InputEvent
            {
                Timestamp = Time.unscaledTime,
                InputType = type,
                Position = pos,
                CardIndex = cardIdx,
                SequenceId = InputSequence.Next()
            };
        }
        
        public bool IsExpired(float maxAge) => 
            Time.unscaledTime - Timestamp > maxAge;
    }
    
    public enum InputType : byte
    {
        None = 0,
        CardSelect,
        CardDrag,
        CardDrop,
        TargetSelect,
        Cancel,
        Confirm
    }
    
    public static class InputSequence
    {
        private static uint _currentSequence = 0;
        public static uint Next() => ++_currentSequence;
    }
}