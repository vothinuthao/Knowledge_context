using RavenDeckbuilding.Core;
using UnityEngine;
using RavenDeckbuilding.Core.Data;

namespace RavenDeckbuilding.Systems.Input
{
    /// <summary>
    /// High-performance ring buffer for input history with zero allocations during operation
    /// Capacity is fixed at compile time for predictable memory usage
    /// </summary>
    public class InputRingBuffer
    {
        private const int BUFFER_SIZE = 256; // Must be power of 2 for efficient modulo
        private const int BUFFER_MASK = BUFFER_SIZE - 1;
        
        private readonly InputEvent[] _buffer;
        private int _head;
        private int _tail;
        private int _count;
        
        public int Count => _count;
        public int Capacity => BUFFER_SIZE;
        public bool IsFull => _count == BUFFER_SIZE;
        public bool IsEmpty => _count == 0;

        public InputRingBuffer()
        {
            _buffer = new InputEvent[BUFFER_SIZE];
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        /// <summary>
        /// Add input event to buffer. Returns false if buffer is full.
        /// O(1) operation with zero allocations.
        /// </summary>
        public bool TryAdd(InputEvent inputEvent)
        {
            if (IsFull)
            {
                // Overwrite oldest entry in ring buffer
                _tail = (_tail + 1) & BUFFER_MASK;
            }
            else
            {
                _count++;
            }

            _buffer[_head] = inputEvent;
            _head = (_head + 1) & BUFFER_MASK;
            return true;
        }

        /// <summary>
        /// Get the most recent input events up to maxCount
        /// Returns actual number of events retrieved
        /// </summary>
        public int GetRecent(InputEvent[] output, int maxCount)
        {
            if (output == null || maxCount <= 0 || IsEmpty)
                return 0;

            int actualCount = Mathf.Min(maxCount, _count);
            int currentIndex = (_head - 1) & BUFFER_MASK;
            
            for (int i = 0; i < actualCount; i++)
            {
                output[i] = _buffer[currentIndex];
                currentIndex = (currentIndex - 1) & BUFFER_MASK;
            }
            
            return actualCount;
        }

        /// <summary>
        /// Get input events within time window (in seconds)
        /// Returns number of events found
        /// </summary>
        public int GetRecentByTime(InputEvent[] output, int maxCount, float timeWindow)
        {
            if (output == null || maxCount <= 0 || IsEmpty)
                return 0;

            float currentTime = Time.realtimeSinceStartup;
            float cutoffTime = currentTime - timeWindow;
            int foundCount = 0;
            int currentIndex = (_head - 1) & BUFFER_MASK;
            
            for (int i = 0; i < _count && foundCount < maxCount; i++)
            {
                InputEvent evt = _buffer[currentIndex];
                if (evt.Timestamp >= cutoffTime)
                {
                    output[foundCount++] = evt;
                }
                else
                {
                    break; // Events are ordered by time, so we can stop here
                }
                currentIndex = (currentIndex - 1) & BUFFER_MASK;
            }
            
            return foundCount;
        }

        /// <summary>
        /// Clear expired events older than maxAge seconds
        /// </summary>
        public void CleanupExpired(float maxAge)
        {
            if (IsEmpty) return;

            float currentTime = Time.realtimeSinceStartup;
            float cutoffTime = currentTime - maxAge;
            
            // Remove old events from tail
            while (!IsEmpty)
            {
                if (_buffer[_tail].Timestamp >= cutoffTime)
                    break;
                    
                _tail = (_tail + 1) & BUFFER_MASK;
                _count--;
            }
        }

        /// <summary>
        /// Get the most recent input event without removing it
        /// </summary>
        public bool TryPeekLatest(out InputEvent latest)
        {
            if (IsEmpty)
            {
                latest = default;
                return false;
            }

            latest = _buffer[(_head - 1) & BUFFER_MASK];
            return true;
        }

        /// <summary>
        /// Clear all events in the buffer
        /// </summary>
        public void Clear()
        {
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        /// <summary>
        /// Get buffer utilization percentage for monitoring
        /// </summary>
        public float GetUtilization() => (float)_count / BUFFER_SIZE;
    }
}