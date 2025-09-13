using UnityEngine;
using System.Collections.Generic;

namespace RavenDeckbuilding.Core.Data
{
    /// <summary>
    /// Core game state management with fixed arrays and O(1) lookups
    /// Designed for zero allocation during gameplay
    /// </summary>
    public class GameState : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxPlayers = 2;
        [SerializeField] private int maxActiveCards = 20;
        [SerializeField] private int maxActiveEffects = 50;
        
        // Fixed-size arrays for predictable memory usage
        private Player[] _players;
        private BaseCard[] _activeCards;
        private ActiveEffect[] _activeEffects;
        
        // Fast lookup dictionaries
        private Dictionary<uint, Player> _playerLookup;
        private Dictionary<string, BaseCard> _cardLookup;
        private Dictionary<uint, ActiveEffect> _effectLookup;
        
        // Current state counters
        private int _activePlayerCount;
        private int _activeCardCount;
        private int _activeEffectCount;
        
        // Game flow state
        private GamePhase _currentPhase;
        private int _currentPlayerIndex;
        private float _phaseStartTime;
        private int _turnCount;
        
        // Performance tracking
        private int _stateUpdatesThisFrame;
        private float _lastUpdateTime;

        public int ActivePlayerCount => _activePlayerCount;
        public int ActiveCardCount => _activeCardCount;
        public int ActiveEffectCount => _activeEffectCount;
        public GamePhase CurrentPhase => _currentPhase;
        public Player CurrentPlayer => _currentPlayerIndex >= 0 && _currentPlayerIndex < _activePlayerCount ? 
                                      _players[_currentPlayerIndex] : null;
        public int TurnCount => _turnCount;
        public float PhaseElapsedTime => Time.realtimeSinceStartup - _phaseStartTime;

        private void Awake()
        {
            InitializeArrays();
            InitializeLookups();
            ResetState();
        }

        private void InitializeArrays()
        {
            _players = new Player[maxPlayers];
            _activeCards = new BaseCard[maxActiveCards];
            _activeEffects = new ActiveEffect[maxActiveEffects];
        }

        private void InitializeLookups()
        {
            _playerLookup = new Dictionary<uint, Player>(maxPlayers);
            _cardLookup = new Dictionary<string, BaseCard>(maxActiveCards);
            _effectLookup = new Dictionary<uint, ActiveEffect>(maxActiveEffects);
        }

        public void ResetState()
        {
            _activePlayerCount = 0;
            _activeCardCount = 0;
            _activeEffectCount = 0;
            _currentPhase = GamePhase.Setup;
            _currentPlayerIndex = -1;
            _phaseStartTime = Time.realtimeSinceStartup;
            _turnCount = 0;
            
            ClearLookups();
        }

        private void ClearLookups()
        {
            _playerLookup.Clear();
            _cardLookup.Clear();
            _effectLookup.Clear();
        }

        // Player Management
        public bool TryAddPlayer(Player player)
        {
            if (_activePlayerCount >= maxPlayers || player == null)
                return false;
                
            _players[_activePlayerCount] = player;
            _playerLookup[player.PlayerId] = player;
            _activePlayerCount++;
            
            return true;
        }

        public Player GetPlayer(uint playerId)
        {
            _playerLookup.TryGetValue(playerId, out Player player);
            return player;
        }

        public Player GetPlayerAt(int index)
        {
            if (index < 0 || index >= _activePlayerCount)
                return null;
            return _players[index];
        }

        // Card Management
        public bool TryAddActiveCard(BaseCard card)
        {
            if (_activeCardCount >= maxActiveCards || card == null)
                return false;
                
            _activeCards[_activeCardCount] = card;
            _cardLookup[card.CardId] = card;
            _activeCardCount++;
            
            return true;
        }

