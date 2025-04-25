// File: Core/Performance/SpatialGrid.cs
using System.Collections.Generic;
using UnityEngine;
using Core.ECS;

namespace Core.Performance
{
    /// <summary>
    /// Spatial partitioning for efficient entity lookups
    /// </summary>
    public class SpatialGrid
    {
        private Dictionary<int, List<Entity>> _cells;
        private int _gridWidth;
        private int _gridHeight;
        private float _cellSize;
        
        public SpatialGrid(int width, int height, float cellSize)
        {
            _gridWidth = width;
            _gridHeight = height;
            _cellSize = cellSize;
            _cells = new Dictionary<int, List<Entity>>();
        }
        
        private int GetCellKey(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x / _cellSize);
            int z = Mathf.FloorToInt(position.z / _cellSize);
            
            // Clamp to grid bounds
            x = Mathf.Clamp(x, 0, _gridWidth - 1);
            z = Mathf.Clamp(z, 0, _gridHeight - 1);
            
            return x + z * _gridWidth;
        }
        
        public void Clear()
        {
            foreach (var cell in _cells.Values)
            {
                cell.Clear();
            }
        }
        
        public void Insert(Entity entity, Vector3 position)
        {
            int key = GetCellKey(position);
            
            if (!_cells.ContainsKey(key))
            {
                _cells[key] = new List<Entity>();
            }
            
            _cells[key].Add(entity);
        }
        
        public List<Entity> GetNearbyEntities(Vector3 position, float radius)
        {
            var result = new List<Entity>();
            int cellRadius = Mathf.CeilToInt(radius / _cellSize);
            
            int centerX = Mathf.FloorToInt(position.x / _cellSize);
            int centerZ = Mathf.FloorToInt(position.z / _cellSize);
            
            // Check neighboring cells
            for (int x = centerX - cellRadius; x <= centerX + cellRadius; x++)
            {
                for (int z = centerZ - cellRadius; z <= centerZ + cellRadius; z++)
                {
                    // Skip cells outside grid
                    if (x < 0 || x >= _gridWidth || z < 0 || z >= _gridHeight)
                        continue;
                    
                    int key = x + z * _gridWidth;
                    if (_cells.ContainsKey(key))
                    {
                        result.AddRange(_cells[key]);
                    }
                }
            }
            
            return result;
        }
    }
}

// File: Core/Performance/ObjectPool.cs
namespace Core.Performance
{
    /// <summary>
    /// Generic object pool for reducing allocations
    /// </summary>
    public class ObjectPool<T> where T : new()
    {
        private Stack<T> _pool;
        private int _maxSize;
        
        public ObjectPool(int initialSize = 100, int maxSize = 1000)
        {
            _maxSize = maxSize;
            _pool = new Stack<T>(initialSize);
            
            // Pre-allocate objects
            for (int i = 0; i < initialSize; i++)
            {
                _pool.Push(new T());
            }
        }
        
        public T Get()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
            
            return new T();
        }
        
        public void Return(T item)
        {
            if (_pool.Count < _maxSize)
            {
                _pool.Push(item);
            }
        }
        
        public void Clear()
        {
            _pool.Clear();
        }
    }
}