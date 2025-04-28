// Complete SquadComponent with all necessary features
using System.Collections.Generic;
using UnityEngine;
using Core.ECS;

namespace Components.Squad
{
    public enum SquadState
    {
        IDLE,
        MOVING,
        ATTACKING,
        DEFENDING
    }

    public enum FormationType
    {
        BASIC,      // Standard rectangular formation
        PHALANX,    // Defensive formation
        TESTUDO,    // Turtle formation for ranged defense
        WEDGE       // Offensive formation
    }

    /// <summary>
    /// Main component for squad entities
    /// </summary>
    public class SquadComponent : IComponent
    {
        // Core state
        public SquadState State { get; set; } = SquadState.IDLE;
        public FormationType Formation { get; set; } = FormationType.BASIC;
        
        public Vector3 TargetPosition { get; set; }
        public int TargetEntityId { get; set; } = -1;
        // Grid position
        public Vector2Int GridPosition { get; set; }
        public Vector2Int TargetGridPosition { get; set; }
        
        // Path management
        public List<Vector2Int> CurrentPath { get; set; } = new List<Vector2Int>();
        public int PathIndex { get; set; } = 0;
        
        // Squad members
        public List<int> MemberIds { get; } = new List<int>();
        public int LeaderId { get; set; } = -1;
        
        // Formation data
        public float FormationSpacing { get; set; } = 1.5f;
        public Vector3[] MemberOffsets { get; set; }
        public Quaternion FormationRotation { get; set; } = Quaternion.identity;
        
        // Combat data
        public float MoraleValue { get; set; } = 1.0f;
        public int TargetSquadId { get; set; } = -1;
        public float CombatRange { get; set; } = 1.5f;
        public float TimeInCurrentState { get; set; } = 0f;
        
        // Movement
        public float MovementSpeed { get; set; } = 3.0f;
        public float RotationSpeed { get; set; } = 5.0f;
        public Vector3 PreviousPosition { get; set; } = Vector3.zero;
        public Vector3 MovementDirection { get; set; } = Vector3.forward;
        
        // Formation management
        private bool _formationNeedsUpdate = false;
        public bool IsMoving => State == SquadState.MOVING;
        public bool ShouldLockTroops => State == SquadState.IDLE;
        public SquadState CurrentState => State;
        
        /// <summary>
        /// Initialize squad with default values
        /// </summary>
        public SquadComponent(int maxMembers = 9)
        {
            MemberOffsets = new Vector3[maxMembers];
            UpdateFormation();
        }
        
        /// <summary>
        /// Add a troop to this squad
        /// </summary>
        public bool AddMember(int entityId)
        {
            if (MemberIds.Count >= MemberOffsets.Length)
            {
                Debug.LogWarning($"Squad {LeaderId} is full! Cannot add more members.");
                return false;
            }
    
            if (MemberIds.Contains(entityId))
            {
                Debug.LogWarning($"Entity {entityId} is already a member of squad {LeaderId}");
                return false;
            }
            MemberIds.Add(entityId);
            if (LeaderId == -1)
                LeaderId = entityId;
            
            _formationNeedsUpdate = true;
            Debug.Log($"Added member {entityId} to squad. Total members: {MemberIds.Count}");
            return true;
        }
        
        /// <summary>
        /// Remove a troop from this squad
        /// </summary>
        public void RemoveMember(int entityId)
        {
            MemberIds.Remove(entityId);
            
            // Select new leader if needed
            if (LeaderId == entityId)
            {
                LeaderId = MemberIds.Count > 0 ? MemberIds[0] : -1;
            }
            
            _formationNeedsUpdate = true;
        }
        
        /// <summary>
        /// Update formation offsets based on current formation type
        /// </summary>
        public void UpdateFormation()
        {
            switch (Formation)
            {
                case FormationType.BASIC:
                    GenerateBasicFormation();
                    break;
                case FormationType.PHALANX:
                    GeneratePhalanxFormation();
                    break;
                case FormationType.TESTUDO:
                    GenerateTestudoFormation();
                    break;
                case FormationType.WEDGE:
                    GenerateWedgeFormation();
                    break;
            }
            
            _formationNeedsUpdate = false;
        }
        
