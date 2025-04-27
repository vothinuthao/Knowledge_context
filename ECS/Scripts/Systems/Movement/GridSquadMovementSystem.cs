// File: Systems/Movement/GridSquadMovementSystem.cs
using UnityEngine;
using Core.ECS;
using Components.Squad;
using Core.Grid;
using Components;

namespace Systems.Movement
{
    /// <summary>
    /// Handles squad movement on grid
    /// </summary>
    public class GridSquadMovementSystem : ISystem
    {
        private World _world;
        private GridManager _gridManager;
        private AStarPathfinder _pathfinder;
        
        public int Priority => 100;
        
        public void Initialize(World world)
        {
            _world = world;
            // _gridManager = GridManager.Instance;
            // _pathfinder = new AStarPathfinder(GridManager.Instance);
        }
        
        public void Update(float deltaTime)
        {
            // Process all squads
            foreach (var entity in _world.GetEntitiesWith<SquadComponent, PositionComponent>())
            {
                var squadComponent = entity.GetComponent<SquadComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                switch (squadComponent.State)
                {
                    case SquadState.IDLE:
                        HandleIdleSquad(entity, squadComponent, positionComponent);
                        break;
                    
                    case SquadState.MOVING:
                        HandleMovingSquad(entity, squadComponent, positionComponent, deltaTime);
                        break;
                    
                    case SquadState.ATTACKING:
                        HandleCombatSquad(entity, squadComponent, positionComponent);
                        break;
                }
                
                // Update formation if needed
                if (squadComponent.NeedsFormationUpdate())
                {
                    squadComponent.UpdateFormation();
                }
            }
        }
        
        private void HandleIdleSquad(Entity entity, SquadComponent squad, PositionComponent position)
        {
            // Ensure squad is centered in current cell
            Vector3 cellCenter = GridManager.Instance.GetCellCenter(squad.GridPosition);
            if (Vector3.Distance(position.Position, cellCenter) > 0.1f)
            {
                position.Position = Vector3.Lerp(position.Position, cellCenter, Time.deltaTime * 5.0f);
            }
            
            // Lock troops in formation
            UpdateTroopFormation(entity, squad, position, true);
        }
        
        private void HandleMovingSquad(Entity entity, SquadComponent squad, PositionComponent position, float deltaTime)
        {
            // Check if path needs calculation
            if (squad.CurrentPath.Count == 0 && squad.GridPosition != squad.TargetGridPosition)
            {
                _pathfinder ??= new AStarPathfinder(GridManager.Instance);
                squad.CurrentPath = _pathfinder.FindPath(squad.GridPosition, squad.TargetGridPosition);
                squad.PathIndex = 0;
            }
            
            // Follow path
            if (squad.CurrentPath.Count > 0 && squad.PathIndex < squad.CurrentPath.Count)
            {
                Vector2Int nextCell = squad.CurrentPath[squad.PathIndex];
                Vector3 targetPosition = GridManager.Instance.GetCellCenter(nextCell);
                
                // Move toward next cell
                Vector3 direction = (targetPosition - position.Position).normalized;
                position.Position += direction * squad.MovementSpeed * deltaTime;
                
                // Check if reached target cell
                if (Vector3.Distance(position.Position, targetPosition) < 0.1f)
                {
                    // Update grid position
                    GridManager.Instance.SetCellOccupied(squad.GridPosition, false);
                    squad.GridPosition = nextCell;
                    GridManager.Instance.SetCellOccupied(squad.GridPosition, true);
                    
                    // Move to next cell in path
                    squad.PathIndex++;
                    
                    // Check if reached final destination
                    if (squad.PathIndex >= squad.CurrentPath.Count)
                    {
                        squad.State = SquadState.IDLE;
                        squad.CurrentPath.Clear();
                    }
                }
                
                // Update rotation to face movement direction
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    squad.FormationRotation = Quaternion.Slerp(squad.FormationRotation, 
                                                             targetRotation, 
                                                             deltaTime * squad.RotationSpeed);
                }
            }
            
