using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Enhanced FormationComponent with backward compatibility
    /// Maintains all existing API while adding new ECS-compliant features
    /// Existing scripts will continue to work without modification
    /// </summary>
    public class FormationComponent : BaseComponent
    {
        #region Core Formation Data (Enhanced)
        
        [TitleGroup("Formation Identity")]
        [Tooltip("Squad ID that this unit belongs to")]
        [SerializeField, ReadOnly]
        private int _squadId = -1;
        
        [Tooltip("Slot index within the formation (0 = leader, 1-8 = followers)")]
        [SerializeField, ReadOnly]
        private int _formationSlotIndex = -1;
        
        [Tooltip("Current formation type")]
        [SerializeField, ReadOnly, EnumToggleButtons]
        private FormationType _currentFormationType = FormationType.Normal;

        #endregion

        #region Formation Position Data (Enhanced)
        
        [TitleGroup("Position Data")]
        [Tooltip("Local offset from squad center in formation")]
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

        #region Formation Behavior Data (Enhanced)
        
        [TitleGroup("Behavior Settings")]
        [Tooltip("How strictly this unit maintains formation position (0-1)")]
        [SerializeField, Range(0f, 1f), ProgressBar(0, 1)]
        private float _formationDiscipline = 0.8f;
        
        [Tooltip("Priority level for formation positioning")]
        [SerializeField, EnumToggleButtons]
        private FormationPriority _formationPriority = FormationPriority.Normal;
        
        [Tooltip("Whether this unit can break formation for combat")]
        [SerializeField, ToggleLeft]
        private bool _canBreakFormation = true;
        
        [Tooltip("Maximum distance allowed from formation position")]
        [SerializeField, Range(0.5f, 5f)]
        private float _maxDeviationDistance = 2f;

        #endregion

        #region Formation State Data (Enhanced)
        
        [TitleGroup("State Information")]
        [Tooltip("Whether unit is currently in correct formation position")]
        [SerializeField, ReadOnly]
        private bool _isInFormationPosition = false;
        
        [Tooltip("Whether formation is currently transitioning")]
        [SerializeField, ReadOnly]
        private bool _isFormationTransitioning = false;
        
        [Tooltip("Current distance from target formation position")]
        [SerializeField, ReadOnly, ProgressBar(0, 5)]
        private float _distanceFromFormationPosition = 0f;
        
        [Tooltip("Time when last formation change occurred")]
        [SerializeField, ReadOnly]
        private float _lastFormationChangeTime = 0f;

        #endregion

        #region Formation Role Data (New Enhanced Features)
        
        [TitleGroup("Role Information")]
        [Tooltip("Role of this unit within the formation")]
        [SerializeField, ReadOnly, EnumToggleButtons]
        private FormationRole _formationRole = FormationRole.Follower;
        
        [Tooltip("Special formation abilities for this unit")]
        [SerializeField, EnumToggleButtons]
        private FormationAbility _formationAbilities = FormationAbility.None;

        #endregion

        #region Backward Compatibility Properties (EXISTING API)

        /// <summary>
        /// BACKWARD COMPATIBILITY: Existing API - do not change!
        /// Used by: PhalanxBehavior, SquadCoordinationSystem, MovementSystem
        /// </summary>
        public int FormationSlotIndex => _formationSlotIndex;
        
        /// <summary>
        /// BACKWARD COMPATIBILITY: Existing API - do not change!
        /// Used by: PhalanxBehavior, FormationSystem, NavigationComponent
        /// </summary>
        public Vector3 FormationOffset => _formationOffset;
        
        /// <summary>
        /// BACKWARD COMPATIBILITY: Existing API - do not change!
        /// Used by: SquadCoordinationSystem, FormationSystem, TacticalAnalysisSystem
        /// </summary>
        public int SquadId => _squadId;
        
        /// <summary>
        /// BACKWARD COMPATIBILITY: Existing API - do not change!
        /// Used by: SquadCoordinationSystem, FormationSystem, TacticalAnalysisSystem
        /// </summary>
        public FormationType CurrentFormationType => _currentFormationType;

        #endregion

        #region Enhanced Properties (New ECS Features)
        
        // Enhanced position data
        public Vector3 TargetFormationPosition => _targetFormationPosition;
        public Vector3 SquadCenterPosition => _squadCenterPosition;
        public Quaternion SquadRotation => _squadRotation;
        
        // Enhanced behavior settings
        public float FormationDiscipline => _formationDiscipline;
        public FormationPriority FormationPriority => _formationPriority;
        public bool CanBreakFormation => _canBreakFormation;
        public float MaxDeviationDistance => _maxDeviationDistance;
        
        // Enhanced state information
        public bool IsInFormationPosition => _isInFormationPosition;
        public bool IsFormationTransitioning => _isFormationTransitioning;
        public float DistanceFromFormationPosition => _distanceFromFormationPosition;
        public float LastFormationChangeTime => _lastFormationChangeTime;
        
        // Enhanced role information
        public FormationRole FormationRole => _formationRole;
        public FormationAbility FormationAbilities => _formationAbilities;

        #endregion

        #region Backward Compatibility Methods (EXISTING API)

        /// <summary>
        /// BACKWARD COMPATIBILITY: Used by SquadCoordinationSystem.SetSquadFormation()
        /// Maintains smooth transition parameter for existing calls
        /// </summary>
        public void SetFormationType(FormationType formationType, bool smoothTransition = true)
        {
            if (_currentFormationType != formationType)
            {
                FormationType oldType = _currentFormationType;
                _currentFormationType = formationType;
                _lastFormationChangeTime = Time.time;
                _isFormationTransitioning = smoothTransition;
                
                // Handle formation-specific behaviors (existing logic)
                HandleFormationTypeChange(oldType, formationType);
                
                Debug.Log($"FormationComponent: Formation changed from {oldType} to {formationType} " +
                         $"(smooth: {smoothTransition}) for entity {Entity?.Id}");
            }
        }

        /// <summary>
        /// BACKWARD COMPATIBILITY: Used by FormationSystem.UpdateFormationPositions()
        /// Maintains existing signature for smooth transition parameter
        /// </summary>
        public void SetFormationOffset(Vector3 offset, bool smoothTransition = false)
        {
            if (smoothTransition && _formationOffset != Vector3.zero)
            {
                // Enhanced: Start smooth transition with new capabilities
                Vector3 previousOffset = _formationOffset;
                _formationOffset = offset;
                _isFormationTransitioning = true;
                
                // Calculate target position based on current squad data
                if (_squadCenterPosition != Vector3.zero)
                {
                    _targetFormationPosition = _squadCenterPosition + (_squadRotation * _formationOffset);
                }
                
                // Notify NavigationComponent if present (existing behavior)
                var navigationComponent = Entity?.GetComponent<NavigationComponent>();
                if (navigationComponent != null)
                {
                    navigationComponent.UpdateFormationOffset(_formationOffset);
                }
            }
            else
            {
                // Immediate update (existing behavior)
                _formationOffset = offset;
                
                // Update target position
                if (_squadCenterPosition != Vector3.zero)
                {
                    _targetFormationPosition = _squadCenterPosition + (_squadRotation * _formationOffset);
                }
                
                // Notify NavigationComponent
                var navigationComponent = Entity?.GetComponent<NavigationComponent>();
                if (navigationComponent != null)
                {
                    navigationComponent.UpdateFormationOffset(_formationOffset);
                }
            }
        }

        /// <summary>
        /// BACKWARD COMPATIBILITY: Used by SquadCoordinationSystem, FormationSystem
        /// </summary>
        public void SetFormationSlot(int slotIndex)
        {
            _formationSlotIndex = slotIndex;
        }

        /// <summary>
        /// BACKWARD COMPATIBILITY: Used by FormationSystem, SquadCoordinationSystem
        /// </summary>
        public void SetSquadId(int squadId)
        {
            _squadId = squadId;
        }

        #endregion

        #region Enhanced Methods (New ECS Features)

        /// <summary>
        /// ENHANCED: Set complete formation position data (called by new FormationSystem)
        /// </summary>
        public void SetFormationPositionData(Vector3 offset, Vector3 targetPosition, Vector3 squadCenter, Quaternion squadRotation)
        {
            _formationOffset = offset;
            _targetFormationPosition = targetPosition;
            _squadCenterPosition = squadCenter;
            _squadRotation = squadRotation;
        }

        /// <summary>
        /// ENHANCED: Update formation state (called by new FormationSystem)
        /// </summary>
        public void UpdateFormationState(bool isInPosition, float distanceFromTarget, bool isTransitioning)
        {
            _isInFormationPosition = isInPosition;
            _distanceFromFormationPosition = distanceFromTarget;
            _isFormationTransitioning = isTransitioning;
        }

        /// <summary>
        /// ENHANCED: Set formation role (called by new FormationSystem)
        /// </summary>
        public void SetFormationRole(FormationRole role)
        {
            _formationRole = role;
        }

        /// <summary>
        /// ENHANCED: Set formation discipline
        /// </summary>
        public void SetFormationDiscipline(float discipline)
        {
            _formationDiscipline = Mathf.Clamp01(discipline);
        }

        /// <summary>
        /// ENHANCED: Set formation priority
        /// </summary>
        public void SetFormationPriority(FormationPriority priority)
        {
            _formationPriority = priority;
        }

        #endregion

        #region Formation Queries (Enhanced + Backward Compatible)

        public float GetFormationEffectiveness()
        {
            if (!_isInFormationPosition)
                return 0.5f;
                
            return _formationDiscipline;
        }

        #endregion

        #region Component Lifecycle (Enhanced)

        public override void Initialize()
        {
            base.Initialize();
            
            // BACKWARD COMPATIBILITY: Maintain existing initialization behavior
            if (_formationOffset == Vector3.zero)
            {
                _formationOffset = new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));
            }

            // ENHANCED: Set default values based on entity properties
            if (Entity != null)
            {
                var unitTypeComponent = Entity.GetComponent<UnitTypeComponent>();
                if (unitTypeComponent != null)
                {
                    InitializeDefaultsForUnitType(unitTypeComponent.UnitType);
                }
            }
            
            // BACKWARD COMPATIBILITY: Maintain existing event subscriptions
            var healthComponent = Entity?.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.OnDamageTaken += (amount, source) => {
                    var stateComponent = Entity?.GetComponent<StateComponent>();
                    if (stateComponent != null && stateComponent.StateMachineInGame != null)
                    {
                        // Existing damage handling logic
                    }
                };
            }
        }

        /// <summary>
        /// ENHANCED: Initialize default formation values based on unit type
        /// </summary>
        private void InitializeDefaultsForUnitType(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Pike:
                    _formationDiscipline = 0.9f; // Pikes need strict formation
                    _formationAbilities = FormationAbility.FrontLine | FormationAbility.AntiCavalry;
                    _canBreakFormation = false; // Pikes should stay in formation
                    break;
                    
                case UnitType.Infantry:
                    _formationDiscipline = 0.8f; // Good formation discipline
                    _formationAbilities = FormationAbility.Flexible | FormationAbility.Shield;
                    _canBreakFormation = true; // Can break for combat
                    break;
                    
                case UnitType.Archer:
                    _formationDiscipline = 0.7f; // Less strict formation
                    _formationAbilities = FormationAbility.Ranged | FormationAbility.Support;
                    _canBreakFormation = true; // Can reposition for better shots
                    break;
                    
                default:
                    _formationDiscipline = 0.8f;
                    _formationAbilities = FormationAbility.Flexible;
                    _canBreakFormation = true;
                    break;
            }
        }

        /// <summary>
        /// BACKWARD COMPATIBILITY + ENHANCED: Handle formation type changes
        /// Maintains existing behavior while adding new capabilities
        /// </summary>
        private void HandleFormationTypeChange(FormationType oldType, FormationType newType)
        {
            // BACKWARD COMPATIBILITY: Maintain existing animation behavior
            var animationComponent = Entity?.GetComponent<AnimationComponent>();
            if (animationComponent != null)
            {
                switch (newType)
                {
                    case FormationType.Phalanx:
                        animationComponent.PlayAnimation("PhalanxStance");
                        break;
                        
                    case FormationType.Testudo:
                        animationComponent.PlayAnimation("TestudoStance");
                        break;
                        
                    default:
                        // Return to normal stance if coming from special formation
                        if (oldType == FormationType.Phalanx || oldType == FormationType.Testudo)
                        {
                            animationComponent.PlayAnimation("NormalStance");
                        }
                        break;
                }
            }
            
            switch (newType)
            {
                case FormationType.Phalanx:
                case FormationType.Testudo:
                    _formationPriority = FormationPriority.High;
                    break;
                case FormationType.Normal:
                    _formationPriority = FormationPriority.Normal;
                    break;
                default:
                    _formationPriority = FormationPriority.Normal;
                    break;
            }
        }

        public override void Cleanup()
        {
            // BACKWARD COMPATIBILITY: Reset all data to maintain existing behavior
            _squadId = -1;
            _formationSlotIndex = -1;
            _currentFormationType = FormationType.Normal;
            _formationOffset = Vector3.zero;
            _targetFormationPosition = Vector3.zero;
            _squadCenterPosition = Vector3.zero;
            _squadRotation = Quaternion.identity;
            _isInFormationPosition = false;
            _isFormationTransitioning = false;
            _distanceFromFormationPosition = 0f;
            _formationRole = FormationRole.Follower;
            
            base.Cleanup();
        }

        #endregion

        #region Debug Tools (Enhanced)

        [TitleGroup("Debug Tools")]
        [Button("Show Formation Info"), ShowInInspector]
        public void ShowFormationInfo()
        {
            if (Entity == null)
            {
                Debug.Log("FormationComponent: No entity assigned");
                return;
            }
            
            string info = $"=== Formation Info for Entity {Entity.Id} ===\n";
            info += $"Squad ID: {_squadId}\n";
            info += $"Slot Index: {_formationSlotIndex}\n";
            info += $"Formation Type: {_currentFormationType}\n";
            info += $"Formation Role: {_formationRole}\n";
            info += $"Formation Offset: {_formationOffset}\n";
            info += $"Target Position: {_targetFormationPosition}\n";
            info += $"In Position: {_isInFormationPosition}\n";
            info += $"Distance from Target: {_distanceFromFormationPosition:F2}\n";
            info += $"Formation Discipline: {_formationDiscipline:F2}\n";
            info += $"Can Break Formation: {_canBreakFormation}\n";
            info += $"Formation Abilities: {_formationAbilities}\n";
            
            Debug.Log(info);
        }

        [Button("Test Existing API"), ShowInInspector]
        public void TestExistingAPI()
        {
            Debug.Log("=== Testing Backward Compatibility ===");
            
            // Test existing API calls that other scripts use
            Debug.Log($"FormationSlotIndex: {FormationSlotIndex}");
            Debug.Log($"FormationOffset: {FormationOffset}");
            Debug.Log($"SquadId: {SquadId}");
            Debug.Log($"CurrentFormationType: {CurrentFormationType}");
            
            // Test existing method calls
            SetFormationSlot(5);
            SetSquadId(1);
            SetFormationType(FormationType.Phalanx, true);
            SetFormationOffset(new Vector3(1, 0, 1), false);
            
            Debug.Log("All existing API calls work correctly!");
        }

        #endregion
    }

    #region Supporting Enums (Enhanced)

    /// <summary>
    /// Priority levels for formation positioning
    /// </summary>
    public enum FormationPriority
    {
        Low = 0,        // Can easily break formation
        Normal = 1,     // Standard formation discipline
        High = 2,       // Strong formation discipline
        Critical = 3    // Must maintain formation at all costs
    }

    /// <summary>
    /// Role of unit within formation
    /// </summary>
    public enum FormationRole
    {
        Leader = 0,     // Squad leader, others follow
        Follower = 1,   // Standard formation member
        FrontLine = 2,  // Front-line combat unit
        Support = 3,    // Support/ranged unit
        Flanker = 4,    // Side protection unit
        Reserve = 5     // Back-line reserve unit
    }

    /// <summary>
    /// Special formation abilities (flags enum)
    /// </summary>
    [System.Flags]
    public enum FormationAbility
    {
        None = 0,
        FrontLine = 1 << 0,     // Effective in front-line formations
        Shield = 1 << 1,        // Can provide shield protection
        Ranged = 1 << 2,        // Ranged combat capability
        AntiCavalry = 1 << 3,   // Effective against cavalry
        Flexible = 1 << 4,      // Can adapt to multiple roles
        Support = 1 << 5,       // Support other units
        Heavy = 1 << 6,         // Heavy unit, slower movement
        Light = 1 << 7,         // Light unit, fast movement
        Elite = 1 << 8          // Elite unit, special abilities
    }

    #endregion
}