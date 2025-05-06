using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Events;
using VikingRaven.Feedback.Components;
using VikingRaven.Units.Components;

namespace VikingRaven.Feedback.Systems
{
    public class FeedbackSystem : BaseSystem
    {
        [SerializeField] private GameObject _healthBarPrefab;
        [SerializeField] private GameObject _stateIndicatorPrefab;
        [SerializeField] private GameObject _behaviorIndicatorPrefab;
        [SerializeField] private GameObject _tacticalRoleIndicatorPrefab;
        
        [SerializeField] private bool _createMissingFeedbackComponents = true;
        
        public override void Initialize()
        {
            // Listen for entity creation events to add feedback components
            EventManager.Instance.RegisterListener<SquadCreatedEvent>(OnSquadCreated);
        }
        
        public override void Execute()
        {
            if (_createMissingFeedbackComponents)
            {
                // Create feedback components for entities that don't have them
                CreateMissingFeedbackComponents();
            }
        }
        
        private void CreateMissingFeedbackComponents()
        {
            // Get all entities that should have feedback
            var entities = EntityRegistry.GetEntitiesWithComponent<HealthComponent>();
            
            foreach (var entity in entities)
            {
                var feedbackComponent = entity.GetComponent<VisualFeedbackComponent>();
                
                if (feedbackComponent == null)
                {
                    // Entity doesn't have feedback, add it
                    var entityObject = (entity as MonoBehaviour)?.gameObject;
                    
                    if (entityObject != null)
                    {
                        feedbackComponent = entityObject.AddComponent<VisualFeedbackComponent>();
                        
                        // Set prefabs
                        var field = feedbackComponent.GetType().GetField("_healthBarPrefab", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                            field.SetValue(feedbackComponent, _healthBarPrefab);
                            
                        field = feedbackComponent.GetType().GetField("_stateIndicatorPrefab", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                            field.SetValue(feedbackComponent, _stateIndicatorPrefab);
                            
                        field = feedbackComponent.GetType().GetField("_behaviorIndicatorPrefab", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                            field.SetValue(feedbackComponent, _behaviorIndicatorPrefab);
                            
                        field = feedbackComponent.GetType().GetField("_tacticalRoleIndicatorPrefab", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                            field.SetValue(feedbackComponent, _tacticalRoleIndicatorPrefab);
                        
                        // Initialize
                        feedbackComponent.Initialize();
                    }
                }
            }
        }
        
        private void OnSquadCreated(SquadCreatedEvent squadEvent)
        {
            if (_createMissingFeedbackComponents)
            {
                // Add feedback components to new squad units
                foreach (var entity in squadEvent.Units)
                {
                    var feedbackComponent = entity.GetComponent<VisualFeedbackComponent>();
                    
                    if (feedbackComponent == null)
                    {
                        // Entity doesn't have feedback, add it
                        var entityObject = (entity as MonoBehaviour)?.gameObject;
                        
                        if (entityObject != null)
                        {
                            feedbackComponent = entityObject.AddComponent<VisualFeedbackComponent>();
                            
                            // Set prefabs
                            var field = feedbackComponent.GetType().GetField("_healthBarPrefab", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (field != null)
                                field.SetValue(feedbackComponent, _healthBarPrefab);
                                
                            field = feedbackComponent.GetType().GetField("_stateIndicatorPrefab", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (field != null)
                                field.SetValue(feedbackComponent, _stateIndicatorPrefab);
                                
                            field = feedbackComponent.GetType().GetField("_behaviorIndicatorPrefab", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (field != null)
                                field.SetValue(feedbackComponent, _behaviorIndicatorPrefab);
                                
                            field = feedbackComponent.GetType().GetField("_tacticalRoleIndicatorPrefab", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (field != null)
                                field.SetValue(feedbackComponent, _tacticalRoleIndicatorPrefab);
                            
                            // Initialize
                            feedbackComponent.Initialize();
                        }
                    }
                }
            }
        }
    }
}