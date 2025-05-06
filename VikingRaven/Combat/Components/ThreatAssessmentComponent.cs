using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Combat.Components
{
    public class ThreatAssessmentComponent : MonoBehaviour, IComponent
    {
        [SerializeField] private bool _isActive = true;
        [SerializeField] private float _assessmentRadius = 30f;
        [SerializeField] private float _assessmentInterval = 1f;
        
        private IEntity _entity;
        private Dictionary<IEntity, float> _threatValues = new Dictionary<IEntity, float>();
        private Dictionary<Vector3, float> _dangerZones = new Dictionary<Vector3, float>();
        private float _lastAssessmentTime = 0f;
        
        public bool IsActive { get => _isActive; set => _isActive = value; }
        public IEntity Entity { get => _entity; set => _entity = value; }
        
        public IReadOnlyDictionary<IEntity, float> ThreatValues => _threatValues;
        public IReadOnlyDictionary<Vector3, float> DangerZones => _dangerZones;
        
        public void Initialize()
        {
            _lastAssessmentTime = -_assessmentInterval; // Force initial assessment
        }
        
        public void Update()
        {
            if (!IsActive)
                return;
                
            // Assess threats at intervals
            if (Time.time - _lastAssessmentTime >= _assessmentInterval)
            {
                AssessThreats();
                _lastAssessmentTime = Time.time;
            }
        }
        
        public void AssessThreats()
        {
            // Clear old data
            _threatValues.Clear();
            _dangerZones.Clear();
            
            // Get all entities that could pose a threat
            var entityRegistry = GameObject.FindObjectOfType<Core.ECS.EntityRegistry>();
            if (entityRegistry == null)
                return;
                
            var potentialThreats = entityRegistry.GetEntitiesWithComponent<CombatComponent>();
            
            // Get entity position
            var transformComponent = Entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                return;
                
            Vector3 position = transformComponent.Position;
            
            // Assess each potential threat
            foreach (var threat in potentialThreats)
            {
                // Skip self
                if (threat == Entity)
                    continue;
                    
                // Check if this is an enemy (simplified faction check)
                bool isEnemy = true; // This would use a proper faction system in a real game
                
                if (isEnemy)
                {
                    float threatValue = CalculateThreatValue(threat, position);
                    
                    if (threatValue > 0)
                    {
                        _threatValues[threat] = threatValue;
                        
                        // Add to danger zones
                        var threatTransform = threat.GetComponent<TransformComponent>();
                        if (threatTransform != null)
                        {
                            AddToDangerZones(threatTransform.Position, threatValue);
                        }
                    }
                }
            }
        }
        
        private float CalculateThreatValue(IEntity threat, Vector3 position)
        {
            float threatValue = 0f;
            
            // Get threat components
            var threatTransform = threat.GetComponent<TransformComponent>();
            var threatCombat = threat.GetComponent<CombatComponent>();
            var threatHealth = threat.GetComponent<HealthComponent>();
            
            if (threatTransform == null || threatCombat == null)
                return 0f;
                
            // Calculate distance
            float distance = Vector3.Distance(position, threatTransform.Position);
            
            // If outside assessment radius, ignore
            if (distance > _assessmentRadius)
                return 0f;
                
            // Base threat on damage capability and distance
            threatValue = threatCombat.AttackDamage * (1f - distance / _assessmentRadius);
            
            // Adjust based on health if available
            if (threatHealth != null && !threatHealth.IsDead)
            {
                threatValue *= threatHealth.HealthPercentage;
            }
            else if (threatHealth != null && threatHealth.IsDead)
            {
                return 0f; // Dead threats pose no danger
            }
            
            // Account for unit type (simplified)
            var threatUnitType = threat.GetComponent<UnitTypeComponent>();
            if (threatUnitType != null)
            {
                switch (threatUnitType.UnitType)
                {
                    case UnitType.Archer:
                        // Archers are more dangerous at range
                        threatValue *= 1f + (distance / _assessmentRadius);
                        break;
                    case UnitType.Pike:
                        // Pikes are more dangerous in formation
                        var formationComp = threat.GetComponent<FormationComponent>();
                        if (formationComp != null && 
                            (formationComp.CurrentFormationType == FormationType.Phalanx || 
                             formationComp.CurrentFormationType == FormationType.Line))
                        {
                            threatValue *= 1.5f;
                        }
                        break;
                }
            }
            
            return threatValue;
        }
        
        private void AddToDangerZones(Vector3 position, float threatValue)
        {
            // Use a grid-based approach for danger zones
            float cellSize = 5f;
            Vector3 cellCenter = new Vector3(
                Mathf.Round(position.x / cellSize) * cellSize,
                0,
                Mathf.Round(position.z / cellSize) * cellSize
            );
            
            // Add threat to cell and surrounding cells with falloff
            float maxRadius = 15f; // Maximum influence radius
            float falloffFactor = 0.5f; // How quickly threat diminishes with distance
            
            for (int x = -3; x <= 3; x++)
            {
                for (int z = -3; z <= 3; z++)
                {
                    Vector3 currentCell = cellCenter + new Vector3(x * cellSize, 0, z * cellSize);
                    float distanceToThreat = Vector3.Distance(currentCell, position);
                    
                    if (distanceToThreat <= maxRadius)
                    {
                        // Calculate falloff based on distance
                        float falloff = 1f - Mathf.Clamp01(distanceToThreat / maxRadius) * falloffFactor;
                        float cellThreat = threatValue * falloff;
                        
                        // Add to danger zones
                        if (_dangerZones.ContainsKey(currentCell))
                        {
                            _dangerZones[currentCell] += cellThreat;
                        }
                        else
                        {
                            _dangerZones[currentCell] = cellThreat;
                        }
                    }
                }
            }
        }
        
        public float GetThreatValueAtPosition(Vector3 position)
        {
            // Convert position to cell
            float cellSize = 5f;
            Vector3 cellCenter = new Vector3(
                Mathf.Round(position.x / cellSize) * cellSize,
                0,
                Mathf.Round(position.z / cellSize) * cellSize
            );
            
            // Check if there's a danger value for this cell
            if (_dangerZones.TryGetValue(cellCenter, out float threatValue))
            {
                return threatValue;
            }
            
            return 0f;
        }
        
        public IEntity GetHighestThreat()
        {
            float highestThreat = 0f;
            IEntity highestThreatEntity = null;
            
            foreach (var pair in _threatValues)
            {
                if (pair.Value > highestThreat)
                {
                    highestThreat = pair.Value;
                    highestThreatEntity = pair.Key;
                }
            }
            
            return highestThreatEntity;
        }
        
        public Vector3 GetSafestDirection(float distance = 10f)
        {
            // Get entity position
            var transformComponent = Entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                return Vector3.zero;
                
            Vector3 position = transformComponent.Position;
            
            // Check threat values in different directions
            int directionCount = 8;
            float lowestThreat = float.MaxValue;
            Vector3 safestDirection = Vector3.zero;
            
            for (int i = 0; i < directionCount; i++)
            {
                float angle = i * (2 * Mathf.PI / directionCount);
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).normalized;
                Vector3 checkPosition = position + direction * distance;
                
                float threatAtPosition = GetThreatValueAtPosition(checkPosition);
                
                if (threatAtPosition < lowestThreat)
                {
                    lowestThreat = threatAtPosition;
                    safestDirection = direction;
                }
            }
            
            return safestDirection;
        }
        
        public void Cleanup() { }
    }
}