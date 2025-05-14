using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Steering;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Component that manages a unit's position within a formation
    /// Enhanced to maintain formation integrity and smooth transitions
    /// </summary>
    public class FormationComponent : BaseComponent
    {
        [Tooltip("Index of the unit's slot within the formation")]
        [SerializeField] private int _formationSlotIndex;
        
        [Tooltip("Unit's position offset relative to squad center")]
        [SerializeField] private Vector3 _formationOffset;
        
        [Tooltip("ID of the squad this unit belongs to")]
        [SerializeField] private int _squadId;
        
        [Tooltip("Current formation type")]
        [SerializeField] private FormationType _currentFormationType = FormationType.Line;
        
        [Header("Enhanced Formation Parameters")]
        [Tooltip("How closely the unit maintains its formation position")]
        [SerializeField] private float _formationDiscipline = 0.8f;
        
        [Tooltip("Delay before changing position after formation change")]
        [SerializeField] private float _formationChangeDelay = 0.2f;
        
        // Transition tracking
        private bool _isTransitioning = false;
        private Vector3 _previousOffset;
        private Vector3 _targetOffset;
        private float _transitionStartTime;
        private float _transitionDuration = 0.75f;
        
        // Properties for external access
        public int FormationSlotIndex => _formationSlotIndex;
        public Vector3 FormationOffset => _isTransitioning ? 
            CalculateTransitionOffset() : _formationOffset;
        public int SquadId => _squadId;
        public FormationType CurrentFormationType => _currentFormationType;
        public float FormationDiscipline => _formationDiscipline;
        public bool IsTransitioning => _isTransitioning;

        private void Update()
        {
            if (_isTransitioning)
            {
                UpdateTransition();
            }
        }
        
        /// <summary>
        /// Update position during formation transition
        /// </summary>
        private void UpdateTransition()
        {
            float elapsed = Time.time - _transitionStartTime;
            float t = Mathf.Clamp01(elapsed / _transitionDuration);
            
            // Smooth step transition for more natural movement
            float smoothT = t * t * (3f - 2f * t);
            
            if (t >= 1.0f)
            {
                // Transition complete
                _isTransitioning = false;
                _formationOffset = _targetOffset;
                
                // Update navigation with final position
                var navigationComponent = Entity?.GetComponent<NavigationComponent>();
                if (navigationComponent != null)
                {
                    navigationComponent.UpdateFormationOffset(_formationOffset);
                }
            }
        }
        
        /// <summary>
        /// Calculate current offset during transition
        /// </summary>
        private Vector3 CalculateTransitionOffset()
        {
            if (!_isTransitioning)
                return _formationOffset;
                
            float elapsed = Time.time - _transitionStartTime;
            float t = Mathf.Clamp01(elapsed / _transitionDuration);
            
            // Smooth step transition
            float smoothT = t * t * (3f - 2f * t);
            
            // Interpolate between previous and target offsets
            return Vector3.Lerp(_previousOffset, _targetOffset, smoothT);
        }

        /// <summary>
        /// Set the unit's slot index in formation
        /// </summary>
        public void SetFormationSlot(int slotIndex)
        {
            _formationSlotIndex = slotIndex;
        }

        /// <summary>
        /// Set the unit's offset position in formation with smooth transition option
        /// </summary>
        public void SetFormationOffset(Vector3 offset, bool smoothTransition = false)
        {
            if (smoothTransition && _formationOffset != Vector3.zero)
            {
                // Start smooth transition
                _previousOffset = _formationOffset;
                _targetOffset = offset;
                _transitionStartTime = Time.time;
                _isTransitioning = true;
                
                // Adjust transition duration based on distance
                float distance = Vector3.Distance(_previousOffset, _targetOffset);
                _transitionDuration = Mathf.Clamp(distance * 0.25f, 0.3f, 0.75f);
            }
            else
            {
                // Immediate update
                _formationOffset = offset;
                
                // Update navigation
                var navigationComponent = Entity?.GetComponent<NavigationComponent>();
                if (navigationComponent != null)
                {
                    navigationComponent.UpdateFormationOffset(_formationOffset);
                }
            }
        }

        /// <summary>
        /// Set squad ID
        /// </summary>
        public void SetSquadId(int squadId)
        {
            _squadId = squadId;
        }

        /// <summary>
        /// Set formation type with smooth transition option
        /// </summary>
        public void SetFormationType(FormationType formationType, bool smoothTransition = true)
        {
            if (_currentFormationType == formationType)
                return;
                
            FormationType oldType = _currentFormationType;
            _currentFormationType = formationType;
            
            // If smooth transition is enabled, delay updating NavigationComponent
            if (smoothTransition)
            {
                Invoke("NotifyFormationTypeChanged", _formationChangeDelay);
            }
            else
            {
                NotifyFormationTypeChanged();
            }
            
            // Notify any listeners about type change
            var stateComponent = Entity?.GetComponent<StateComponent>();
            if (stateComponent != null && stateComponent.CurrentState != null)
            {
                // Different behavior based on state and formation type
                switch (_currentFormationType)
                {
                    case FormationType.Phalanx:
                        // Special behavior for Phalanx formation
                        var animationComponent = Entity?.GetComponent<AnimationComponent>();
                        if (animationComponent != null)
                        {
                            animationComponent.PlayAnimation("PhalanxStance");
                        }
                        break;
                        
                    case FormationType.Testudo:
                        // Special behavior for Testudo formation
                        animationComponent = Entity?.GetComponent<AnimationComponent>();
                        if (animationComponent != null)
                        {
                            animationComponent.PlayAnimation("TestudoStance");
                        }
                        break;
                        
                    default:
                        // Return to normal stance if coming from special formation
                        if (oldType == FormationType.Phalanx || oldType == FormationType.Testudo)
                        {
                            animationComponent = Entity?.GetComponent<AnimationComponent>();
                            if (animationComponent != null)
                            {
                                animationComponent.PlayAnimation("NormalStance");
                            }
                        }
                        break;
                }
            }
        }
        
        /// <summary>
        /// Notify other components of formation type change
        /// </summary>
        private void NotifyFormationTypeChanged()
        {
            // Update steering behaviors if present
            var steeringComponent = Entity?.GetComponent<SteeringComponent>();
            if (steeringComponent != null && steeringComponent.SteeringManager != null)
            {
                var behaviors = GetSteeringBehaviors(steeringComponent.SteeringManager);
                foreach (var behavior in behaviors)
                {
                    switch (_currentFormationType)
                    {
                        case FormationType.Phalanx:
                            if (behavior.Name == "Separation") behavior.Weight = 0.5f;
                            if (behavior.Name == "Cohesion") behavior.Weight = 1.2f;
                            break;
                    
                        case FormationType.Testudo:
                            if (behavior.Name == "Separation") behavior.Weight = 0.2f;
                            if (behavior.Name == "Cohesion") behavior.Weight = 1.5f;
                            break;
                    
                        case FormationType.Circle:
                            if (behavior.Name == "Separation") behavior.Weight = 0.8f;
                            if (behavior.Name == "Cohesion") behavior.Weight = 0.8f;
                            break;
                    
                        default:
                            if (behavior.Name == "Separation") behavior.Weight = 1.0f;
                            if (behavior.Name == "Cohesion") behavior.Weight = 1.0f;
                            break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Get formation discipline multiplier based on current formation type
        /// </summary>
        public float GetFormationDisciplineMultiplier()
        {
            switch (_currentFormationType)
            {
                case FormationType.Phalanx:
                    return 1.2f; // Tighter discipline for phalanx
                    
                case FormationType.Testudo:
                    return 1.5f; // Strictest discipline for testudo
                    
                case FormationType.Circle:
                    return 0.9f; // Medium discipline for circle
                    
                case FormationType.Line:
                    return 0.8f; // Standard discipline for line
                    
                case FormationType.Column:
                    return 0.8f; // Standard discipline for column
                    
                default:
                    return 1.0f; // Default value
            }
        }
        
        /// <summary>
        /// Calculate target position based on squad center and formation offset
        /// </summary>
        public Vector3 CalculateFormationPosition(Vector3 squadCenter, Quaternion squadRotation)
        {
            // Apply rotation to offset
            Vector3 rotatedOffset = squadRotation * FormationOffset;
            
            // Calculate world position
            return squadCenter + rotatedOffset;
        }
        
        /// <summary>
        /// Initialize component
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            if (_formationOffset == Vector3.zero)
            {
                _formationOffset = new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));
            }
    
            // Gán các callbacks
            var healthComponent = Entity?.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.OnDamageTaken += (amount, source) => {
                    var stateComponent = Entity?.GetComponent<StateComponent>();
                    if (stateComponent != null && stateComponent.StateMachineInGame != null)
                    {
                    }
                };
            }
        }
        
        /// <summary>
        /// Clean up component
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();
        }
        private List<ISteeringBehavior> GetSteeringBehaviors(SteeringManager steeringManager)
        {
            List<ISteeringBehavior> behaviors = new List<ISteeringBehavior>();
    
            var field = steeringManager.GetType().GetField("_behaviors", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
        
            if (field != null)
            {
                var fieldValue = field.GetValue(steeringManager) as List<ISteeringBehavior>;
                if (fieldValue != null)
                {
                    behaviors = fieldValue;
                }
            }
    
            return behaviors;
        }
    }
}