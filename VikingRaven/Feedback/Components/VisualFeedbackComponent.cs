using UnityEngine;
using VikingRaven.Combat.Components;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Feedback.Components
{
     public class VisualFeedbackComponent : BaseComponent
    {
        [SerializeField] private bool _showStateIndicator = true;
        [SerializeField] private bool _showHealthBar = true;
        [SerializeField] private bool _showBehaviorIndicator = true;
        [SerializeField] private bool _showTacticalRole = true;
        
        [SerializeField] private Transform _feedbackParent;
        [SerializeField] private Vector3 _feedbackOffset = new Vector3(0, 2f, 0);
        
        [SerializeField] private GameObject _healthBarPrefab;
        [SerializeField] private GameObject _stateIndicatorPrefab;
        [SerializeField] private GameObject _behaviorIndicatorPrefab;
        [SerializeField] private GameObject _tacticalRoleIndicatorPrefab;
        
        private GameObject _healthBar;
        private GameObject _stateIndicator;
        private GameObject _behaviorIndicator;
        private GameObject _tacticalRoleIndicator;
        
        private HealthBarController _healthBarController;
        private StateIndicatorController _stateIndicatorController;
        private BehaviorIndicatorController _behaviorIndicatorController;
        private TacticalRoleIndicatorController _tacticalRoleController;
        
        public override void Initialize()
        {
            CreateFeedbackParent();
            
            if (_showHealthBar)
            {
                CreateHealthBar();
            }
            
            if (_showStateIndicator)
            {
                CreateStateIndicator();
            }
            
            if (_showBehaviorIndicator)
            {
                CreateBehaviorIndicator();
            }
            
            if (_showTacticalRole)
            {
                CreateTacticalRoleIndicator();
            }
        }
        
        private void CreateFeedbackParent()
        {
            if (_feedbackParent == null)
            {
                GameObject feedbackParentObj = new GameObject("FeedbackParent");
                feedbackParentObj.transform.SetParent(transform);
                feedbackParentObj.transform.localPosition = _feedbackOffset;
                _feedbackParent = feedbackParentObj.transform;
            }
        }
        
        private void CreateHealthBar()
        {
            if (_healthBarPrefab != null)
            {
                _healthBar = Instantiate(_healthBarPrefab, _feedbackParent);
                _healthBarController = _healthBar.GetComponent<HealthBarController>();
                
                if (_healthBarController != null)
                {
                    // Connect to health component
                    var healthComponent = Entity.GetComponent<HealthComponent>();
                    if (healthComponent != null)
                    {
                        _healthBarController.SetHealthComponent(healthComponent);
                    }
                }
            }
        }
        
        private void CreateStateIndicator()
        {
            if (_stateIndicatorPrefab != null)
            {
                _stateIndicator = Instantiate(_stateIndicatorPrefab, _feedbackParent);
                _stateIndicatorController = _stateIndicator.GetComponent<StateIndicatorController>();
                
                if (_stateIndicatorController != null)
                {
                    // Connect to state component
                    var stateComponent = Entity.GetComponent<StateComponent>();
                    if (stateComponent != null)
                    {
                        _stateIndicatorController.SetStateComponent(stateComponent);
                    }
                }
            }
        }
        
        private void CreateBehaviorIndicator()
        {
            if (_behaviorIndicatorPrefab != null)
            {
                _behaviorIndicator = Instantiate(_behaviorIndicatorPrefab, _feedbackParent);
                _behaviorIndicatorController = _behaviorIndicator.GetComponent<BehaviorIndicatorController>();
                
                if (_behaviorIndicatorController != null)
                {
                    // Connect to behavior component
                    var behaviorComponent = Entity.GetComponent<WeightedBehaviorComponent>();
                    if (behaviorComponent != null)
                    {
                        _behaviorIndicatorController.SetBehaviorComponent(behaviorComponent);
                    }
                }
            }
        }
        
        private void CreateTacticalRoleIndicator()
        {
            if (_tacticalRoleIndicatorPrefab != null)
            {
                _tacticalRoleIndicator = Instantiate(_tacticalRoleIndicatorPrefab, _feedbackParent);
                _tacticalRoleController = _tacticalRoleIndicator.GetComponent<TacticalRoleIndicatorController>();
                
                if (_tacticalRoleController != null)
                {
                    // Connect to tactical component
                    var tacticalComponent = Entity.GetComponent<TacticalComponent>();
                    if (tacticalComponent != null)
                    {
                        _tacticalRoleController.SetTacticalComponent(tacticalComponent);
                    }
                }
            }
        }
        
        private void Update()
        {
            // Make feedback face camera
            if (_feedbackParent != null && Camera.main != null)
            {
                _feedbackParent.LookAt(_feedbackParent.position + Camera.main.transform.rotation * Vector3.forward,
                                      Camera.main.transform.rotation * Vector3.up);
            }
        }
        
        public override void Cleanup()
        {
            if (_healthBar != null)
            {
                Destroy(_healthBar);
            }
            
            if (_stateIndicator != null)
            {
                Destroy(_stateIndicator);
            }
            
            if (_behaviorIndicator != null)
            {
                Destroy(_behaviorIndicator);
            }
            
            if (_tacticalRoleIndicator != null)
            {
                Destroy(_tacticalRoleIndicator);
            }
            
            if (_feedbackParent != null)
            {
                Destroy(_feedbackParent.gameObject);
            }
        }
    }
}