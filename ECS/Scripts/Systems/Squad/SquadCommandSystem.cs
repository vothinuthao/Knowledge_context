// ECS/Scripts/Systems/Squad/SquadCommandSystem.cs
using Core.ECS;
using Movement;
using Squad;
using Steering;
using UnityEngine;
using System.Collections.Generic;

namespace Systems.Squad
{
    /// <summary>
    /// System that handles squad commands and state changes
    /// </summary>
    public class SquadCommandSystem : ISystem
    {
        private World _world;
        
        public int Priority => 120; // Very high priority
        
        // FIX: Dictionary để lưu trữ các target positions của squad
        private Dictionary<int, Vector3> _squadTargets = new Dictionary<int, Vector3>();
        
        // FIX: Dictionary để lưu thời gian delay cập nhật để tránh cập nhật liên tục
        private Dictionary<int, float> _squadUpdateTimers = new Dictionary<int, float>();
        private const float UPDATE_INTERVAL = 0.2f; // Cập nhật mỗi 0.2 giây
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            // Process all squads with state components
            foreach (var squadEntity in _world.GetEntitiesWith<SquadStateComponent, PositionComponent>())
            {
                var stateComponent = squadEntity.GetComponent<SquadStateComponent>();
                var positionComponent = squadEntity.GetComponent<PositionComponent>();
                
                // FIX: Cập nhật timer cho squad
                int squadId = squadEntity.Id;
                if (!_squadUpdateTimers.ContainsKey(squadId))
                {
                    _squadUpdateTimers[squadId] = 0f;
                }
                _squadUpdateTimers[squadId] += deltaTime;
                
                switch (stateComponent.CurrentState)
                {
                    case SquadState.Moving:
                        UpdateMovingSquad(squadEntity, stateComponent, positionComponent, deltaTime);
                        break;
                        
                    case SquadState.Attacking:
                        UpdateAttackingSquad(squadEntity, stateComponent, deltaTime);
                        break;
                        
                    case SquadState.Defending:
                        UpdateDefendingSquad(squadEntity, stateComponent, deltaTime);
                        break;
                        
                    case SquadState.Idle:
                    default:
                        // Idle squads should have their members locked in formation
                        if (_squadUpdateTimers[squadId] >= UPDATE_INTERVAL)
                        {
                            _squadUpdateTimers[squadId] = 0f;
                            UpdateSquadMembersFormation(squadEntity);
                        }
                        break;
                }
            }
        }
        
        /// <summary>
        /// Update a squad in the moving state
        /// </summary>
        private void UpdateMovingSquad(Entity squadEntity, SquadStateComponent stateComponent, 
            PositionComponent positionComponent, float deltaTime)
        {
            // Check if squad has reached destination
            float distanceToTarget = Vector3.Distance(positionComponent.Position, stateComponent.TargetPosition);
            
            // FIX: Lưu target vị trí trong dictionary
            _squadTargets[squadEntity.Id] = stateComponent.TargetPosition;
            
            int squadId = squadEntity.Id;
            
            if (distanceToTarget < 1.0f)
            {
                // Reached destination, transition to idle
                stateComponent.CurrentState = SquadState.Idle;
                
                // FIX: Xóa mục tiêu khi đến nơi
                if (_squadTargets.ContainsKey(squadId))
                {
                    _squadTargets.Remove(squadId);
                }
                
                // FIX: Reset các troop về vị trí trong formation
                UpdateSquadMembersFormation(squadEntity);
                
                Debug.Log($"Squad {squadEntity.Id} reached destination and is now idle");
            }
            else
            {
                // Move squad towards target
                Vector3 direction = (stateComponent.TargetPosition - positionComponent.Position).normalized;
                
                // Move squad with appropriate velocity
                if (squadEntity.HasComponent<VelocityComponent>())
                {
                    var velocityComponent = squadEntity.GetComponent<VelocityComponent>();
                    float effectiveSpeed = velocityComponent.GetEffectiveMaxSpeed();
                    
                    // FIX: Áp dụng tốc độ giảm dần khi gần đến mục tiêu
                    if (distanceToTarget < 3.0f)
                    {
                        effectiveSpeed *= distanceToTarget / 3.0f;
                    }
                    
                    // Set squad velocity
                    velocityComponent.Velocity = direction * effectiveSpeed;
                    
                    // FIX: Update troop positions at intervals to prevent constant target updating
                    if (_squadUpdateTimers[squadId] >= UPDATE_INTERVAL)
                    {
                        _squadUpdateTimers[squadId] = 0f;
                        UpdateSquadMembersTargets(squadEntity);
                    }
                }
            }
        }
        
