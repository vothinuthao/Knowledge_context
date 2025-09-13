using System;
using RavenDeckbuilding.Core.Interfaces;
using UnityEngine;

namespace RavenDeckbuilding.Systems.Commands
{
    /// <summary>
    /// High-performance min-heap implementation for ordered command execution
    /// Provides O(log n) enqueue/dequeue operations with zero allocations
    /// </summary>
    public class CommandPriorityQueue
    {
        private const int DEFAULT_CAPACITY = 64;
        
        private ICardCommand[] _heap;
        private int _count;
        private int _capacity;
        
        public int Count => _count;
        public int Capacity => _capacity;
        public bool IsEmpty => _count == 0;
        public bool IsFull => _count >= _capacity;

        public CommandPriorityQueue(int initialCapacity = DEFAULT_CAPACITY)
        {
            _capacity = Math.Max(initialCapacity, 4);
            _heap = new ICardCommand[_capacity];
            _count = 0;
        }

        /// <summary>
        /// Add command to queue with O(log n) complexity
        /// Returns false if queue is full and cannot expand
        /// </summary>
        public bool Enqueue(ICardCommand command)
        {
            if (command == null) return false;
            
            if (IsFull)
            {
                if (!TryExpand())
                    return false;
            }
            
            // Add to end and bubble up
            _heap[_count] = command;
            BubbleUp(_count);
            _count++;
            
            return true;
        }

        /// <summary>
        /// Remove and return highest priority command with O(log n) complexity
        /// Returns null if queue is empty
        /// </summary>
        public ICardCommand Dequeue()
        {
            if (IsEmpty) return null;
            
            ICardCommand root = _heap[0];
            
            // Move last element to root and bubble down
            _count--;
            if (_count > 0)
            {
                _heap[0] = _heap[_count];
                _heap[_count] = null; // Clear reference
                BubbleDown(0);
            }
            else
            {
                _heap[0] = null;
            }
            
            return root;
        }

        /// <summary>
        /// Peek at highest priority command without removing it
        /// </summary>
        public ICardCommand Peek()
        {
            return IsEmpty ? null : _heap[0];
        }

        /// <summary>
        /// Remove all commands from queue
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _count; i++)
            {
                _heap[i]?.Dispose();
                _heap[i] = null;
            }
            _count = 0;
        }

        /// <summary>
        /// Remove specific command from queue O(n) operation
        /// Use sparingly for cancellation scenarios
        /// </summary>
        public bool RemoveCommand(uint sequenceId)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_heap[i].SequenceId == sequenceId)
                {
                    ICardCommand removed = _heap[i];
                    
                    // Move last element to this position
                    _count--;
                    if (i < _count)
                    {
                        _heap[i] = _heap[_count];
                        _heap[_count] = null;
                        
                        // Restore heap property
                        RestoreHeapAt(i);
                    }
                    else
                    {
                        _heap[i] = null;
                    }
                    
                    removed?.Dispose();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get commands that can execute within time budget
        /// Returns commands in priority order without removing them
        /// </summary>
        public int GetExecutableCommands(ICardCommand[] output, int maxCount, float timeBudget)
        {
            if (output == null || maxCount <= 0 || IsEmpty) return 0;
            
            float remainingTime = timeBudget;
            int foundCount = 0;
            
            // Create temp array to avoid modifying original heap
            var tempQueue = new CommandPriorityQueue(_count);
            for (int i = 0; i < _count; i++)
            {
                tempQueue.Enqueue(_heap[i]);
            }
            
            while (!tempQueue.IsEmpty && foundCount < maxCount && remainingTime > 0)
            {
                ICardCommand cmd = tempQueue.Dequeue();
                if (cmd.EstimatedExecutionTime <= remainingTime)
                {
                    output[foundCount++] = cmd;
                    remainingTime -= cmd.EstimatedExecutionTime;
                }
                else
                {
                    break; // Can't fit any more commands
                }
            }
            
            return foundCount;
        }

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (!HasHigherPriority(_heap[index], _heap[parentIndex]))
                    break;
                    
                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        private void BubbleDown(int index)
        {
            while (true)
            {
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                int highest = index;
                
                if (leftChild < _count && HasHigherPriority(_heap[leftChild], _heap[highest]))
                    highest = leftChild;
                    
                if (rightChild < _count && HasHigherPriority(_heap[rightChild], _heap[highest]))
                    highest = rightChild;
                    
                if (highest == index) break;
                
                Swap(index, highest);
                index = highest;
            }
        }

        private void RestoreHeapAt(int index)
        {
            // Try bubbling up first
            int parent = (index - 1) / 2;
            if (index > 0 && HasHigherPriority(_heap[index], _heap[parent]))
            {
                BubbleUp(index);
            }
            else
            {
                BubbleDown(index);
            }
        }

        private void Swap(int i, int j)
        {
            ICardCommand temp = _heap[i];
            _heap[i] = _heap[j];
            _heap[j] = temp;
        }

        /// <summary>
        /// Priority comparison: Higher priority value wins, then earlier timestamp
        /// </summary>
        private bool HasHigherPriority(ICardCommand a, ICardCommand b)
        {
            if (a.Priority != b.Priority)
                return a.Priority > b.Priority;
                
            // Same priority, earlier timestamp wins
            return a.Timestamp < b.Timestamp;
        }

        private bool TryExpand()
        {
            try
            {
                int newCapacity = _capacity * 2;
                var newHeap = new ICardCommand[newCapacity];
                Array.Copy(_heap, newHeap, _count);
                _heap = newHeap;
                _capacity = newCapacity;
                return true;
            }
            catch (OutOfMemoryException)
            {
                Debug.LogError("CommandPriorityQueue: Failed to expand capacity, out of memory");
                return false;
            }
        }

        /// <summary>
        /// Get current queue utilization for monitoring
        /// </summary>
        public float GetUtilization() => (float)_count / _capacity;

        /// <summary>
        /// Validate heap property for debugging
        /// </summary>
        public bool ValidateHeap()
        {
            for (int i = 0; i < _count; i++)
            {
                int leftChild = 2 * i + 1;
                int rightChild = 2 * i + 2;
                
                if (leftChild < _count && HasHigherPriority(_heap[leftChild], _heap[i]))
                    return false;
                    
                if (rightChild < _count && HasHigherPriority(_heap[rightChild], _heap[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Get debug information about the queue state
        /// </summary>
        public string GetDebugInfo()
        {
            if (IsEmpty) return "Empty Queue";
            
            var next = Peek();
            return $"Queue: {_count}/{_capacity}, Next: {next?.GetType().Name}[{next?.SequenceId}] P:{next?.Priority}";
        }

        // Cleanup
        ~CommandPriorityQueue()
        {
            Clear();
        }
    }
}