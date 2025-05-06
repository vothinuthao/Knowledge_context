using System;
using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Events;
using VikingRaven.Units.Components;
using VikingRaven.Units.Systems;

namespace VikingRaven.Combat.Components
{
    public class TacticalDecisionSystem : BaseSystem
    {
        [SerializeField] private float _decisionInterval = 3.0f;
        
        private float _lastDecisionTime = 0f;
        private Dictionary<int, List<IEntity>> _squads = new Dictionary<int, List<IEntity>>();
        private Dictionary<int, SquadTacticalInfo> _squadTactics = new Dictionary<int, SquadTacticalInfo>();
        
        public override void Initialize()
        {
            // Find all squads
            IdentifySquads();
            
            // Subscribe to relevant events
            EventManager.Instance.RegisterListener<SquadCreatedEvent>(OnSquadCreated);
            EventManager.Instance.RegisterListener<DeathEvent>(OnUnitDeath);
        }
        
        public override void Execute()
        {
            // Make tactical decisions at intervals
            if (Time.time - _lastDecisionTime >= _decisionInterval)
            {
                UpdateTacticalSituation();
                MakeTacticalDecisions();
                AssignTacticalRoles();
                IssueObjectives();
                
                _lastDecisionTime = Time.time;
            }
        }
        
        private void IdentifySquads()
        {
            _squads.Clear();
            
            // Get all entities with formation components
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                
                if (formationComponent != null)
                {
                    int squadId = formationComponent.SquadId;
                    
                    if (!_squads.ContainsKey(squadId))
                    {
                        _squads[squadId] = new List<IEntity>();
                    }
                    
                    _squads[squadId].Add(entity);
                }
            }
            
            Debug.Log($"Identified {_squads.Count} squads");
        }
        
        private void UpdateTacticalSituation()
        {
            _squadTactics.Clear();
            
            // Analyze each squad's tactical situation
            foreach (var squadPair in _squads)
            {
                int squadId = squadPair.Key;
                List<IEntity> squadMembers = squadPair.Value;
                
                if (squadMembers.Count == 0)
                    continue;
                    
                SquadTacticalInfo tacticalInfo = new SquadTacticalInfo();
                
                // Calculate squad center and unit composition
                tacticalInfo.SquadCenter = CalculateSquadCenter(squadMembers);
                AnalyzeSquadComposition(squadMembers, ref tacticalInfo);
                
                // Analyze terrain at squad position
                AnalyzeTerrainAtPosition(tacticalInfo.SquadCenter, ref tacticalInfo);
                
                // Assess threats to squad
                AssessThreatsToSquad(squadMembers, ref tacticalInfo);
                
                // Store tactical info
                _squadTactics[squadId] = tacticalInfo;
            }
        }
        
        private Vector3 CalculateSquadCenter(List<IEntity> squadMembers)
        {
            if (squadMembers.Count == 0)
                return Vector3.zero;
                
            Vector3 sum = Vector3.zero;
            int count = 0;
            
            foreach (var member in squadMembers)
            {
                var transformComponent = member.GetComponent<TransformComponent>();
                
                if (transformComponent != null)
                {
                    sum += transformComponent.Position;
                    count++;
                }
            }
            
            return (count > 0) ? sum / count : Vector3.zero;
        }
        
