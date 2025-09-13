using UnityEngine;
using RavenDeckbuilding.Core.Data;
using RavenDeckbuilding.Core.Interfaces;

namespace RavenDeckbuilding.Systems.Cards
{
    public enum CardTargetType
    {
        None = 0,
        Self,
        Enemy,
        Ground,
        Direction,
        Position,
        Area,
        NoTarget
    }

    /// <summary>
    /// Abstract base class for all cards with automatic registration
    /// Implements the modular card development system
    /// </summary>
    public abstract class BaseCard : MonoBehaviour
    {
        [Header("Card Properties")]
        [SerializeField] protected string cardName = "Unknown Card";
        [SerializeField] protected string cardId = "";
        [SerializeField] protected int manaCost = 1;
        [SerializeField] protected float cooldown = 0f;
        [SerializeField] protected CardTargetType targetType = CardTargetType.None;
        [SerializeField] protected int priority = 100;
        
        [Header("Visual")]
        [SerializeField] protected GameObject previewPrefab;
        [SerializeField] protected Color previewColor = Color.white;
        
        // Properties
        public string CardName => cardName;
        public string CardId => cardId;
        public int ManaCost => manaCost;
        public float Cooldown => cooldown;
        public CardTargetType TargetType => targetType;
        public int Priority => priority;
        
        // State
        protected float _lastCastTime;
        protected bool _isOnCooldown;
        protected GameObject _currentPreview;

        // Auto-generate ID if not set
        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(cardId))
            {
                cardId = $"{GetType().Name}_{GetInstanceID()}";
            }
        }

        protected virtual void OnEnable()
        {
            // Register with CardRegistry
            CardRegistry.RegisterCard(this);
        }

        protected virtual void OnDisable()
        {
            // Unregister from CardRegistry
            CardRegistry.UnregisterCard(this);
            
            // Clean up preview
            HidePreview();
        }

        protected virtual void Update()
        {
            // Update cooldown state
            if (_isOnCooldown && Time.realtimeSinceStartup - _lastCastTime >= cooldown)
            {
                _isOnCooldown = false;
            }
        }

        // Abstract interface that subclasses must implement
        public abstract bool CanCast(CastingContext context);
        public abstract ICardCommand CreateCastCommand(CastingContext context);
        
        // Virtual methods with default implementations
        public virtual void ShowPreview(CastingContext context)
        {
            if (previewPrefab != null && _currentPreview == null)
            {
                _currentPreview = Instantiate(previewPrefab, context.targetPosition, Quaternion.identity);
                
                // Set preview color
                var renderers = _currentPreview.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.material.color = previewColor;
                }
            }
        }

        public virtual void HidePreview()
        {
            if (_currentPreview != null)
            {
                DestroyImmediate(_currentPreview);
                _currentPreview = null;
            }
        }

        // Helper methods for subclasses
        protected bool IsOnCooldown()
        {
            return _isOnCooldown;
        }

        protected void StartCooldown()
        {
            _lastCastTime = Time.realtimeSinceStartup;
            _isOnCooldown = cooldown > 0f;
        }

        protected bool HasValidTarget(CastingContext context)
        {
            switch (targetType)
            {
                case CardTargetType.None:
                    return true;
                    
                case CardTargetType.Self:
                    return context.caster != null;
                    
                case CardTargetType.Enemy:
                    return context.target != null && context.target != context.caster;
                    
                case CardTargetType.Position:
                    return context.targetPosition != Vector3.zero;
                    
                case CardTargetType.Area:
                    return context.targetPosition != Vector3.zero;
                    
                default:
                    return false;
            }
        }

        protected bool HasSufficientMana(Player caster)
        {
            return caster != null && caster.CurrentMana >= manaCost;
        }

        // Inspector validation
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(cardName))
                cardName = GetType().Name;
                
            if (manaCost < 0)
                manaCost = 0;
                
            if (cooldown < 0f)
                cooldown = 0f;
        }

        // Debug information
        public virtual string GetDebugInfo()
        {
            return $"{cardName} [ID: {cardId}] - Mana: {manaCost}, Cooldown: {cooldown}s, " +
                   $"Target: {targetType}, OnCooldown: {_isOnCooldown}";
        }
    }

    /// <summary>
    /// Data structure for card execution context
    /// Contains all information needed for card casting
    /// </summary>
    [System.Serializable]
    public struct CastingContext
    {
        public Player caster;
        public Player target;
        public Vector3 targetPosition;
        public Vector3 castDirection;
        public GameState gameState;
        public float timestamp;

        public static CastingContext Create(Player caster, Vector3 targetPos, GameState gameState)
        {
            return new CastingContext
            {
                caster = caster,
                target = null,
                targetPosition = targetPos,
                castDirection = Vector3.forward,
                gameState = gameState,
                timestamp = Time.realtimeSinceStartup
            };
        }

        public static CastingContext Create(Player caster, Player target, GameState gameState)
        {
            return new CastingContext
            {
                caster = caster,
                target = target,
                targetPosition = target?.transform.position ?? Vector3.zero,
                castDirection = (target?.transform.position - caster?.transform.position ?? Vector3.forward).normalized,
                gameState = gameState,
                timestamp = Time.realtimeSinceStartup
            };
        }

        public bool IsValid()
        {
            return caster != null && gameState != null;
        }
    }
}