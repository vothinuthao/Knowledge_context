using System.Collections.Generic;
using UnityEngine;

namespace Core.Grid
{
    public class AStarPathfinder
    {
        private GridManager _gridManager;
        
        public AStarPathfinder(GridManager gridManager)
        {
            _gridManager = gridManager;
        }
        
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            // Simple A* implementation
            var openSet = new List<Vector2Int> { start };
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float>();
            var fScore = new Dictionary<Vector2Int, float>();
            
            gScore[start] = 0;
            fScore[start] = Heuristic(start, goal);
            
            while (openSet.Count > 0)
            {
                Vector2Int current = GetLowestFScore(openSet, fScore);
                
                if (current == goal)
                {
                    return ReconstructPath(cameFrom, current);
                }
                
                openSet.Remove(current);
                
                foreach (var neighbor in GetNeighbors(current))
                {
                    if (!_gridManager.IsValidCell(neighbor) || _gridManager.IsCellOccupied(neighbor))
                        continue;
                    
                    float tentativeGScore = gScore[current] + 1;
                    
                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, goal);
                        
                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }
            
            return new List<Vector2Int>(); // No path found
        }
        
        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
        
        private Vector2Int GetLowestFScore(List<Vector2Int> openSet, Dictionary<Vector2Int, float> fScore)
        {
            Vector2Int lowest = openSet[0];
            float lowestScore = fScore.ContainsKey(lowest) ? fScore[lowest] : float.MaxValue;
            
            foreach (var pos in openSet)
            {
                float score = fScore.ContainsKey(pos) ? fScore[pos] : float.MaxValue;
                if (score < lowestScore)
                {
                    lowest = pos;
                    lowestScore = score;
                }
            }
            
            return lowest;
        }
        
        private List<Vector2Int> GetNeighbors(Vector2Int position)
        {
            return new List<Vector2Int>
            {
                position + Vector2Int.up,
                position + Vector2Int.down,
                position + Vector2Int.left,
                position + Vector2Int.right
            };
        }
        
        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            
            return path;
        }
    }
}