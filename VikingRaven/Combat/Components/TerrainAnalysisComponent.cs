using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Combat.Components
{
    public class TerrainAnalysisComponent : MonoBehaviour, IComponent
    {
        [SerializeField] private bool _isActive = true;
        [SerializeField] private float _analysisRadius = 20f;
        [SerializeField] private float _analysisInterval = 2f;
        [SerializeField] private LayerMask _obstacleLayer;
        
        private IEntity _entity;
        private Dictionary<Vector3, TerrainCell> _terrainAnalysis = new Dictionary<Vector3, TerrainCell>();
        private List<Vector3> _strategicPoints = new List<Vector3>();
        private float _lastAnalysisTime = 0f;
        
        public bool IsActive { get => _isActive; set => _isActive = value; }
        public IEntity Entity { get => _entity; set => _entity = value; }
        
        public float AnalysisRadius
        {
            get => _analysisRadius;
            set => _analysisRadius = value;
        }
        
        public IReadOnlyList<Vector3> StrategicPoints => _strategicPoints;
        
        public void Initialize()
        {
            _lastAnalysisTime = -_analysisInterval; // Force initial analysis
        }
        
        public void Update()
        {
            if (!IsActive)
                return;
                
            // Analyze terrain at intervals
            if (Time.time - _lastAnalysisTime >= _analysisInterval)
            {
                AnalyzeTerrain();
                _lastAnalysisTime = Time.time;
            }
        }
        
        public void AnalyzeTerrain()
        {
            // Clear old data
            _terrainAnalysis.Clear();
            _strategicPoints.Clear();
            
            // Get entity position
            var transformComponent = Entity.GetComponent<Units.Components.TransformComponent>();
            if (transformComponent == null)
                return;
                
            Vector3 center = transformComponent.Position;
            
            // Simple terrain analysis - sample points in a grid around the entity
            float cellSize = 5f; // Size of terrain analysis cell
            int gridSize = Mathf.CeilToInt(_analysisRadius / cellSize);
            
            for (int x = -gridSize; x <= gridSize; x++)
            {
                for (int z = -gridSize; z <= gridSize; z++)
                {
                    Vector3 cellCenter = center + new Vector3(x * cellSize, 0, z * cellSize);
                    AnalyzeTerrainCell(cellCenter, cellSize);
                }
            }
            
            // Find strategic points
            FindStrategicPoints();
        }
        
        private void AnalyzeTerrainCell(Vector3 cellCenter, float cellSize)
        {
            TerrainCell cell = new TerrainCell();
            
            // Check for obstacles
            int obstacleCount = 0;
            float sampleSize = cellSize / 2f;
            
            // Sample multiple points within the cell
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3 samplePoint = cellCenter + new Vector3(x * sampleSize, 0, z * sampleSize);
                    
                    // Raycast down to find ground height
                    if (Physics.Raycast(samplePoint + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
                    {
                        // Record ground height
                        cell.AverageHeight += hit.point.y;
                        
                        // Check if this is an obstacle
                        if (_obstacleLayer == (_obstacleLayer | (1 << hit.collider.gameObject.layer)))
                        {
                            obstacleCount++;
                        }
                    }
                }
            }
            
            // Calculate average height and determine if cell is obstructed
            int sampleCount = 9; // 3x3 samples
            cell.AverageHeight /= sampleCount;
            cell.IsObstructed = obstacleCount > sampleCount / 2;
            
            // Calculate slope
            cell.Slope = CalculateCellSlope(cellCenter, cellSize);
            
            // Calculate cover value
            cell.CoverValue = CalculateCellCoverValue(cellCenter);
            
            // Store cell data
            _terrainAnalysis[cellCenter] = cell;
        }
        
        private float CalculateCellSlope(Vector3 cellCenter, float cellSize)
        {
            // Simplified slope calculation
            float maxSlope = 0f;
            
            // Cast rays from corners and calculate slope
            Vector3[] corners = new Vector3[4]
            {
                cellCenter + new Vector3(-cellSize/2, 0, -cellSize/2),
                cellCenter + new Vector3(cellSize/2, 0, -cellSize/2),
                cellCenter + new Vector3(-cellSize/2, 0, cellSize/2),
                cellCenter + new Vector3(cellSize/2, 0, cellSize/2)
            };
            
            float[] heights = new float[4];
            
            for (int i = 0; i < 4; i++)
            {
                if (Physics.Raycast(corners[i] + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
                {
                    heights[i] = hit.point.y;
                }
            }
            
            // Calculate slopes between adjacent corners
            for (int i = 0; i < 4; i++)
            {
                int nextIdx = (i + 1) % 4;
                float distance = Vector3.Distance(new Vector3(corners[i].x, 0, corners[i].z), 
                                                 new Vector3(corners[nextIdx].x, 0, corners[nextIdx].z));
                float heightDiff = Mathf.Abs(heights[i] - heights[nextIdx]);
                float slope = heightDiff / distance;
                
                maxSlope = Mathf.Max(maxSlope, slope);
            }
            
            return maxSlope;
        }
        
        private float CalculateCellCoverValue(Vector3 cellCenter)
        {
            // Simplified cover value calculation
            float coverValue = 0f;
            
            // Cast rays in multiple directions to check for obstacles that provide cover
            int rayCount = 8;
            float rayDistance = 10f;
            
            for (int i = 0; i < rayCount; i++)
            {
                float angle = i * (2 * Mathf.PI / rayCount);
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                
                if (Physics.Raycast(cellCenter + Vector3.up, direction, out RaycastHit hit, rayDistance, _obstacleLayer))
                {
                    // Add cover value based on distance (closer obstacles provide better cover)
                    coverValue += 1f - (hit.distance / rayDistance);
                }
            }
            
            // Normalize cover value
            coverValue /= rayCount;
            
            return coverValue;
        }
        
        private void FindStrategicPoints()
        {
            // Find strategic points based on terrain analysis
            foreach (var pair in _terrainAnalysis)
            {
                Vector3 position = pair.Key;
                TerrainCell cell = pair.Value;
                
                // Check if cell is a potential strategic point
                if (!cell.IsObstructed && IsStrategicPoint(position, cell))
                {
                    _strategicPoints.Add(position);
                }
            }
        }
        
        private bool IsStrategicPoint(Vector3 position, TerrainCell cell)
        {
            // Simplified strategic point detection
            
            // High ground advantage
            bool isHighGround = IsHighGroundAdvantage(position, cell);
            
            // Choke point detection
            bool isChokePoint = IsChokePoint(position);
            
            // Good cover position
            bool isGoodCover = cell.CoverValue > 0.6f;
            
            return isHighGround || isChokePoint || isGoodCover;
        }
        
        private bool IsHighGroundAdvantage(Vector3 position, TerrainCell cell)
        {
            // Check surrounding cells to see if this cell is higher
            float heightAdvantage = 0f;
            int higherCount = 0;
            
            foreach (var pair in _terrainAnalysis)
            {
                Vector3 otherPos = pair.Key;
                TerrainCell otherCell = pair.Value;
                
                // Only check nearby cells
                if (Vector3.Distance(position, otherPos) <= 10f && position != otherPos)
                {
                    float heightDiff = cell.AverageHeight - otherCell.AverageHeight;
                    heightAdvantage += heightDiff;
                    
                    if (heightDiff > 1f) // Significant height advantage
                    {
                        higherCount++;
                    }
                }
            }
            
            // If it's higher than most surrounding cells, it's high ground
            return higherCount >= 5 || heightAdvantage > 10f;
        }
        
        private bool IsChokePoint(Vector3 position)
        {
            // Simplified choke point detection
            // Count obstructed vs non-obstructed cells around this position
            int obstructedCount = 0;
            int totalCount = 0;
            
            foreach (var pair in _terrainAnalysis)
            {
                Vector3 otherPos = pair.Key;
                TerrainCell otherCell = pair.Value;
                
                // Only check nearby cells
                if (Vector3.Distance(position, otherPos) <= 10f)
                {
                    totalCount++;
                    
                    if (otherCell.IsObstructed)
                    {
                        obstructedCount++;
                    }
                }
            }
            
            // If there are many obstructions but this cell is clear, it might be a choke point
            float obstructionRatio = (float)obstructedCount / totalCount;
            return !_terrainAnalysis[position].IsObstructed && obstructionRatio > 0.5f;
        }
        
        public struct TerrainCell
        {
            public float AverageHeight;
            public bool IsObstructed;
            public float Slope;
            public float CoverValue;
        }
        
        public void Cleanup() { }
    }
}