        /// <summary>
        /// Update target positions for all squad members
        /// </summary>
        private void UpdateSquadMembersTargets(Entity squadEntity)
        {
            // Lấy ID của squad để trace log
            int squadId = squadEntity.Id;
            
            // FIX: Ensure squad has formation component
            if (!squadEntity.HasComponent<SquadFormationComponent>())
            {
                return;
            }
            
            var formationComponent = squadEntity.GetComponent<SquadFormationComponent>();
            
            // FIX: Ensure squad has necessary movement components
            if (!squadEntity.HasComponent<PositionComponent>() || !squadEntity.HasComponent<RotationComponent>())
            {
                return;
            }
            
            var squadPosition = squadEntity.GetComponent<PositionComponent>().Position;
            var squadRotation = squadEntity.GetComponent<RotationComponent>().Rotation;
            
            // FIX: Update formation world positions
            formationComponent.UpdateWorldPositions(squadPosition, squadRotation);
            
            // Find all entities that are members of this squad
            foreach (var memberEntity in _world.GetEntitiesWith<SquadMemberComponent, SteeringDataComponent>())
            {
                var memberComponent = memberEntity.GetComponent<SquadMemberComponent>();
                
                // Skip if not a member of this squad
                if (memberComponent.SquadEntityId != squadId)
                {
                    continue;
                }
                
                // Get grid position
                int row = memberComponent.GridPosition.x;
                int col = memberComponent.GridPosition.y;
                
                // Skip if invalid position
                if (row < 0 || row >= formationComponent.Rows || col < 0 || col >= formationComponent.Columns)
                {
                    continue;
                }
                
                // Get world position from formation
                Vector3 desiredPosition = formationComponent.WorldPositions[row, col];
                
                // Update desired position in SquadMemberComponent
                memberComponent.DesiredPosition = desiredPosition;
                
                // Update steering target
                var steeringData = memberEntity.GetComponent<SteeringDataComponent>();
                steeringData.TargetPosition = desiredPosition;
                
                // FIX: Reset any accumulated steering forces if we're updating targets
                steeringData.Reset();
                
                // FIX: Để debug xem target position có đúng không
                // Debug.Log($"Troop {memberEntity.Id} of Squad {squadId} - Target pos: {desiredPosition}");
            }
        }
        
        /// <summary>
        /// Update a squad in the attacking state
        /// </summary>
        private void UpdateAttackingSquad(Entity squadEntity, SquadStateComponent stateComponent, float deltaTime)
        {
            // Check if target entity still exists
            bool targetExists = false;
            
            foreach (var entity in _world.GetEntitiesWith<PositionComponent>())
            {
                if (entity.Id == stateComponent.TargetEntityId)
                {
                    targetExists = true;
                    break;
                }
            }
            
            // If target no longer exists, transition to idle
            if (!targetExists)
            {
                stateComponent.CurrentState = SquadState.Idle;
                stateComponent.TargetEntityId = -1;
                
                // FIX: Remove target
                if (_squadTargets.ContainsKey(squadEntity.Id))
                {
                    _squadTargets.Remove(squadEntity.Id);
                }
            }
            else
            {
                // FIX: Cập nhật vị trí troop định kỳ khi tấn công
                int squadId = squadEntity.Id;
                if (_squadUpdateTimers[squadId] >= UPDATE_INTERVAL)
                {
                    _squadUpdateTimers[squadId] = 0f;
                    UpdateSquadMembersTargets(squadEntity);
                }
            }
        }
        
        /// <summary>
        /// Update a squad in the defending state
        /// </summary>
        private void UpdateDefendingSquad(Entity squadEntity, SquadStateComponent stateComponent, float deltaTime)
        {
            // FIX: Cập nhật vị trí troop định kỳ khi phòng thủ
            int squadId = squadEntity.Id;
            if (_squadUpdateTimers[squadId] >= UPDATE_INTERVAL)
            {
                _squadUpdateTimers[squadId] = 0f;
                UpdateSquadMembersFormation(squadEntity);
            }
        }
        
        /// <summary>
        /// Update squad members to maintain formation
        /// </summary>
        private void UpdateSquadMembersFormation(Entity squadEntity)
        {
            // Ensure squad has formation component
            if (!squadEntity.HasComponent<SquadFormationComponent>())
            {
                return;
            }
            
            var formationComponent = squadEntity.GetComponent<SquadFormationComponent>();
            
            // Ensure squad has necessary movement components
            if (!squadEntity.HasComponent<PositionComponent>() || !squadEntity.HasComponent<RotationComponent>())
            {
                return;
            }
            
            var squadPosition = squadEntity.GetComponent<PositionComponent>().Position;
            var squadRotation = squadEntity.GetComponent<RotationComponent>().Rotation;
            
            // Update formation world positions
            formationComponent.UpdateWorldPositions(squadPosition, squadRotation);
            
            // Find all members of this squad
            int squadId = squadEntity.Id;
            foreach (var memberEntity in _world.GetEntitiesWith<SquadMemberComponent>())
            {
                var memberComponent = memberEntity.GetComponent<SquadMemberComponent>();
                
                // Skip if not a member of this squad
                if (memberComponent.SquadEntityId != squadId)
                {
                    continue;
                }
                
                // Get grid position
                int row = memberComponent.GridPosition.x;
                int col = memberComponent.GridPosition.y;
                
                // Skip if invalid position
                if (row < 0 || row >= formationComponent.Rows || col < 0 || col >= formationComponent.Columns)
                {
                    continue;
                }
                
                // Get world position from formation
                Vector3 desiredPosition = formationComponent.WorldPositions[row, col];
                
                // Update desired position in SquadMemberComponent
                memberComponent.DesiredPosition = desiredPosition;
                
                // Update steering target if has steering component
                if (memberEntity.HasComponent<SteeringDataComponent>())
                {
                    var steeringData = memberEntity.GetComponent<SteeringDataComponent>();
                    steeringData.TargetPosition = desiredPosition;
                    
                    // Reset any accumulated steering forces
                    steeringData.Reset();
                }
            }
        }
        
