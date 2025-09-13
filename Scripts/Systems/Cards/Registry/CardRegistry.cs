using System.Collections.Generic;
using System;
using UnityEngine;

namespace RavenDeckbuilding.Systems.Cards
{
    /// <summary>
    /// Static registry for card management with automatic registration and fast lookup
    /// Provides O(1) lookups and efficient iteration for card systems
    /// </summary>
    public static class CardRegistry
    {
        // Fast lookup dictionary for card instances
        private static readonly Dictionary<string, BaseCard> _cardLookup = new Dictionary<string, BaseCard>();
        
        // Type registry for runtime instantiation
        private static readonly Dictionary<string, Type> _cardTypes = new Dictionary<string, Type>();
        
        // Cached array for efficient iteration - updated when cards are added/removed
        private static BaseCard[] _allCardsCache = new BaseCard[0];
        private static bool _cacheNeedsUpdate = false;
        
        // Event callbacks for card registration changes
        private static event Action<BaseCard> OnCardRegistered;
        private static event Action<BaseCard> OnCardUnregistered;

        public static int RegisteredCardCount => _cardLookup.Count;
        public static IReadOnlyDictionary<string, BaseCard> AllCards => _cardLookup;

        /// <summary>
        /// Register a card instance for fast lookup
        /// Called automatically by BaseCard.OnEnable()
        /// </summary>
        public static bool RegisterCard(BaseCard card)
        {
            if (card == null)
            {
                Debug.LogError("CardRegistry: Attempted to register null card");
                return false;
            }

            if (string.IsNullOrEmpty(card.CardId))
            {
                Debug.LogError($"CardRegistry: Card {card.name} has empty CardId");
                return false;
            }

            // Check for duplicate IDs
            if (_cardLookup.ContainsKey(card.CardId))
            {
                Debug.LogWarning($"CardRegistry: Card with ID '{card.CardId}' already registered. Overwriting.");
            }

            // Register the card instance
            _cardLookup[card.CardId] = card;
            
            // Register the card type for instantiation
            string typeName = card.GetType().Name;
            _cardTypes[typeName] = card.GetType();
            
            // Mark cache for update
            _cacheNeedsUpdate = true;
            
            // Fire event
            OnCardRegistered?.Invoke(card);
            
            if (Application.isPlaying && Debug.isDebugBuild)
            {
                Debug.Log($"CardRegistry: Registered card '{card.CardName}' [ID: {card.CardId}]");
            }

            return true;
        }

