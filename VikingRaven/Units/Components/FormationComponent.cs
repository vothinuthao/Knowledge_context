using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// SIMPLIFIED: Formation Component with backward compatibility
    /// Maintains existing API while simplifying internal logic
    /// Formation index is assigned once and rarely changes
    /// Focus on direct position updates rather than complex calculations
    /// </summary>
    public class FormationComponent : BaseComponent
    {
        #region Core Formation Identity - SIMPLIFIED

        [TitleGroup("Formation Identity")]
        [Tooltip("Squad ID that this unit belongs to")]
        [SerializeField, ReadOnly]
        private int _squadId = -1;
        
        [Tooltip("Formation slot index (assigned once, rarely changes)")]
        [SerializeField, ReadOnly]
        private int _formationSlotIndex = -1;
        
        [Tooltip("Current formation type")]
        [SerializeField, ReadOnly, EnumToggleButtons]
        private FormationType _currentFormationType = FormationType.Normal;

        #endregion

        #region Formation Position Data - SIMPLIFIED

        [TitleGroup("Position Data")]
        [Tooltip("Local offset from squad center")]
        [SerializeField, ReadOnly]
        private Vector3 _formationOffset = Vector3.zero;
        
        [Tooltip("Target world position in formation")]
        [SerializeField, ReadOnly]
        private Vector3 _targetFormationPosition = Vector3.zero;
        
        [Tooltip("Squad center position")]
        [SerializeField, ReadOnly]
        private Vector3 _squadCenterPosition = Vector3.zero;
        
        [Tooltip("Squad rotation")]
        [SerializeField, ReadOnly]
        private Quaternion _squadRotation = Quaternion.identity;

        #endregion

        #region Formation Settings - SIMPLIFIED

        [TitleGroup("Formation Settings")]
        [Tooltip("Formation discipline (how strictly unit maintains position)")]
        [SerializeField, Range(0f, 1f), ProgressBar(0, 1, ColorGetter = "GetDisciplineColor")]
        private float _formationDiscipline = 0.8f;
        
        [Tooltip("Whether unit can break formation for combat")]
        [SerializeField, ToggleLeft]
        private bool _canBreakFormation = true;
        
        [Tooltip("Maximum allowed distance from formation position")]
        [SerializeField, Range(0.5f, 5f)]
        private float _maxDeviationDistance = 2f;

        #endregion

        #region Formation State - SIMPLIFIED

        [TitleGroup("Formation State")]
        [Tooltip("Whether unit is in correct formation position")]
        [SerializeField, ReadOnly]
        private bool _isInFormationPosition = false;
        
        [Tooltip("Distance from target formation position")]
        [SerializeField, ReadOnly, ProgressBar(0, 5, ColorGetter = "GetDistanceColor")]
        private float _distanceFromFormationPosition = 0f;
        
        [Tooltip("Role within the formation")]
        [SerializeField, ReadOnly, EnumToggleButtons]
        private FormationRole _formationRole = FormationRole.Follower;

        #endregion

        #region BACKWARD COMPATIBILITY Properties - DO NOT CHANGE!

        public int FormationSlotIndex => _formationSlotIndex;
        
        public Vector3 FormationOffset => _formationOffset;
        
        /// <summary>
        /// BACKWARD COMPATIBILITY: Used by squad management systems
        /// </summary>
        public int SquadId => _squadId;
        
        /// <summary>
        /// BACKWARD COMPATIBILITY: Used by formation systems
        /// </summary>
        public FormationType CurrentFormationType => _currentFormationType;

        #endregion

        #region Enhanced Properties - NEW FEATURES

        public Vector3 TargetFormationPosition => _targetFormationPosition;
        public Vector3 SquadCenterPosition => _squadCenterPosition;
        public Quaternion SquadRotation => _squadRotation;
        public float FormationDiscipline => _formationDiscipline;
        public bool CanBreakFormation => _canBreakFormation;
        public float MaxDeviationDistance => _maxDeviationDistance;
        public bool IsInFormationPosition => _isInFormationPosition;
        public float DistanceFromFormationPosition => _distanceFromFormationPosition;
        public FormationRole FormationRole => _formationRole;

        #endregion

        #region BACKWARD COMPATIBILITY Methods - MAINTAIN EXISTING API

        public void SetFormationType(FormationType formationType, bool smoothTransition = true)
        {
            if (_currentFormationType != formationType)
            {
                FormationType oldType = _currentFormationType;
                _currentFormationType = formationType;
                
                UpdateFormationDisciplineForType(formationType);
                
                // Handle animation changes if needed
                HandleFormationTypeChange(oldType, formationType);
                
                if (Application.isPlaying && _enableDetailedLogging)
                {
                    Debug.Log($"FormationComponent: Entity {Entity?.Id} changed formation " +
                             $"from {oldType} to {formationType} (smooth: {smoothTransition})");
                }
            }
        }

        /// <summary>
        /// BACKWARD COMPATIBILITY: Used by FormationSystem and SquadFactory
        /// </summary>
        public void SetFormationOffset(Vector3 offset, bool smoothTransition = false)
        {
            _formationOffset = offset;
            
            // Update target position if squad data is available
            if (_squadCenterPosition != Vector3.zero)
            {
                _targetFormationPosition = _squadCenterPosition + (_squadRotation * _formationOffset);
            }
            
            // Notify NavigationComponent for backward compatibility
            var navigationComponent = Entity?.GetComponent<NavigationComponent>();
            if (navigationComponent != null)
            {
                navigationComponent.UpdateFormationOffset(_formationOffset);
            }
        }

        /// <summary>
        /// BACKWARD COMPATIBILITY: Used by SquadFactory and coordination systems
        /// </summary>
        public void SetFormationSlot(int slotIndex)
        {
            _formationSlotIndex = slotIndex;
        }

        /// <summary>
        /// BACKWARD COMPATIBILITY: Used by squad management
        /// </summary>
        public void SetSquadId(int squadId)
        {
            _squadId = squadId;
        }

        #endregion

        #region Enhanced Methods - NEW SIMPLIFIED FEATURES

        /// <summary>
        /// SIMPLIFIED: Set complete formation position data in one call
        /// Used by new FormationSystem for efficient updates
        /// </summary>
        public void SetFormationPositionData(Vector3 offset, Vector3 targetPosition, 
            Vector3 squadCenter, Quaternion squadRotation)
        {
            _formationOffset = offset;
            _targetFormationPosition = targetPosition;
            _squadCenterPosition = squadCenter;
            _squadRotation = squadRotation;
        }
        public void UpdateFormationState(bool isInPosition, float distanceFromTarget, bool isTransitioning)
        {
            _isInFormationPosition = isInPosition;
            _distanceFromFormationPosition = distanceFromTarget;
        }
        public void SetFormationRole(FormationRole role)
        {
            _formationRole = role;
        }
        public void SetFormationDiscipline(float discipline)
        {
            _formationDiscipline = Mathf.Clamp01(discipline);
        }

        #endregion

        #region Formation Queries - SIMPLIFIED
        public float GetFormationEffectiveness()
        {
            if (!_isInFormationPosition)
                return 0.5f * _formationDiscipline;
                
            return _formationDiscipline;
        }
        public bool ShouldMaintainFormation()
        {
            if (!_canBreakFormation) return true;
            return _formationDiscipline > 0.7f;
        }

        #endregion

        #region Component Lifecycle - SIMPLIFIED

        public override void Initialize()
        {
            base.Initialize();
            
            if (Entity != null)
            {
                var unitTypeComponent = Entity.GetComponent<UnitTypeComponent>();
                if (unitTypeComponent != null)
                {
                    InitializeDefaultsForUnitType(unitTypeComponent.UnitType);
                }
            }
            
            if (_formationOffset == Vector3.zero && _formationSlotIndex >= 0)
            {
                _formationOffset = new Vector3(
                    Random.Range(-0.1f, 0.1f), 
                    0, 
                    Random.Range(-0.1f, 0.1f)
                );
            }
        }

        private void InitializeDefaultsForUnitType(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Pike:
                    _formationDiscipline = 0.9f;
                    _canBreakFormation = false;
                    break;
                    
                case UnitType.Infantry:
                    _formationDiscipline = 0.8f;
                    _canBreakFormation = true;
                    break;
                    
                case UnitType.Archer:
                    _formationDiscipline = 0.7f;
                    _canBreakFormation = true;
                    break;
                    
                default:
                    _formationDiscipline = 0.8f;
                    _canBreakFormation = true;
                    break;
            }
        }
        private void UpdateFormationDisciplineForType(FormationType formationType)
        {
            switch (formationType)
            {
                case FormationType.Phalanx:
                case FormationType.Testudo:
                    _formationDiscipline = Mathf.Max(_formationDiscipline, 0.8f);
                    break;
                case FormationType.Normal:
                default:
                    break;
            }
        }
        private void HandleFormationTypeChange(FormationType oldType, FormationType newType)
        {
            var animationComponent = Entity?.GetComponent<AnimationComponent>();
            if (animationComponent != null)
            {
                string animationName = newType switch
                {
                    FormationType.Phalanx => "PhalanxStance",
                    FormationType.Testudo => "TestudoStance",
                    _ => "NormalStance"
                };
                
                animationComponent.PlayAnimation(animationName);
            }
        }

        public override void Cleanup()
        {
            _squadId = -1;
            _formationSlotIndex = -1;
            _currentFormationType = FormationType.Normal;
            _formationOffset = Vector3.zero;
            _targetFormationPosition = Vector3.zero;
            _squadCenterPosition = Vector3.zero;
            _squadRotation = Quaternion.identity;
            _isInFormationPosition = false;
            _distanceFromFormationPosition = 0f;
            _formationRole = FormationRole.Follower;
            
            base.Cleanup();
        }

        #endregion

        #region Debug and Visualization - ENHANCED ODIN

        [TitleGroup("Debug Information")]
        [SerializeField, ReadOnly] private bool _enableDetailedLogging = false;
        
        [TitleGroup("Debug Information")]
        [Button("Show Formation Info"), ShowInInspector]
        public void ShowFormationInfo()
        {
            if (Entity == null)
            {
                Debug.Log("FormationComponent: No entity assigned");
                return;
            }
            
            string info = BuildFormationInfoString();
            Debug.Log(info);
        }

        private string BuildFormationInfoString()
        {
            string info = $"=== Formation Info for Entity {Entity.Id} ===\n";
            info += $"Squad ID: {_squadId}\n";
            info += $"Formation Slot: {_formationSlotIndex}\n";
            info += $"Formation Type: {_currentFormationType}\n";
            info += $"Formation Role: {_formationRole}\n";
            info += $"Formation Offset: {_formationOffset}\n";
            info += $"Target Position: {_targetFormationPosition}\n";
            info += $"Squad Center: {_squadCenterPosition}\n";
            info += $"In Position: {_isInFormationPosition}\n";
            info += $"Distance from Target: {_distanceFromFormationPosition:F2}\n";
            info += $"Formation Discipline: {_formationDiscipline:F2}\n";
            info += $"Can Break Formation: {_canBreakFormation}\n";
            info += $"Max Deviation: {_maxDeviationDistance:F2}\n";
            
            return info;
        }

        #endregion

        #region Editor-Only Debug Gizmos

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            if (_formationSlotIndex >= 0)
            {
                Vector3 textPos = transform.position + Vector3.up * 2f;
                UnityEditor.Handles.Label(textPos, $"Slot: {_formationSlotIndex}");
            }
            if (_targetFormationPosition != Vector3.zero)
            {
                Gizmos.color = _isInFormationPosition ? Color.green : Color.red;
                Gizmos.DrawWireSphere(_targetFormationPosition, 0.3f);
                Gizmos.DrawLine(transform.position, _targetFormationPosition);
            }
            if (_squadCenterPosition != Vector3.zero)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(_squadCenterPosition, Vector3.one * 0.5f);
            }
        }
        #endif

        #endregion
    }

    #region Supporting Enums
    public enum FormationRole
    {
        Leader = 0,     // Squad leader (slot 0)
        Follower = 1,   // Standard formation member
        FrontLine = 2,  // Front-line combat unit
        Support = 3,    // Support/ranged unit
        Flanker = 4,    // Side protection
        Reserve = 5     // Back-line reserve
    }

    #endregion
}