        public bool TryRemoveActiveCard(string cardId)
        {
            if (!_cardLookup.TryGetValue(cardId, out BaseCard card))
                return false;
                
            // Find and remove from array (compact array)
            for (int i = 0; i < _activeCardCount; i++)
            {
                if (_activeCards[i] == card)
                {
                    // Move last element to this position
                    _activeCards[i] = _activeCards[_activeCardCount - 1];
                    _activeCards[_activeCardCount - 1] = null;
                    _activeCardCount--;
                    break;
                }
            }
            
            _cardLookup.Remove(cardId);
            return true;
        }

        public BaseCard GetCard(string cardId)
        {
            _cardLookup.TryGetValue(cardId, out BaseCard card);
            return card;
        }

        // Effect Management
        public bool TryAddEffect(ActiveEffect effect)
        {
            if (_activeEffectCount >= maxActiveEffects || effect == null)
                return false;
                
            _activeEffects[_activeEffectCount] = effect;
            _effectLookup[effect.EffectId] = effect;
            _activeEffectCount++;
            
            return true;
        }

        public bool TryRemoveEffect(uint effectId)
        {
            if (!_effectLookup.TryGetValue(effectId, out ActiveEffect effect))
                return false;
                
            // Find and remove from array
            for (int i = 0; i < _activeEffectCount; i++)
            {
                if (_activeEffects[i].EffectId == effectId)
                {
                    _activeEffects[i] = _activeEffects[_activeEffectCount - 1];
                    _activeEffects[_activeEffectCount - 1] = null;
                    _activeEffectCount--;
                    break;
                }
            }
            
            _effectLookup.Remove(effectId);
            return true;
        }

        // Game Flow Management
        public void SetPhase(GamePhase newPhase)
        {
            if (newPhase != _currentPhase)
            {
                _currentPhase = newPhase;
                _phaseStartTime = Time.realtimeSinceStartup;
                OnPhaseChanged(newPhase);
            }
        }

        public void NextPlayer()
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _activePlayerCount;
            if (_currentPlayerIndex == 0)
                _turnCount++;
        }

        private void OnPhaseChanged(GamePhase newPhase)
        {
            // Notify systems of phase change
            switch (newPhase)
            {
                case GamePhase.PlayerTurn:
                    // Start turn logic
                    break;
                case GamePhase.Combat:
                    // Combat phase logic
                    break;
                case GamePhase.EndTurn:
                    // End turn cleanup
                    break;
            }
        }

        // Validation Methods for Commands
        public bool IsValidCardPlay(string cardId, uint playerId)
        {
            Player player = GetPlayer(playerId);
            BaseCard card = GetCard(cardId);
            
            return player != null && card != null && 
                   player.CanPlayCard(card) && 
                   _currentPhase == GamePhase.PlayerTurn;
        }

        public bool IsValidTarget(Vector3 targetPosition)
        {
            // Implement target validation logic
            return true; // Placeholder
        }

        public bool HasResources(uint playerId, int cost)
        {
            Player player = GetPlayer(playerId);
            return player != null && player.CurrentMana >= cost;
        }

        // Performance monitoring
        public void IncrementStateUpdates()
        {
            _stateUpdatesThisFrame++;
        }

        private void LateUpdate()
        {
            _stateUpdatesThisFrame = 0;
            _lastUpdateTime = Time.realtimeSinceStartup;
        }

        // Debug information
        public string GetStateInfo()
        {
            return $"Phase: {_currentPhase}, Players: {_activePlayerCount}, " +
                   $"Cards: {_activeCardCount}, Effects: {_activeEffectCount}, Turn: {_turnCount}";
        }
    }

    public enum GamePhase
    {
        Setup,
        PlayerTurn,
        Combat,
        EndTurn,
        GameOver
    }

    // Placeholder classes - would be implemented elsewhere
    public class Player : MonoBehaviour
    {
        public uint PlayerId { get; set; }
        public int CurrentMana { get; set; }
        public bool CanPlayCard(BaseCard card) => CurrentMana >= card.ManaCost;
    }

    public class BaseCard : MonoBehaviour
    {
        public string CardId { get; set; }
        public int ManaCost { get; set; }
    }

    public class ActiveEffect
    {
        public uint EffectId { get; set; }
        public float Duration { get; set; }
        public float RemainingTime { get; set; }
    }
}