            // Update troop positions
            UpdateTroopFormation(entity, squad, position, false);
        }
        
        private void HandleCombatSquad(Entity entity, SquadComponent squad, PositionComponent position)
        {
            // Check if target squad still exists and is in range
            Entity targetSquad = _world.GetEntityById(squad.TargetSquadId);
            
            if (targetSquad == null || !targetSquad.HasComponent<PositionComponent>())
            {
                // Target lost, return to idle
                squad.State = SquadState.IDLE;
                squad.TargetSquadId = -1;
                return;
            }
            
            var targetPosition = targetSquad.GetComponent<PositionComponent>().Position;
            float distance = Vector3.Distance(position.Position, targetPosition);
            
            // Face target
            Vector3 toTarget = (targetPosition - position.Position).normalized;
            if (toTarget != Vector3.zero)
            {
                squad.FormationRotation = Quaternion.LookRotation(toTarget);
            }
            
            // Adjust position if needed
            if (distance > squad.CombatRange * 1.5f)
            {
                // Move closer
                Vector2Int targetCell = GridManager.Instance.GetGridCoordinates(targetPosition);
                if (targetCell != squad.TargetGridPosition)
                {
                    squad.TargetGridPosition = targetCell;
                    squad.State = SquadState.MOVING;
                }
            }
            
            // Update troop positions for combat
            UpdateTroopFormation(entity, squad, position, false);
        }
        
        private void UpdateTroopFormation(Entity squadEntity, SquadComponent squad, 
                                        PositionComponent squadPosition, bool lockPosition)
        {
            for (int i = 0; i < squad.MemberIds.Count; i++)
            {
                Entity memberEntity = _world.GetEntityById(squad.MemberIds[i]);
                if (memberEntity == null || !memberEntity.HasComponent<PositionComponent>())
                    continue;
                
                var memberPosition = memberEntity.GetComponent<PositionComponent>();
                
                // Calculate desired position
                Vector3 offset = squad.FormationRotation * squad.GetMemberOffset(i);
                Vector3 desiredPosition = squadPosition.Position + offset;
                
                if (lockPosition)
                {
                    // Snap to position
                    memberPosition.Position = desiredPosition;
                }
                else
                {
                    // Smooth movement
                    memberPosition.Position = Vector3.Lerp(memberPosition.Position, 
                                                         desiredPosition, 
                                                         Time.deltaTime * 10.0f);
                }
            }
        }
        
        /// <summary>
        /// Command squad to move to a new position
        /// </summary>
        public void CommandMove(Entity squadEntity, Vector2Int targetCell)
        {
            var squad = squadEntity.GetComponent<SquadComponent>();
            if (squad == null) return;
            
            // Check if target cell is valid and not occupied
            if (!GridManager.Instance.IsValidCell(targetCell) || GridManager.Instance.IsCellOccupied(targetCell))
                return;
            
            squad.TargetGridPosition = targetCell;
            squad.State = SquadState.MOVING;
            squad.CurrentPath.Clear();
            squad.PathIndex = 0;
        }
        
        /// <summary>
        /// Command squad to attack target
        /// </summary>
        public void CommandAttack(Entity squadEntity, Entity targetEntity)
        {
            var squad = squadEntity.GetComponent<SquadComponent>();
            if (squad == null) return;
            
            squad.TargetSquadId = targetEntity.Id;
            squad.State = SquadState.ATTACKING;
        }
        
        /// <summary>
        /// Command squad to stop
        /// </summary>
        public void CommandStop(Entity squadEntity)
        {
            var squad = squadEntity.GetComponent<SquadComponent>();
            if (squad == null) return;
            
            squad.State = SquadState.IDLE;
            squad.CurrentPath.Clear();
            squad.TargetSquadId = -1;
        }
    }
}