        private void AnalyzeSquadComposition(List<IEntity> squadMembers, ref SquadTacticalInfo tacticalInfo)
        {
            tacticalInfo.UnitCount = squadMembers.Count;
            tacticalInfo.InfantryCount = 0;
            tacticalInfo.ArcherCount = 0;
            tacticalInfo.PikeCount = 0;
            
            // Count unit types
            foreach (var member in squadMembers)
            {
                var unitTypeComponent = member.GetComponent<UnitTypeComponent>();
                
                if (unitTypeComponent != null)
                {
                    switch (unitTypeComponent.UnitType)
                    {
                        case UnitType.Infantry:
                            tacticalInfo.InfantryCount++;
                            break;
                        case UnitType.Archer:
                            tacticalInfo.ArcherCount++;
                            break;
                        case UnitType.Pike:
                            tacticalInfo.PikeCount++;
                            break;
                    }
                }
                
                // Calculate average health
                var healthComponent = member.GetComponent<HealthComponent>();
                if (healthComponent != null && !healthComponent.IsDead)
                {
                    tacticalInfo.AverageHealth += healthComponent.HealthPercentage;
                }
            }
            
            if (tacticalInfo.UnitCount > 0)
            {
                tacticalInfo.AverageHealth /= tacticalInfo.UnitCount;
            }
            
            // Determine squad type based on composition
            if (tacticalInfo.ArcherCount > tacticalInfo.InfantryCount && tacticalInfo.ArcherCount > tacticalInfo.PikeCount)
            {
                tacticalInfo.PrimarySquadType = UnitType.Archer;
            }
            else if (tacticalInfo.PikeCount > tacticalInfo.InfantryCount)
            {
                tacticalInfo.PrimarySquadType = UnitType.Pike;
            }
            else
            {
                tacticalInfo.PrimarySquadType = UnitType.Infantry;
            }
        }
        
        private void AnalyzeTerrainAtPosition(Vector3 position, ref SquadTacticalInfo tacticalInfo)
        {
            // Get terrain info at the squad's position
            // In a real implementation, you would use TerrainAnalysisComponent data
            
            // For now, use simple raycasts to check height and surface type
            if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            {
                tacticalInfo.TerrainHeight = hit.point.y;
                
                // Check for terrain features
                if (hit.normal.y < 0.9f)
                {
                    tacticalInfo.IsSloped = true;
                    tacticalInfo.SlopeDirection = new Vector3(hit.normal.x, 0, hit.normal.z).normalized;
                }
                
                // Check for high ground
                bool isHighGround = CheckForHighGround(position, tacticalInfo.TerrainHeight);
                tacticalInfo.IsHighGround = isHighGround;
                
                // Check for choke points
                bool isChokePoint = CheckForChokePoint(position);
                tacticalInfo.IsChokePoint = isChokePoint;
            }
        }
        
        private bool CheckForHighGround(Vector3 position, float height)
        {
            // Simplified high ground check
            // Cast rays in multiple directions to see if surrounding terrain is lower
            
            int rayCount = 8;
            float rayDistance = 15f;
            int lowerCount = 0;
            
            for (int i = 0; i < rayCount; i++)
            {
                float angle = i * (2 * Mathf.PI / rayCount);
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 checkPosition = position + direction * rayDistance;
                
                if (Physics.Raycast(checkPosition + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f))
                {
                    if (hit.point.y < height - 1.5f) // Significant height advantage
                    {
                        lowerCount++;
                    }
                }
            }
            
            // If most surrounding terrain is lower, this is high ground
            return lowerCount >= rayCount / 2;
        }
        
        private bool CheckForChokePoint(Vector3 position)
        {
            // Simplified choke point check
            // Cast rays in multiple directions to see if there are obstacles forming a passage
            
            int rayCount = 16;
            float rayDistance = 10f;
            int hitCount = 0;
            
            for (int i = 0; i < rayCount; i++)
            {
                float angle = i * (2 * Mathf.PI / rayCount);
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                
                if (Physics.Raycast(position + Vector3.up, direction, rayDistance))
                {
                    hitCount++;
                }
            }
            
            // If there are obstacles in multiple directions but not all, it might be a choke point
            return hitCount > rayCount / 3 && hitCount < rayCount * 2 / 3;
        }
        
