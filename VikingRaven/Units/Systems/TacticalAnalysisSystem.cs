using System.Collections.Generic;
using UnityEngine;
using Zenject;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class TacticalAnalysisSystem : BaseSystem
    {
        [Inject] private SquadCoordinationSystem _squadCoordinationSystem;
        
        // Tactical parameters
        private float _combatEvaluationInterval = 2.0f;
        private float _lastEvaluationTime = 0f;
        
        public override void Execute()
        {
            // Only evaluate tactics periodically to save performance
            if (Time.time - _lastEvaluationTime < _combatEvaluationInterval)
                return;
                
            _lastEvaluationTime = Time.time;
            
            // Get all entities with combat and unit type components
            var entities = EntityRegistry.GetEntitiesWithComponent<CombatComponent>();
            
            // Group entities by squad
            Dictionary<int, List<IEntity>> squadEntities = new Dictionary<int, List<IEntity>>();
            Dictionary<int, List<IEntity>> enemyEntitiesBySquad = new Dictionary<int, List<IEntity>>();
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                
                if (formationComponent != null)
                {
                    int squadId = formationComponent.SquadId;
                    
                    // Add to squad entities
                    if (!squadEntities.ContainsKey(squadId))
                    {
                        squadEntities[squadId] = new List<IEntity>();
                    }
                    
                    squadEntities[squadId].Add(entity);
                    
                    // Find enemies for this squad
                    var aggroDetectionComponent = entity.GetComponent<AggroDetectionComponent>();
                    if (aggroDetectionComponent != null && aggroDetectionComponent.HasEnemyInRange())
                    {
                        if (!enemyEntitiesBySquad.ContainsKey(squadId))
                        {
                            enemyEntitiesBySquad[squadId] = new List<IEntity>();
                        }
                        
                        // Add closest enemy if not already in the list
                        var enemy = aggroDetectionComponent.GetClosestEnemy();
                        if (enemy != null && !enemyEntitiesBySquad[squadId].Contains(enemy))
                        {
                            enemyEntitiesBySquad[squadId].Add(enemy);
                        }
                    }
                }
            }
            
            // Analyze tactics for each squad
            foreach (var squadId in squadEntities.Keys)
            {
                AnalyzeSquadTactics(squadId, squadEntities[squadId], 
                    enemyEntitiesBySquad.ContainsKey(squadId) ? enemyEntitiesBySquad[squadId] : new List<IEntity>());
            }
        }

        private void AnalyzeSquadTactics(int squadId, List<IEntity> squadEntities, List<IEntity> enemies)
        {
            if (squadEntities.Count == 0)
                return;
                
            // Count unit types in the squad
            int infantryCount = 0;
            int archerCount = 0;
            int pikeCount = 0;
            
            foreach (var entity in squadEntities)
            {
                var unitTypeComponent = entity.GetComponent<UnitTypeComponent>();
                if (unitTypeComponent != null)
                {
                    switch (unitTypeComponent.UnitType)
                    {
                        case UnitType.Infantry:
                            infantryCount++;
                            break;
                        case UnitType.Archer:
                            archerCount++;
                            break;
                        case UnitType.Pike:
                            pikeCount++;
                            break;
                    }
                }
            }
            
            // Determine best formation based on unit composition and enemies
            FormationType bestFormation = FormationType.Line; // Default
            
            if (enemies.Count > 0)
            {
                // Count enemy unit types
                int enemyInfantryCount = 0;
                int enemyArcherCount = 0;
                int enemyPikeCount = 0;
                
                foreach (var enemy in enemies)
                {
                    var unitTypeComponent = enemy.GetComponent<UnitTypeComponent>();
                    if (unitTypeComponent != null)
                    {
                        switch (unitTypeComponent.UnitType)
                        {
                            case UnitType.Infantry:
                                enemyInfantryCount++;
                                break;
                            case UnitType.Archer:
                                enemyArcherCount++;
                                break;
                            case UnitType.Pike:
                                enemyPikeCount++;
                                break;
                        }
                    }
                }
                
                // Tactical decisions
                if (enemyArcherCount > enemyInfantryCount && enemyArcherCount > enemyPikeCount)
                {
                    // Against archers, use Testudo formation
                    bestFormation = FormationType.Testudo;
                }
                else if (enemyPikeCount > enemyInfantryCount)
                {
                    // Against pikes, use Circle formation to surround
                    bestFormation = FormationType.Circle;
                }
                else
                {
                    // Against infantry, use Phalanx if we have pikes
                    if (pikeCount > infantryCount)
                    {
                        bestFormation = FormationType.Phalanx;
                    }
                    else
                    {
                        // Otherwise use Line formation
                        bestFormation = FormationType.Line;
                    }
                }
            }
            else
            {
                // No enemies, use Column for movement
                bestFormation = FormationType.Column;
            }
            
            // Update squad formation through the coordination system
            if (_squadCoordinationSystem != null)
            {
                _squadCoordinationSystem.SetSquadFormation(squadId, bestFormation);
            }
        }
    }
}