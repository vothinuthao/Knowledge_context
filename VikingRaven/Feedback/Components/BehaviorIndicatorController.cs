using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VikingRaven.Core.Behavior;
using VikingRaven.Units.Components;

namespace VikingRaven.Feedback.Components
{
    public class BehaviorIndicatorController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _behaviorText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private float _updateInterval = 0.5f;
        
        private WeightedBehaviorComponent _behaviorComponent;
        private IBehavior _currentBehavior;
        private float _updateTimer = 0f;
        
        private Dictionary<string, Color> _behaviorColors = new Dictionary<string, Color>
        {
            { "Move", new Color(0.2f, 0.7f, 0.2f, 0.7f) },             // Green
            { "Attack", new Color(0.7f, 0.2f, 0.2f, 0.7f) },           // Red
            { "Strafe", new Color(0.2f, 0.7f, 0.7f, 0.7f) },           // Cyan
            { "AmbushMove", new Color(0.4f, 0.3f, 0.7f, 0.7f) },       // Purple
            { "Surround", new Color(0.7f, 0.5f, 0.2f, 0.7f) },         // Orange
            { "Protect", new Color(0.2f, 0.2f, 0.7f, 0.7f) },          // Blue
            { "Cover", new Color(0.5f, 0.2f, 0.7f, 0.7f) },            // Violet
            { "Phalanx", new Color(0.5f, 0.5f, 0.2f, 0.7f) },          // Yellow-green
            { "Testudo", new Color(0.7f, 0.7f, 0.2f, 0.7f) },          // Yellow
            { "Charge", new Color(0.7f, 0.3f, 0.3f, 0.7f) }            // Light red
        };
        
        public void SetBehaviorComponent(WeightedBehaviorComponent behaviorComponent)
        {
            _behaviorComponent = behaviorComponent;
            
            // Initialize with current behavior
            UpdateBehaviorIndicator();
        }
        
        private void Update()
        {
            _updateTimer += Time.deltaTime;
            
            if (_updateTimer >= _updateInterval)
            {
                _updateTimer = 0f;
                UpdateBehaviorIndicator();
            }
        }
        
        private void UpdateBehaviorIndicator()
        {
            if (_behaviorComponent == null || _behaviorComponent.BehaviorManager == null)
                return;
                
            // Use reflection to get the current behavior (simplified approach)
            _currentBehavior = GetCurrentBehavior();
            
            if (_behaviorText != null && _currentBehavior != null)
            {
                _behaviorText.text = _currentBehavior.Name;
                
                // Update color based on behavior
                if (_backgroundImage != null)
                {
                    if (_behaviorColors.TryGetValue(_currentBehavior.Name, out Color color))
                    {
                        _backgroundImage.color = color;
                    }
                    else
                    {
                        _backgroundImage.color = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Default gray
                    }
                }
            }
        }
        
        private IBehavior GetCurrentBehavior()
        {
            // Get current behavior using reflection (for demonstration)
            // In a real implementation, WeightedBehaviorManager would expose the current behavior
            
            var behaviorManager = _behaviorComponent.BehaviorManager;
            var currentBehaviorField = behaviorManager.GetType().GetField("_currentBehavior", 
                BindingFlags.NonPublic | BindingFlags.Instance);
                
            if (currentBehaviorField != null)
            {
                return currentBehaviorField.GetValue(behaviorManager) as IBehavior;
            }
            
            return null;
        }
    }
}