        private void AssessThreatsToSquad(List<IEntity> squadMembers, ref SquadTacticalInfo tacticalInfo)
        {
            // Get threat assessments from squad members
            foreach (var member in squadMembers)
            {
                var threatComponent = member.GetComponent<ThreatAssessmentComponent>();
                
                if (threatComponent != null)
                {
                    // Combine threat information
                    foreach (var threat in threatComponent.ThreatValues)
                    {
                        if (tacticalInfo.Threats.ContainsKey(threat.Key))
                        {
                            tacticalInfo.Threats[threat.Key] += threat.Value;
                        }
                        else
                        {
                            tacticalInfo.Threats[threat.Key] = threat.Value;
                        }
                    }
                }
            }
            
            // Calculate overall threat level and direction
            if (tacticalInfo.Threats.Count > 0)
            {
                float totalThreat = 0f;
                Vector3 threatDirection = Vector3.zero;
                
                foreach (var threat in tacticalInfo.Threats)
                {
                    totalThreat += threat.Value;
                    
                    var threatTransform = threat.Key.GetComponent<TransformComponent>();
                    if (threatTransform != null)
                    {
                        Vector3 direction = (threatTransform.Position - tacticalInfo.SquadCenter).normalized;
                        threatDirection += direction * threat.Value;
                    }
                }
                
                tacticalInfo.ThreatLevel = totalThreat;
                
                if (threatDirection.magnitude > 0.1f)
                {
                    tacticalInfo.PrimaryThreatDirection = threatDirection.normalized;
                }
            }
            
            // Identify primary threat
            IEntity primaryThreat = null;
            float highestThreat = 0f;
            
            foreach (var threat in tacticalInfo.Threats)
            {
                if (threat.Value > highestThreat)
                {
                    highestThreat = threat.Value;
                    primaryThreat = threat.Key;
                }
            }
            
            tacticalInfo.PrimaryThreat = primaryThreat;
        }
        
        private void MakeTacticalDecisions()
        {
            // Make tactical decisions for each squad
            foreach (var tacticalPair in _squadTactics)
            {
                int squadId = tacticalPair.Key;
                SquadTacticalInfo tacticalInfo = tacticalPair.Value;
                
                // Decide formation based on situation
                DecideFormation(squadId, tacticalInfo);
                
                // Decide behavior strategy
                DecideBehaviorStrategy(squadId, tacticalInfo);
                
                // Decide squad positioning
                DecideSquadPositioning(squadId, tacticalInfo);
            }
        }
        
        [Obsolete("Obsolete")]
        private void DecideFormation(int squadId, SquadTacticalInfo tacticalInfo)
        {
            FormationType bestFormation = FormationType.Line; // Default
            
            // Decision logic based on squad type and threats
            switch (tacticalInfo.PrimarySquadType)
            {
                case UnitType.Infantry:
                    if (tacticalInfo.ArcherCount > 0 && tacticalInfo.ThreatLevel > 10f)
                    {
                        // Protect archers with testudo against high threats
                        bestFormation = FormationType.Testudo;
                    }
                    else if (tacticalInfo.ThreatLevel > 5f)
                    {
                        // Line formation for moderate threats
                        bestFormation = FormationType.Line;
                    }
                    else
                    {
                        // Column for movement when threat is low
                        bestFormation = FormationType.Column;
                    }
                    break;
                    
                case UnitType.Pike:
                    if (tacticalInfo.IsChokePoint)
                    {
                        // Phalanx is effective in choke points
                        bestFormation = FormationType.Phalanx;
                    }
                    else if (tacticalInfo.PrimaryThreat != null)
                    {
                        // Line formation against enemies
                        bestFormation = FormationType.Line;
                    }
                    else
                    {
                        // Column for movement
                        bestFormation = FormationType.Column;
                    }
                    break;
                    
                case UnitType.Archer:
                    if (tacticalInfo.ThreatLevel > 8f)
                    {
                        // Circle formation for protection
                        bestFormation = FormationType.Circle;
                    }
                    else if (tacticalInfo.IsHighGround)
                    {
                        // Line formation on high ground
                        bestFormation = FormationType.Line;
                    }
                    else
                    {
                        // Column for movement
                        bestFormation = FormationType.Column;
                    }
                    break;
            }
            
            // Apply formation change through SquadCoordinationSystem
            var squadCoordinationSystem = GameObject.FindObjectOfType<SquadCoordinationSystem>();
            if (squadCoordinationSystem != null)
            {
                squadCoordinationSystem.SetSquadFormation(squadId, bestFormation);
                
                // Trigger formation changed event
                var formationEvent = new FormationChangedEvent(squadId, FormationType.None, bestFormation);
                EventManager.Instance.QueueEvent(formationEvent);
            }
        }
        
