using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class FormationComponent : BaseComponent
    {
        #region Core Formation Identity
        [Tooltip("Get information Unit")]
        [SerializeField]
        private UnitInfoComponent unitInfo;
        
        [TitleGroup("Formation Identity")]
        [Tooltip("Squad ID that this unit belongs to")]
        [SerializeField, ReadOnly]
        private int _squadId = -1;
        
        [TitleGroup("Formation Identity")]
        [Tooltip("Formation slot index (0 = Leader, 1-8 = Followers)")]
        [SerializeField, ReadOnly, ProgressBar(0, 8, ColorGetter = "GetSlotIndexColor")]
        private int _formationSlotIndex = -1;
        
        [TitleGroup("Formation Identity")]
        [Tooltip("Is this unit the squad leader?")]
        [SerializeField, ReadOnly, ToggleLeft]
        private bool _isSquadLeader = false;
        
        [TitleGroup("Formation Identity")]
        [Tooltip("Current formation type")]
        [SerializeField, ReadOnly, EnumToggleButtons]
        private FormationType _currentFormationType = FormationType.Normal;

        #endregion

        #region Formation Position Data

        [TitleGroup("Position Data")]
        [Tooltip("Local offset from squad center")]
        [SerializeField, ReadOnly]
        private Vector3 _formationOffset = Vector3.zero;
        
        [TitleGroup("Position Data")]
        [Tooltip("Target world position in formation")]
        [SerializeField, ReadOnly]
        private Vector3 _targetFormationPosition = Vector3.zero;
        
        [TitleGroup("Position Data")]
        [Tooltip("Squad center position")]
        [SerializeField, ReadOnly]
        private Vector3 _squadCenterPosition = Vector3.zero;
        
        [TitleGroup("Position Data")]
        [Tooltip("Squad rotation")]
        [SerializeField, ReadOnly]
        private Quaternion _squadRotation = Quaternion.identity;

        #endregion

        #region Formation Settings - ENHANCED

        [TitleGroup("Formation Settings")]
        [Tooltip("Formation discipline (how strictly unit maintains position)")]
        [SerializeField, Range(0f, 1f), ProgressBar(0, 1, ColorGetter = "GetDisciplineColor")]
        private float _formationDiscipline = 0.8f;
        
        [TitleGroup("Formation Settings")]
        [Tooltip("Whether unit can break formation for combat")]
        [SerializeField, ToggleLeft]
        private bool _canBreakFormation = true;
        
        [TitleGroup("Formation Settings")]
        [Tooltip("Maximum allowed distance from formation position")]
        [SerializeField, Range(0.5f, 5f)]
        private float _maxDeviationDistance = 2f;

        #endregion

        #region Formation State - ENHANCED

        [TitleGroup("Formation State")]
        [Tooltip("Whether unit is in correct formation position")]
        [SerializeField, ReadOnly, ToggleLeft]
        private bool _isInFormationPosition = false;
        
        [TitleGroup("Formation State")]
        [Tooltip("Distance from target formation position")]
        [SerializeField, ReadOnly, ProgressBar(0, 5, ColorGetter = "GetDistanceColor")]
        private float _distanceFromFormationPosition = 0f;
        
        [TitleGroup("Formation State")]
        [Tooltip("Role within the formation")]
        [SerializeField, ReadOnly, EnumToggleButtons]
        private FormationRole _formationRole = FormationRole.Follower;

        #endregion

        #region Debug and Visualization - NEW

        [TitleGroup("Debug Visualization")]
        [Tooltip("Enable debug gizmos for this unit")]
        [SerializeField, ToggleLeft]
        private bool _enableDebugGizmos = true;
        
        [TitleGroup("Debug Visualization")]
        [Tooltip("Show formation slot number in scene view")]
        [SerializeField, ToggleLeft]
        private bool _showSlotNumber = true;
        
        [TitleGroup("Debug Visualization")]
        [Tooltip("Show leader crown indicator")]
        [SerializeField, ToggleLeft]
        private bool _showLeaderIndicator = true;
        
        [TitleGroup("Debug Visualization")]
        [Tooltip("Line color for formation connections")]
        [SerializeField]
        private Color _formationLineColor = Color.green;

        #endregion

        #region Color Getters for Odin Inspector

        /// <summary>
        /// Color getter for formation slot index progress bar
        /// </summary>
        private Color GetSlotIndexColor()
        {
            if (_formationSlotIndex == 0) return Color.yellow; // Leader
            if (_formationSlotIndex <= 3) return Color.green;  // Front line
            return Color.blue; // Support/Flanker
        }

        /// <summary>
        /// Color getter for formation discipline progress bar
        /// </summary>
        private Color GetDisciplineColor()
        {
            if (_formationDiscipline >= 0.8f) return Color.green;
            if (_formationDiscipline >= 0.6f) return Color.yellow;
            return Color.red;
        }

        /// <summary>
        /// Color getter for distance from formation position progress bar
        /// </summary>
        private Color GetDistanceColor()
        {
            if (_distanceFromFormationPosition <= 1f) return Color.green;
            if (_distanceFromFormationPosition <= 3f) return Color.yellow;
            return Color.red;
        }

        #endregion

        #region BACKWARD COMPATIBILITY Properties

        public int FormationSlotIndex => _formationSlotIndex;
        public Vector3 FormationOffset => _formationOffset;
        public int SquadId => _squadId;
        public FormationType CurrentFormationType => _currentFormationType;
        public bool IsSquadLeader => _isSquadLeader;

        #endregion

        #region Enhanced Properties

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

        #region ENHANCED API - Leader Marking
        public void SetFormationSlot(int slotIndex)
        {
            _formationSlotIndex = slotIndex;
            _isSquadLeader = (slotIndex == 0);
            if (_isSquadLeader)
            {
                _formationRole = FormationRole.Leader;
                _formationDiscipline = Mathf.Max(_formationDiscipline, 0.9f);
            }
            else
            {
                _formationRole = DetermineFormationRole(slotIndex, _currentFormationType);
            }
        }
        
        public void SetFormationType(FormationType formationType, bool smoothTransition = true)
        {
            if (_currentFormationType != formationType)
            {
                FormationType oldType = _currentFormationType;
                _currentFormationType = formationType;
                if (!_isSquadLeader)
                {
                    _formationRole = DetermineFormationRole(_formationSlotIndex, formationType);
                }
                UpdateFormationDisciplineForType(formationType);
                HandleFormationTypeChange(oldType, formationType);
            }
        }

        #endregion

        #region Formation Role Logic
        private FormationRole DetermineFormationRole(int slotIndex, FormationType formationType)
        {
            if (slotIndex == 0) return FormationRole.Leader;
            
            switch (formationType)
            {
                case FormationType.Normal:
                    if (slotIndex <= 3) return FormationRole.FrontLine;
                    if (slotIndex <= 6) return FormationRole.Support;
                    return FormationRole.Flanker;
                    
                case FormationType.Phalanx:
                    if (slotIndex <= 4) return FormationRole.FrontLine;
                    return FormationRole.Support;
                    
                case FormationType.Testudo:
                    return FormationRole.Support;
                    
                default:
                    return FormationRole.Follower;
            }
        }

        #endregion

        #region BACKWARD COMPATIBILITY Methods

        public void SetFormationOffset(Vector3 offset, bool smoothTransition = false)
        {
            _formationOffset = offset;
            
            if (_squadCenterPosition != Vector3.zero)
            {
                _targetFormationPosition = _squadCenterPosition + (_squadRotation * _formationOffset);
            }
            
            var navigationComponent = Entity?.GetComponent<NavigationComponent>();
            if (navigationComponent)
            {
                navigationComponent.UpdateFormationOffset(_formationOffset);
            }
        }

        public void SetSquadId(int squadId)
        {
            _squadId = squadId;
        }

        #endregion

        #region Methods

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

        #region Formation Queries

        public float GetFormationEffectiveness()
        {
            if (!_isInFormationPosition)
                return 0.5f * _formationDiscipline;
                
            return _formationDiscipline;
        }

        #endregion

        #region Component Lifecycle

        public override void Initialize()
        {
            base.Initialize();
            InitializeDefaultsForUnitType(unitInfo.GetUnitType());
            if (_formationOffset == Vector3.zero)
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
            _isSquadLeader = false;
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

        #region Editor-Only Debug Gizmos - ENHANCED

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !_enableDebugGizmos) return;
            
            // Draw slot number
            if (_showSlotNumber && _formationSlotIndex >= 0)
            {
                Vector3 textPos = transform.position + Vector3.up * 2.5f;
                string slotText = _isSquadLeader ? $"LEADER (Slot {_formationSlotIndex})" : $"Slot {_formationSlotIndex}";
                UnityEditor.Handles.Label(textPos, slotText);
            }
            
            // Draw leader crown indicator
            if (_showLeaderIndicator && _isSquadLeader)
            {
                Vector3 crownPos = transform.position + Vector3.up * 3f;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(crownPos, 0.3f);
                UnityEditor.Handles.Label(crownPos + Vector3.up * 0.5f, "♔");
            }
            
            // Draw formation target position
            if (_targetFormationPosition != Vector3.zero)
            {
                Gizmos.color = _isInFormationPosition ? Color.green : Color.red;
                Gizmos.DrawWireSphere(_targetFormationPosition, 0.3f);
                Gizmos.color = _formationLineColor;
                Gizmos.DrawLine(transform.position, _targetFormationPosition);
            }
            
            // Draw squad center position
            if (_squadCenterPosition != Vector3.zero)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(_squadCenterPosition, Vector3.one * 0.5f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, _squadCenterPosition);
            }
            
            // Draw formation role icon
            Vector3 rolePos = transform.position + Vector3.up * 1.5f;
            string roleText = _formationRole switch
            {
                FormationRole.Leader => "★",
                FormationRole.FrontLine => "⚔",
                FormationRole.Support => "🛡",
                FormationRole.Flanker => "➤",
                _ => "◯"
            };
            UnityEditor.Handles.Label(rolePos, roleText);
        }
        #endif

        #endregion
    }
}