        /// <summary>
        /// Command a squad to move to a position
        /// </summary>
        public void CommandMove(Entity squadEntity, Vector3 targetPosition)
        {
            if (!squadEntity.HasComponent<SquadStateComponent>())
            {
                return;
            }
            
            var stateComponent = squadEntity.GetComponent<SquadStateComponent>();
            
            // Log the command being executed
            Debug.Log($"Commanding Squad {squadEntity.Id} to move to {targetPosition}");
            
            // Change state to Moving
            stateComponent.CurrentState = SquadState.Moving;
            stateComponent.TargetPosition = targetPosition;
            stateComponent.TargetEntityId = -1;
            
            // Store target in dictionary
            _squadTargets[squadEntity.Id] = targetPosition;
            
            // Make sure initial velocity is set
            if (squadEntity.HasComponent<VelocityComponent>() && squadEntity.HasComponent<PositionComponent>())
            {
                var velocityComponent = squadEntity.GetComponent<VelocityComponent>();
                var positionComponent = squadEntity.GetComponent<PositionComponent>();
                
                Vector3 direction = (targetPosition - positionComponent.Position).normalized;
                velocityComponent.Velocity = direction * velocityComponent.GetEffectiveMaxSpeed();
                
                Debug.Log($"Initial velocity set to {velocityComponent.Velocity.magnitude}");
            }
            
            // FIX: Đặt lại update timer để cập nhật ngay lập tức
            _squadUpdateTimers[squadEntity.Id] = UPDATE_INTERVAL;
            
            // Immediately update squad members' targets
            UpdateSquadMembersTargets(squadEntity);
        }
        
        /// <summary>
        /// Command a squad to attack a target
        /// </summary>
        public void CommandAttack(Entity squadEntity, Entity targetEntity)
        {
            if (!squadEntity.HasComponent<SquadStateComponent>() || 
                !targetEntity.HasComponent<PositionComponent>())
            {
                return;
            }
            
            var stateComponent = squadEntity.GetComponent<SquadStateComponent>();
            var targetPosition = targetEntity.GetComponent<PositionComponent>().Position;
            
            // Change state to Attacking
            stateComponent.CurrentState = SquadState.Attacking;
            stateComponent.TargetPosition = targetPosition;
            stateComponent.TargetEntityId = targetEntity.Id;
            
            // Store target in dictionary
            _squadTargets[squadEntity.Id] = targetPosition;
            
            // FIX: Đặt lại update timer để cập nhật ngay lập tức
            _squadUpdateTimers[squadEntity.Id] = UPDATE_INTERVAL;
            
            // Immediately update squad members
            UpdateSquadMembersTargets(squadEntity);
        }
        
        /// <summary>
        /// Command a squad to defend its current position
        /// </summary>
        public void CommandDefend(Entity squadEntity)
        {
            if (!squadEntity.HasComponent<SquadStateComponent>())
            {
                return;
            }
            
            var stateComponent = squadEntity.GetComponent<SquadStateComponent>();
            
            // Change state to Defending
            stateComponent.CurrentState = SquadState.Defending;
            stateComponent.TargetEntityId = -1;
            
            // FIX: Remove any target
            if (_squadTargets.ContainsKey(squadEntity.Id))
            {
                _squadTargets.Remove(squadEntity.Id);
            }
            
            // FIX: Đặt lại update timer để cập nhật ngay lập tức
            _squadUpdateTimers[squadEntity.Id] = UPDATE_INTERVAL;
            
            // Immediately update squad members to formation
            UpdateSquadMembersFormation(squadEntity);
        }
        
        /// <summary>
        /// Command a squad to stop and become idle
        /// </summary>
        public void CommandStop(Entity squadEntity)
        {
            if (!squadEntity.HasComponent<SquadStateComponent>())
            {
                return;
            }
            
            var stateComponent = squadEntity.GetComponent<SquadStateComponent>();
            
            // Change state to Idle
            stateComponent.CurrentState = SquadState.Idle;
            stateComponent.TargetEntityId = -1;
            
            // FIX: Remove any target
            if (_squadTargets.ContainsKey(squadEntity.Id))
            {
                _squadTargets.Remove(squadEntity.Id);
            }
            
            // Clear velocity
            if (squadEntity.HasComponent<VelocityComponent>())
            {
                squadEntity.GetComponent<VelocityComponent>().Velocity = Vector3.zero;
            }
            
            // FIX: Đặt lại update timer để cập nhật ngay lập tức
            _squadUpdateTimers[squadEntity.Id] = UPDATE_INTERVAL;
            
            // Immediately update squad members to formation
            UpdateSquadMembersFormation(squadEntity);
        }
    }
}