        private void DecideBehaviorStrategy(int squadId, SquadTacticalInfo tacticalInfo)
        {
            // Set behavior strategy based on situation
            SquadBehaviorStrategy strategy = SquadBehaviorStrategy.Neutral;
            
            // Based on health, threat, and position
            if (tacticalInfo.AverageHealth < 0.3f)
            {
                // Low health - retreat or defensive
                strategy = SquadBehaviorStrategy.Defensive;
            }
            else if (tacticalInfo.ThreatLevel > 15f)
            {
                // High threat - defensive
                strategy = SquadBehaviorStrategy.Defensive;
            }
            else if (tacticalInfo.ThreatLevel > 5f)
            {
                // Moderate threat - balanced approach
                strategy = SquadBehaviorStrategy.Balanced;
            }
            else if (tacticalInfo.IsHighGround)
            {
                // High ground - aggressive from advantage
                strategy = SquadBehaviorStrategy.Aggressive;
            }
            else
            {
                // Default - balanced
                strategy = SquadBehaviorStrategy.Balanced;
            }
            
            // Store decision
            tacticalInfo.CurrentStrategy = strategy;
        }
        
        private void DecideSquadPositioning(int squadId, SquadTacticalInfo tacticalInfo)
        {
            // Decide where the squad should position itself
            Vector3 idealPosition = tacticalInfo.SquadCenter; // Default to current position
            
            switch (tacticalInfo.CurrentStrategy)
            {
                case SquadBehaviorStrategy.Aggressive:
                    // Move toward primary threat
                    if (tacticalInfo.PrimaryThreat != null)
                    {
                        var threatTransform = tacticalInfo.PrimaryThreat.GetComponent<TransformComponent>();
                        if (threatTransform != null)
                        {
                            idealPosition = threatTransform.Position;
                        }
                    }
                    break;
                    
                case SquadBehaviorStrategy.Defensive:
                    // Move to high ground or away from threats
                    if (tacticalInfo.IsHighGround)
                    {
                        // Stay at current position if it's high ground
                        idealPosition = tacticalInfo.SquadCenter;
                    }
                    else if (tacticalInfo.PrimaryThreatDirection != Vector3.zero)
                    {
                        // Move away from threat
                        idealPosition = tacticalInfo.SquadCenter - tacticalInfo.PrimaryThreatDirection * 15f;
                    }
                    break;
                    
                case SquadBehaviorStrategy.Balanced:
                    // Find strategic position
                    if (tacticalInfo.IsChokePoint)
                    {
                        // Stay at choke point
                        idealPosition = tacticalInfo.SquadCenter;
                    }
                    else
                    {
                        // Find nearby strategic points
                        Vector3 strategicPoint = FindNearbyStrategicPoint(tacticalInfo.SquadCenter);
                        if (strategicPoint != Vector3.zero)
                        {
                            idealPosition = strategicPoint;
                        }
                    }
                    break;
            }
            
            // Store the decision
            tacticalInfo.IdealPosition = idealPosition;
        }
        
        private Vector3 FindNearbyStrategicPoint(Vector3 position)
        {
            // Find strategic points near the given position
            // In a real implementation, you would use data from TerrainAnalysisComponent
            
            // For now, return a simplified result
            // This would be replaced with actual strategic point finding logic
            
            // Sample points in a radius
            float radius = 20f;
            int sampleCount = 8;
            float bestScore = -1f;
            Vector3 bestPoint = Vector3.zero;
            
            for (int i = 0; i < sampleCount; i++)
            {
                float angle = i * (2 * Mathf.PI / sampleCount);
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 samplePoint = position + direction * radius;
                
                // Score the point
                float score = ScoreStrategicPoint(samplePoint);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoint = samplePoint;
                }
            }
            
