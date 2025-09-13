using UnityEngine;
using System.Collections.Generic;

namespace RavenDeckbuilding.Core
{
    /// <summary>
    /// Represents a card entity with component-based architecture
    /// </summary>
    public class CardEntity : MonoBehaviour
    {
        [Header("Card Settings")]
        [SerializeField] private int cardId;
        [SerializeField] private string cardName;
        [SerializeField] private int cost;
        
        private List<ICardComponent> _components;
        
        public int CardId => cardId;
        public string CardName => cardName;
        public int Cost => cost;
        public bool IsInitialized { get; private set; }
        
        void Awake()
        {
            _components = new List<ICardComponent>();
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            // Initialize all card components
            foreach (var component in _components)
            {
                component.Initialize(this);
            }
            
            // Sort by execution priority
            _components.Sort((a, b) => a.ExecutionPriority.CompareTo(b.ExecutionPriority));
            
            IsInitialized = true;
        }
        
        public void AddComponent(ICardComponent component)
        {
            _components.Add(component);
            if (IsInitialized)
            {
                component.Initialize(this);
                _components.Sort((a, b) => a.ExecutionPriority.CompareTo(b.ExecutionPriority));
            }
        }
        
        public void RemoveComponent(ICardComponent component)
        {
            if (_components.Remove(component))
            {
                component.Cleanup();
            }
        }
        
        public void ExecuteCard(in GameContext context)
        {
            foreach (var component in _components)
            {
                if (component.IsActiveThisFrame)
                {
                    component.Execute(in context);
                }
            }
        }
        
        public new T GetComponent<T>() where T : class, ICardComponent
        {
            foreach (var component in _components)
            {
                if (component is T targetComponent)
                    return targetComponent;
            }
            return null;
        }
        
        void OnDestroy()
        {
            foreach (var component in _components)
            {
                component.Cleanup();
            }
            _components.Clear();
        }
    }
}