        private void GenerateBasicFormation()
        {
            int rows = Mathf.CeilToInt(Mathf.Sqrt(MemberIds.Count));
            int cols = Mathf.CeilToInt((float)MemberIds.Count / rows);
            
            for (int i = 0; i < MemberIds.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                
                float xOffset = (col - (cols - 1) / 2.0f) * FormationSpacing;
                float zOffset = (row - (rows - 1) / 2.0f) * FormationSpacing;
                
                MemberOffsets[i] = new Vector3(xOffset, 0, zOffset);
            }
        }
        
        private void GeneratePhalanxFormation()
        {
            // Phalanx: tight rows, spears forward
            int rows = Mathf.Min(3, MemberIds.Count);
            int colsPerRow = Mathf.CeilToInt((float)MemberIds.Count / rows);
            
            for (int i = 0; i < MemberIds.Count; i++)
            {
                int row = i % rows;
                int col = i / rows;
                
                float xOffset = (col - (colsPerRow - 1) / 2.0f) * FormationSpacing;
                float zOffset = row * FormationSpacing * 0.7f; // Tighter rows
                
                MemberOffsets[i] = new Vector3(xOffset, 0, zOffset);
            }
        }
        
        private void GenerateTestudoFormation()
        {
            // Testudo: square formation, shields all around
            int side = Mathf.CeilToInt(Mathf.Sqrt(MemberIds.Count));
            
            for (int i = 0; i < MemberIds.Count; i++)
            {
                int row = i / side;
                int col = i % side;
                
                float xOffset = (col - (side - 1) / 2.0f) * FormationSpacing * 0.8f;
                float zOffset = (row - (side - 1) / 2.0f) * FormationSpacing * 0.8f;
                
                MemberOffsets[i] = new Vector3(xOffset, 0, zOffset);
            }
        }
        
        private void GenerateWedgeFormation()
        {
            // Wedge: triangular formation for charging
            int numRows = Mathf.CeilToInt(Mathf.Sqrt(MemberIds.Count * 2));
            int index = 0;
            
            for (int row = 0; row < numRows && index < MemberIds.Count; row++)
            {
                int colsInRow = row + 1;
                
                for (int col = 0; col < colsInRow && index < MemberIds.Count; col++)
                {
                    float xOffset = (col - (colsInRow - 1) / 2.0f) * FormationSpacing;
                    float zOffset = row * FormationSpacing;
                    
                    MemberOffsets[index++] = new Vector3(xOffset, 0, zOffset);
                }
            }
        }
        
        /// <summary>
        /// Check if formation needs update
        /// </summary>
        public bool NeedsFormationUpdate()
        {
            return _formationNeedsUpdate;
        }
        
        /// <summary>
        /// Get offset for a specific member
        /// </summary>
        public Vector3 GetMemberOffset(int index)
        {
            if (index >= 0 && index < MemberOffsets.Length)
                return MemberOffsets[index];
            return Vector3.zero;
        }
        public void UpdateTime(float deltaTime)
        {
            TimeInCurrentState += deltaTime;
        }
    
        public void ChangeState(SquadState newState)
        {
            if (State != newState)
            {
                State = newState;
                TimeInCurrentState = 0f;
            }
        }
        public void UpdateMovementInfo(Vector3 currentPosition)
         {
             if (PreviousPosition == Vector3.zero)
             {
                 PreviousPosition = currentPosition;
                 return;
             }
             Vector3 movementVector = currentPosition - PreviousPosition;
             if (movementVector.magnitude > 0.1f)
             {
                 MovementDirection = movementVector.normalized;
             }
             PreviousPosition = currentPosition;
         }
    }
}