            return (bestScore > 0) ? bestPoint : Vector3.zero;
        }
        
        private float ScoreStrategicPoint(Vector3 position)
        {
            float score = 0f;
            
            // Check height (high ground advantage)
            if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            {
                // Higher points are better
                score += hit.point.y * 0.1f;
                
                // Check if it's a high ground
                if (CheckForHighGround(position, hit.point.y))
                {
                    score += 2f;
                }
                
                // Check slope
                if (hit.normal.y < 0.9f)
                {
                    // Flat ground is generally better
                    score -= (1f - hit.normal.y) * 2f;
                }
            }
            
            // Check for cover
            score += CheckCoverValue(position);
            
            // Check visibility (open field of view)
            score += CheckVisibility(position);
            
            return score;
        }
        
        private float CheckCoverValue(Vector3 position)
        {
            // Simplified cover check
            int rayCount = 8;
            float rayDistance = 10f;
            int hitCount = 0;
            
            for (int i = 0; i < rayCount; i++)
            {
                float angle = i * (2 * Mathf.PI / rayCount);
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                
                if (Physics.Raycast(position + Vector3.up, direction, rayDistance))
                {
                    hitCount++;
                }
            }
            
            // Some cover is good, too much is bad
            return (hitCount > 0 && hitCount < rayCount / 2) ? 1.5f : 0f;
        }
        
        private float CheckVisibility(Vector3 position)
        {
            // Simplified visibility check
            int rayCount = 16;
            float rayDistance = 30f;
            int visibleCount = 0;
            
            for (int i = 0; i < rayCount; i++)
            {
                float angle = i * (2 * Mathf.PI / rayCount);
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                
                if (!Physics.Raycast(position + Vector3.up, direction, rayDistance))
                {
                    visibleCount++;
                }
            }
            
            // Good visibility is valuable
            return visibleCount * 0.1f;
        }
        
        private void AssignTacticalRoles()
        {
            // Assign roles to units within each squad
            foreach (var squadPair in _squads)
            {
                int squadId = squadPair.Key;
                List<IEntity> squadMembers = squadPair.Value;
                
                if (!_squadTactics.TryGetValue(squadId, out SquadTacticalInfo tacticalInfo))
                    continue;
                    
                // Sort squad members by type and position
                List<IEntity> infantryUnits = new List<IEntity>();
                List<IEntity> archerUnits = new List<IEntity>();
                List<IEntity> pikeUnits = new List<IEntity>();
                
                foreach (var member in squadMembers)
                {
                    var unitTypeComponent = member.GetComponent<UnitTypeComponent>();
                    
                    if (unitTypeComponent != null)
                    {
                        switch (unitTypeComponent.UnitType)
                        {
                            case UnitType.Infantry:
                                infantryUnits.Add(member);
                                break;
                            case UnitType.Archer:
                                archerUnits.Add(member);
                                break;
                            case UnitType.Pike:
                                pikeUnits.Add(member);
                                break;
                        }
                    }
                }
                
                // Assign roles based on unit type and strategy
                AssignInfantryRoles(infantryUnits, tacticalInfo);
                AssignArcherRoles(archerUnits, tacticalInfo);
                AssignPikeRoles(pikeUnits, tacticalInfo);
            }
        }
        
        private void AssignInfantryRoles(List<IEntity> infantryUnits, SquadTacticalInfo tacticalInfo)
        {
            int frontlineCount = Mathf.CeilToInt(infantryUnits.Count * 0.7f); // Most infantry are frontline
            
            for (int i = 0; i < infantryUnits.Count; i++)
            {
                var tactical = infantryUnits[i].GetComponent<TacticalComponent>();
                
                if (tactical != null)
                {
                    if (i < frontlineCount)
                    {
                        // Frontline fighters
                        tactical.AssignedRole = TacticalRole.Frontline;
                    }
                    else
                    {
                        // Support units protect others
                        tactical.AssignedRole = TacticalRole.Defender;
                    }
                }
            }
        }
        
        private void AssignArcherRoles(List<IEntity> archerUnits, SquadTacticalInfo tacticalInfo)
        {
            for (int i = 0; i < archerUnits.Count; i++)
            {
                var tactical = archerUnits[i].GetComponent<TacticalComponent>();
                
                if (tactical != null)
                {
                    // Archers are primarily support
                    tactical.AssignedRole = TacticalRole.Support;
                }
            }
        }
        
        private void AssignPikeRoles(List<IEntity> pikeUnits, SquadTacticalInfo tacticalInfo)
        {
            for (int i = 0; i < pikeUnits.Count; i++)
            {
                var tactical = pikeUnits[i].GetComponent<TacticalComponent>();
                
                if (tactical != null)
                {
                    if (tacticalInfo.CurrentStrategy == SquadBehaviorStrategy.Defensive)
                    {
                        // Pike units form defensive line
                        tactical.AssignedRole = TacticalRole.Defender;
                    }
                    else
                    {
                        // Pike units in offensive or balanced strategy are frontline
                        tactical.AssignedRole = TacticalRole.Frontline;
                    }
                }
            }
        }
        
        private void IssueObjectives()
        {
            // Issue tactical objectives to each unit based on their role
            foreach (var squadPair in _squads)
            {
                int squadId = squadPair.Key;
                List<IEntity> squadMembers = squadPair.Value;
                
                if (!_squadTactics.TryGetValue(squadId, out SquadTacticalInfo tacticalInfo))
                    continue;
                    
                // Set squad-level objective
                TacticalObjective squadObjective = DetermineSquadObjective(tacticalInfo);
                
                // Set individual unit objectives based on role and squad objective
                foreach (var member in squadMembers)
                {
                    var tacticalComponent = member.GetComponent<TacticalComponent>();
                    
                    if (tacticalComponent != null)
                    {
                        TacticalObjective unitObjective = squadObjective; // Default to squad objective
                        
                        // Adjust based on role
                        switch (tacticalComponent.AssignedRole)
                        {
                            case TacticalRole.Frontline:
                                // Frontline units attack or defend
                                if (squadObjective == TacticalObjective.Attack)
                                {
                                    unitObjective = TacticalObjective.Attack;
                                    tacticalComponent.ObjectiveTarget = tacticalInfo.PrimaryThreat;
                                }
                                else if (squadObjective == TacticalObjective.Defend)
                                {
                                    unitObjective = TacticalObjective.Defend;
                                    tacticalComponent.ObjectivePosition = tacticalInfo.SquadCenter;
                                }
                                else
                                {
                                    unitObjective = TacticalObjective.Move;
                                    tacticalComponent.ObjectivePosition = tacticalInfo.IdealPosition;
                                }
                                break;
                                
                            case TacticalRole.Support:
                                // Support units focus on positioning and ranged attacks
                                if (tacticalInfo.IsHighGround)
                                {
                                    unitObjective = TacticalObjective.Hold;
                                    tacticalComponent.ObjectivePosition = tacticalInfo.SquadCenter;
                                }
                                else
                                {
                                    unitObjective = TacticalObjective.Move;
                                    tacticalComponent.ObjectivePosition = tacticalInfo.IdealPosition;
                                }
                                break;
                                
                            case TacticalRole.Flanker:
                                // Flankers try to attack from different angles
                                if (tacticalInfo.PrimaryThreat != null)
                                {
                                    unitObjective = TacticalObjective.Attack;
                                    tacticalComponent.ObjectiveTarget = tacticalInfo.PrimaryThreat;
                                    
                                    // Calculate flanking position
                                    var threatTransform = tacticalInfo.PrimaryThreat.GetComponent<TransformComponent>();
                                    if (threatTransform != null)
                                    {
                                        Vector3 flankDirection = Vector3.Cross(Vector3.up, 
                                            tacticalInfo.PrimaryThreatDirection).normalized;
                                        tacticalComponent.ObjectivePosition = threatTransform.Position + flankDirection * 10f;
                                    }
                                }
                                break;
                                
                            case TacticalRole.Defender:
                                // Defenders protect the squad
                                unitObjective = TacticalObjective.Defend;
                                tacticalComponent.ObjectivePosition = tacticalInfo.SquadCenter;
                                break;
                                
                            case TacticalRole.Scout:
                                // Scouts explore ahead
                                unitObjective = TacticalObjective.Scout;
                                
                                if (tacticalInfo.PrimaryThreatDirection != Vector3.zero)
                                {
                                    tacticalComponent.ObjectivePosition = tacticalInfo.SquadCenter + 
                                        tacticalInfo.PrimaryThreatDirection * 20f;
                                }
                                else
                                {
                                    tacticalComponent.ObjectivePosition = tacticalInfo.IdealPosition;
                                }
                                break;
                        }
                        
                        // Set the objective
                        tacticalComponent.CurrentObjective = unitObjective;
                    }
                }
            }
        }
        
        private TacticalObjective DetermineSquadObjective(SquadTacticalInfo tacticalInfo)
        {
            // Determine the overall squad objective based on strategy and situation
            
            switch (tacticalInfo.CurrentStrategy)
            {
                case SquadBehaviorStrategy.Aggressive:
                    // Aggressive squads attack if there are threats
                    return (tacticalInfo.PrimaryThreat != null) ? 
                        TacticalObjective.Attack : TacticalObjective.Move;
                        
                case SquadBehaviorStrategy.Defensive:
                    // Defensive squads hold position or retreat
                    return (tacticalInfo.AverageHealth < 0.3f) ? 
                        TacticalObjective.Retreat : TacticalObjective.Defend;
                        
                case SquadBehaviorStrategy.Balanced:
                    // Balanced squads adapt to the situation
                    if (tacticalInfo.PrimaryThreat != null && tacticalInfo.ThreatLevel > 10f)
                    {
                        // High threat, defend
                        return TacticalObjective.Defend;
                    }
                    else if (tacticalInfo.PrimaryThreat != null)
                    {
                        // Moderate threat, attack
                        return TacticalObjective.Attack;
                    }
                    else
                    {
                        // No immediate threat, move to ideal position
                        return TacticalObjective.Move;
                    }
                    
                default:
                    return TacticalObjective.Move;
            }
        }
        
        private void OnSquadCreated(SquadCreatedEvent squadEvent)
        {
            // Add new squad to tracking
            int squadId = squadEvent.SquadId;
            
            if (!_squads.ContainsKey(squadId))
            {
                _squads[squadId] = new List<IEntity>();
            }
            
            foreach (var unit in squadEvent.Units)
            {
                if (!_squads[squadId].Contains(unit))
                {
                    _squads[squadId].Add(unit);
                }
            }
            
            Debug.Log($"Added new squad {squadId} with {squadEvent.Units.Count} units");
        }
        
        private void OnUnitDeath(DeathEvent deathEvent)
        {
            // Remove dead unit from squads
            IEntity deadUnit = deathEvent.Unit;
            
            foreach (var squadPair in _squads)
            {
                squadPair.Value.Remove(deadUnit);
            }
        }
    }
    
    public class SquadTacticalInfo
    {
        // Squad composition
        public int UnitCount;
        public int InfantryCount;
        public int ArcherCount;
        public int PikeCount;
        public UnitType PrimarySquadType;
        public float AverageHealth;
        
        // Position data
        public Vector3 SquadCenter;
        public Vector3 IdealPosition;
        
        // Terrain data
        public float TerrainHeight;
        public bool IsSloped;
        public Vector3 SlopeDirection;
        public bool IsHighGround;
        public bool IsChokePoint;
        
        // Threat assessment
        public Dictionary<IEntity, float> Threats = new Dictionary<IEntity, float>();
        public float ThreatLevel;
        public Vector3 PrimaryThreatDirection;
        public IEntity PrimaryThreat;
        
        // Current tactical decisions
        public SquadBehaviorStrategy CurrentStrategy;
    }
    
    public enum SquadBehaviorStrategy
    {
        Neutral,
        Aggressive,
        Defensive,
        Balanced
    }
}