        /// <summary>
        /// Unregister a card instance
        /// Called automatically by BaseCard.OnDisable()
        /// </summary>
        public static bool UnregisterCard(BaseCard card)
        {
            if (card == null || string.IsNullOrEmpty(card.CardId))
                return false;

            if (_cardLookup.Remove(card.CardId))
            {
                _cacheNeedsUpdate = true;
                OnCardUnregistered?.Invoke(card);
                
                if (Application.isPlaying && Debug.isDebugBuild)
                {
                    Debug.Log($"CardRegistry: Unregistered card '{card.CardName}' [ID: {card.CardId}]");
                }
                
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get card by ID with O(1) lookup
        /// </summary>
        public static BaseCard GetCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return null;

            _cardLookup.TryGetValue(cardId, out BaseCard card);
            return card;
        }

        /// <summary>
        /// Get all registered cards as array for efficient iteration
        /// Array is cached and only updated when cards are added/removed
        /// </summary>
        public static BaseCard[] GetAllCards()
        {
            if (_cacheNeedsUpdate)
            {
                UpdateCache();
            }
            
            return _allCardsCache;
        }

        /// <summary>
        /// Get cards by type name
        /// </summary>
        public static List<BaseCard> GetCardsByType<T>() where T : BaseCard
        {
            return GetCardsByType(typeof(T));
        }

        /// <summary>
        /// Get cards by type
        /// </summary>
        public static List<BaseCard> GetCardsByType(Type cardType)
        {
            var results = new List<BaseCard>();
            
            foreach (var card in _cardLookup.Values)
            {
                if (card.GetType() == cardType || card.GetType().IsSubclassOf(cardType))
                {
                    results.Add(card);
                }
            }
            
            return results;
        }

        /// <summary>
        /// Get cards by mana cost
        /// </summary>
        public static List<BaseCard> GetCardsByManaCost(int manaCost)
        {
            var results = new List<BaseCard>();
            
            foreach (var card in _cardLookup.Values)
            {
                if (card.ManaCost == manaCost)
                {
                    results.Add(card);
                }
            }
            
            return results;
        }

        /// <summary>
        /// Get cards that can be cast with given context
        /// </summary>
        public static List<BaseCard> GetCastableCards(CastingContext context)
        {
            var results = new List<BaseCard>();
            
            foreach (var card in _cardLookup.Values)
            {
                if (card.CanCast(context))
                {
                    results.Add(card);
                }
            }
            
            return results;
        }

        /// <summary>
        /// Create new card instance by type name
        /// Useful for loading cards from data or network
        /// </summary>
        public static T CreateCard<T>(string typeName) where T : BaseCard
        {
            if (_cardTypes.TryGetValue(typeName, out Type cardType))
            {
                if (cardType.IsSubclassOf(typeof(T)) || cardType == typeof(T))
                {
                    GameObject cardObject = new GameObject($"Card_{typeName}");
                    return cardObject.AddComponent(cardType) as T;
                }
            }
            
            Debug.LogError($"CardRegistry: Card type '{typeName}' not found or is not of type {typeof(T).Name}");
            return null;
        }

        /// <summary>
        /// Check if card ID is already registered
        /// </summary>
        public static bool IsCardIdRegistered(string cardId)
        {
            return !string.IsNullOrEmpty(cardId) && _cardLookup.ContainsKey(cardId);
        }

        /// <summary>
        /// Find cards by name (can be multiple with same name)
        /// </summary>
        public static List<BaseCard> FindCardsByName(string cardName)
        {
            var results = new List<BaseCard>();
            
            foreach (var card in _cardLookup.Values)
            {
                if (string.Equals(card.CardName, cardName, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(card);
                }
            }
            
            return results;
        }

        /// <summary>
        /// Get registered card types for editor tools or debugging
        /// </summary>
        public static string[] GetRegisteredCardTypes()
        {
            var types = new string[_cardTypes.Count];
            _cardTypes.Keys.CopyTo(types, 0);
            return types;
        }

        /// <summary>
        /// Subscribe to card registration events
        /// </summary>
        public static void SubscribeToRegistration(Action<BaseCard> onRegistered, Action<BaseCard> onUnregistered = null)
        {
            if (onRegistered != null)
                OnCardRegistered += onRegistered;
                
            if (onUnregistered != null)
                OnCardUnregistered += onUnregistered;
        }

        /// <summary>
        /// Unsubscribe from card registration events
        /// </summary>
        public static void UnsubscribeFromRegistration(Action<BaseCard> onRegistered, Action<BaseCard> onUnregistered = null)
        {
            if (onRegistered != null)
                OnCardRegistered -= onRegistered;
                
            if (onUnregistered != null)
                OnCardUnregistered -= onUnregistered;
        }

        /// <summary>
        /// Clear all registered cards (for testing or scene cleanup)
        /// </summary>
        public static void ClearRegistry()
        {
            var cardsToRemove = new List<BaseCard>(_cardLookup.Values);
            
            _cardLookup.Clear();
            _cardTypes.Clear();
            _cacheNeedsUpdate = true;
            
            // Fire unregistered events
            foreach (var card in cardsToRemove)
            {
                OnCardUnregistered?.Invoke(card);
            }
            
            Debug.Log("CardRegistry: Cleared all registered cards");
        }

        /// <summary>
        /// Update the cached array of all cards
        /// </summary>
        private static void UpdateCache()
        {
            _allCardsCache = new BaseCard[_cardLookup.Count];
            int index = 0;
            
            foreach (var card in _cardLookup.Values)
            {
                _allCardsCache[index++] = card;
            }
            
            _cacheNeedsUpdate = false;
        }

        /// <summary>
        /// Get debug information about the registry state
        /// </summary>
        public static string GetDebugInfo()
        {
            return $"CardRegistry: {RegisteredCardCount} cards registered, " +
                   $"{_cardTypes.Count} types known, Cache valid: {!_cacheNeedsUpdate}";
        }

        /// <summary>
        /// Validate registry integrity (for debugging)
        /// </summary>
        public static bool ValidateRegistry()
        {
            bool isValid = true;
            
            foreach (var kvp in _cardLookup)
            {
                if (kvp.Value == null)
                {
                    Debug.LogError($"CardRegistry: Null card found for ID '{kvp.Key}'");
                    isValid = false;
                }
                else if (kvp.Value.CardId != kvp.Key)
                {
                    Debug.LogError($"CardRegistry: Card ID mismatch - Key: '{kvp.Key}', Card ID: '{kvp.Value.CardId}'");
                    isValid = false;
                }
            }
            
            return isValid;
        }

        // Editor helper methods
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Cards/Validate Registry")]
        private static void ValidateRegistryMenuItem()
        {
            if (ValidateRegistry())
            {
                Debug.Log("CardRegistry validation passed!");
            }
        }

        [UnityEditor.MenuItem("Tools/Cards/Clear Registry")]
        private static void ClearRegistryMenuItem()
        {
            ClearRegistry();
        }

        [UnityEditor.MenuItem("Tools/Cards/Print Registry Info")]
        private static void PrintRegistryInfo()
        {
            Debug.Log(GetDebugInfo());
            
            if (RegisteredCardCount > 0)
            {
                Debug.Log("Registered Cards:");
                foreach (var card in _cardLookup.Values)
                {
                    Debug.Log($"  - {card.GetDebugInfo()}");
                }
            }
        }
#endif
    }
}