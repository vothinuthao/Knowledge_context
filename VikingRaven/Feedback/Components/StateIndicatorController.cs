using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VikingRaven.Core.StateMachine;
using VikingRaven.Units.Components;

namespace VikingRaven.Feedback.Components
{
    public class StateIndicatorController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _stateText;
        [SerializeField] private Image _backgroundImage;
        
        private StateComponent _stateComponent;
        private IState _lastState;
        
        public void SetStateComponent(StateComponent stateComponent)
        {
            _stateComponent = stateComponent;
            _lastState = _stateComponent.CurrentState;
            
            // Update initial state
            UpdateStateIndicator();
        }
        
        private void Update()
        {
            if (_stateComponent != null && _stateComponent.CurrentState != _lastState)
            {
                _lastState = _stateComponent.CurrentState;
                UpdateStateIndicator();
            }
        }
        
        private void UpdateStateIndicator()
        {
            if (_stateText != null && _stateComponent.CurrentState != null)
            {
                string stateName = _stateComponent.CurrentState.GetType().Name;
                
                // Remove "State" suffix if present
                if (stateName.EndsWith("State"))
                {
                    stateName = stateName.Substring(0, stateName.Length - 5);
                }
                
                _stateText.text = stateName;
                
                // Update color based on state
                if (_backgroundImage != null)
                {
                    if (stateName == "Idle")
                    {
                        _backgroundImage.color = new Color(0.2f, 0.2f, 0.8f, 0.7f); // Blue
                    }
                    else if (stateName == "Aggro")
                    {
                        _backgroundImage.color = new Color(0.8f, 0.2f, 0.2f, 0.7f); // Red
                    }
                    else if (stateName == "Knockback")
                    {
                        _backgroundImage.color = new Color(0.8f, 0.8f, 0.2f, 0.7f); // Yellow
                    }
                    else if (stateName == "Stun")
                    {
                        _backgroundImage.color = new Color(0.8f, 0.5f, 0.2f, 0.7f); // Orange
                    }
                    else
                    {
                        _backgroundImage.color = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Gray
                    }
                }
            }
        }
    }
}