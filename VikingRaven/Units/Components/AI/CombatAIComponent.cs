using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;
using VikingRaven.Units.Team;

namespace VikingRaven.Units.AI
{
    /// <summary>
    /// AI Component for automatic combat behavior
    /// Handles enemy detection, movement, and attack coordination
    /// </summary>
    public class CombatAIComponent : BaseComponent
    {
        #region AI Configuration
        
        [TitleGroup("AI Combat Settings")]
        [InfoBox("AI settings for automatic combat behavior", InfoMessageType.Info)]
        
        [Tooltip("Enable automatic combat AI")]
        [SerializeField] private bool _enableCombatAI = true;
        
        [Tooltip("Update frequency for AI decisions (lower = more responsive)")]
        [SerializeField, Range(0.1f, 1f)] private float _aiUpdateInterval = 0.2f;
        
        [Tooltip("Aggression level (affects targeting priority)")]
        [SerializeField, Range(0.1f, 2f)] private float _aggressionLevel = 1f;
        
        [Tooltip("Minimum distance to maintain from target when attacking")]
        [SerializeField, Range(0.5f, 3f)] private float _combatDistance = 1.5f;
        
        #endregion
        
        #region Combat Stats (From UnitDataSO)
        
        [TitleGroup("Combat Stats")]
        [InfoBox("Stats loaded from UnitDataSO", InfoMessageType.None)]
        
        [ShowInInspector, ReadOnly] private float _detectionRange;
        [ShowInInspector, ReadOnly] private float _attackRange;
        [ShowInInspector, ReadOnly] private float _moveSpeed;
        [ShowInInspector, ReadOnly] private float _damage;
        
        #endregion
        
        #region AI State
        
        [TitleGroup("AI State")]
        [ShowInInspector, ReadOnly] private CombatAIState _currentState = CombatAIState.Idle;
        [ShowInInspector, ReadOnly] private Transform _currentTarget;
        [ShowInInspector, ReadOnly] private float _distanceToTarget;
        [ShowInInspector, ReadOnly] private bool _isMovingToTarget;
        [ShowInInspector, ReadOnly] private Vector3 _lastKnownTargetPosition;
        
        #endregion
        
        #region Dependencies
        
        private CombatComponent _combatComponent;
        private TransformComponent _transformComponent;
        private UnitDataSO _unitData;
        private List<Transform> _detectedEnemies = new List<Transform>();
        private float _lastAIUpdateTime;
        
        #endregion
        
        #region Unity Lifecycle
        
        public override void Initialize()
        {
            base.Initialize();
            
            // Get required components
            _combatComponent = Entity.GetComponent<CombatComponent>();
            _transformComponent = Entity.GetComponent<TransformComponent>();
            
            if (_combatComponent == null)
            {
                Debug.LogError($"CombatAIComponent requires CombatComponent on {gameObject.name}");
                enabled = false;
                return;
            }
            
            LoadStatsFromUnitData();
            SetState(CombatAIState.Idle);
            
            Debug.Log($"CombatAI initialized for {Entity.Id}");
        }
        
        private void Update()
        {
            if (!_enableCombatAI) return;
            
            // Throttle AI updates for performance
            if (Time.time - _lastAIUpdateTime < _aiUpdateInterval) return;
            _lastAIUpdateTime = Time.time;
            
            UpdateAI();
        }
        
        #endregion
        
        #region AI Core Logic
        
        /// <summary>
        /// Main AI update loop
        /// </summary>
        private void UpdateAI()
        {
            // Update enemy detection
            DetectEnemies();
            
            // Update current target validity
            ValidateCurrentTarget();
            
            // Execute state behavior
            switch (_currentState)
            {
                case CombatAIState.Idle:
                    HandleIdleState();
                    break;
                    
                case CombatAIState.Seeking:
                    HandleSeekingState();
                    break;
                    
                case CombatAIState.Approaching:
                    HandleApproachingState();
                    break;
                    
                case CombatAIState.Combat:
                    HandleCombatState();
                    break;
                    
                case CombatAIState.Retreating:
                    HandleRetreatingState();
                    break;
            }
        }
        
        #endregion
        
        #region State Handlers
        
        private void HandleIdleState()
        {
            // Look for enemies in detection range
            if (_detectedEnemies.Count > 0)
            {
                SelectBestTarget();
                if (_currentTarget != null)
                {
                    SetState(CombatAIState.Seeking);
                }
            }
        }
        
        private void HandleSeekingState()
        {
            if (_currentTarget == null)
            {
                SetState(CombatAIState.Idle);
                return;
            }
            
            _distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
            
            // If enemy is within detection range, start approaching
            if (_distanceToTarget <= _detectionRange)
            {
                SetState(CombatAIState.Approaching);
            }
            else
            {
                // Enemy moved out of detection range
                SetState(CombatAIState.Idle);
                _currentTarget = null;
            }
        }
        
        private void HandleApproachingState()
        {
            if (_currentTarget == null)
            {
                SetState(CombatAIState.Idle);
                return;
            }
            
            _distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
            
            // Check if in attack range
            if (_distanceToTarget <= _attackRange)
            {
                SetState(CombatAIState.Combat);
                return;
            }
            
            // Check if target moved too far away
            if (_distanceToTarget > _detectionRange * 1.2f)
            {
                SetState(CombatAIState.Idle);
                _currentTarget = null;
                return;
            }
            
            // Move towards target
            MoveTowardsTarget();
        }
        
        private void HandleCombatState()
        {
            if (_currentTarget == null)
            {
                SetState(CombatAIState.Idle);
                return;
            }
            
            _distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
            
            // If target moved out of attack range, approach again
            if (_distanceToTarget > _attackRange)
            {
                SetState(CombatAIState.Approaching);
                return;
            }
            
            // Maintain combat distance
            MaintainCombatDistance();
            
            // Attack if possible
            AttemptAttack();
        }
        
        private void HandleRetreatingState()
        {
            // Implement retreat behavior if needed
            // For now, return to idle after a short delay
            if (Time.time - _lastAIUpdateTime > 2f)
            {
                SetState(CombatAIState.Idle);
            }
        }
        
        #endregion
        
        #region Enemy Detection
        
        /// <summary>
        /// Detect enemies within detection range
        /// </summary>
        private void DetectEnemies()
        {
            _detectedEnemies.Clear();
            
            // Use Physics.OverlapSphere to find enemies
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, _detectionRange);
            
            foreach (var collider in nearbyColliders)
            {
                // Skip self
                if (collider.transform == transform) continue;
                
                // Check if it's an enemy (you may need to adjust this based on your team system)
                if (IsEnemy(collider.transform))
                {
                    _detectedEnemies.Add(collider.transform);
                }
            }
        }
        
        /// <summary>
        /// Check if a transform belongs to an enemy
        /// </summary>
        private bool IsEnemy(Transform target)
        {
            // Get our team component
            var myTeam = Entity.GetComponent<TeamComponent>();
            if (myTeam == null) return false;
            
            // Get target's team component
            var targetEntity = target.GetComponent<IEntity>();
            if (targetEntity != null)
            {
                var targetTeam = targetEntity.GetComponent<TeamComponent>();
                if (targetTeam != null)
                {
                    return myTeam.IsEnemy(targetTeam);
                }
            }
            
            // Fallback: check for enemy tag
            return target.CompareTag("Enemy");
        }
        
        #endregion
        
        #region Target Selection
        
        /// <summary>
        /// Select the best target from detected enemies
        /// </summary>
        private void SelectBestTarget()
        {
            if (_detectedEnemies.Count == 0)
            {
                _currentTarget = null;
                return;
            }
            
            Transform bestTarget = null;
            float bestScore = float.MinValue;
            
            foreach (var enemy in _detectedEnemies)
            {
                float score = CalculateTargetScore(enemy);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }
            
            _currentTarget = bestTarget;
            
            if (_currentTarget != null)
            {
                _lastKnownTargetPosition = _currentTarget.position;
            }
        }
        
        /// <summary>
        /// Calculate target priority score
        /// </summary>
        private float CalculateTargetScore(Transform target)
        {
            float distance = Vector3.Distance(transform.position, target.position);
            float distanceScore = 1f / (distance + 1f); // Closer targets get higher score
            
            // Add other factors based on target type, health, threat level, etc.
            float aggressionBonus = _aggressionLevel;
            
            return distanceScore * aggressionBonus;
        }
        
        #endregion
        
        #region Movement
        
        /// <summary>
        /// Move towards the current target
        /// </summary>
        private void MoveTowardsTarget()
        {
            if (_currentTarget == null) return;
            
            Vector3 direction = (_currentTarget.position - transform.position).normalized;
            Vector3 targetPosition = _currentTarget.position - direction * _combatDistance;
            
            // Move towards target
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                _moveSpeed * Time.deltaTime
            );
            
            // Rotate to face target
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
            _isMovingToTarget = true;
            _lastKnownTargetPosition = _currentTarget.position;
        }
        
        /// <summary>
        /// Maintain optimal combat distance
        /// </summary>
        private void MaintainCombatDistance()
        {
            if (_currentTarget == null) return;
            
            float currentDistance = Vector3.Distance(transform.position, _currentTarget.position);
            
            // If too close, move back slightly
            if (currentDistance < _combatDistance * 0.8f)
            {
                Vector3 direction = (transform.position - _currentTarget.position).normalized;
                transform.position += direction * _moveSpeed * 0.5f * Time.deltaTime;
            }
            // If too far, move closer
            else if (currentDistance > _combatDistance * 1.2f)
            {
                Vector3 direction = (_currentTarget.position - transform.position).normalized;
                transform.position += direction * _moveSpeed * 0.3f * Time.deltaTime;
            }
            
            // Always face the target
            Vector3 lookDirection = (_currentTarget.position - transform.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
        
        #endregion
        
        #region Combat Actions
        
        /// <summary>
        /// Attempt to attack the current target
        /// </summary>
        private void AttemptAttack()
        {
            if (_currentTarget == null || _combatComponent == null) return;
            
            // Check if we can attack (cooldown, etc.)
            if (!_combatComponent.CanAttack()) return;
            
            // Check if target is in attack range
            var targetEntity = _currentTarget.GetComponent<IEntity>();
            if (targetEntity != null && _combatComponent.IsInAttackRange(targetEntity))
            {
                // Perform attack
                _combatComponent.PerformEnhancedAttack(targetEntity);
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Validate current target (check if still alive, in range, etc.)
        /// </summary>
        private void ValidateCurrentTarget()
        {
            if (_currentTarget == null) return;
            
            // Check if target still exists
            if (_currentTarget.gameObject == null || !_currentTarget.gameObject.activeInHierarchy)
            {
                _currentTarget = null;
                SetState(CombatAIState.Idle);
                return;
            }
            
            // Check if target is still an enemy
            if (!IsEnemy(_currentTarget))
            {
                _currentTarget = null;
                SetState(CombatAIState.Idle);
                return;
            }
        }
        
        /// <summary>
        /// Set AI state with logging
        /// </summary>
        private void SetState(CombatAIState newState)
        {
            if (_currentState != newState)
            {
                _currentState = newState;
                OnStateChanged(newState);
            }
        }
        
        /// <summary>
        /// Handle state change events
        /// </summary>
        private void OnStateChanged(CombatAIState newState)
        {
            switch (newState)
            {
                case CombatAIState.Idle:
                    _isMovingToTarget = false;
                    break;
                    
                case CombatAIState.Combat:
                    _isMovingToTarget = false;
                    break;
            }
        }
        
        /// <summary>
        /// Load combat stats from UnitDataSO
        /// </summary>
        private void LoadStatsFromUnitData()
        {
            // Try to get UnitDataSO from UnitModel component
            // var unitModel = Entity.GetComponent<UnitModel>();
            // if (unitModel != null && unitModel.UnitData != null)
            // {
            //     var unitData = unitModel.UnitData;
            //     _detectionRange = unitData.DetectionRange;
            //     _attackRange = unitData.Range;
            //     _moveSpeed = unitData.MoveSpeed;
            //     _damage = unitData.Damage;
            //     
            //     Debug.Log($"CombatAI: Loaded stats - Detection: {_detectionRange}, Attack: {_attackRange}, Speed: {_moveSpeed}");
            // }
            // else
            // {
            //     // Fallback values
            //     _detectionRange = 10f;
            //     _attackRange = 2f;
            //     _moveSpeed = 3f;
            //     _damage = 10f;
            //     
            //     Debug.LogWarning($"CombatAI: Using fallback stats for {gameObject.name}");
            // }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Enable/disable combat AI
        /// </summary>
        public void SetCombatAIEnabled(bool enabled)
        {
            _enableCombatAI = enabled;
            if (!enabled)
            {
                SetState(CombatAIState.Idle);
                _currentTarget = null;
            }
        }
        
        /// <summary>
        /// Force target selection
        /// </summary>
        public void SetTarget(Transform target)
        {
            if (IsEnemy(target))
            {
                _currentTarget = target;
                SetState(CombatAIState.Seeking);
            }
        }
        
        /// <summary>
        /// Get current combat target
        /// </summary>
        public Transform GetCurrentTarget()
        {
            return _currentTarget;
        }
        
        /// <summary>
        /// Get current AI state
        /// </summary>
        public CombatAIState GetCurrentState()
        {
            return _currentState;
        }
        
        #endregion
        
        #region Gizmos
        
        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);
            
            // Draw line to current target
            if (_currentTarget != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// AI states for combat behavior
    /// </summary>
    public enum CombatAIState
    {
        Idle,       // No target, scanning for enemies
        Seeking,    // Target detected, evaluating approach
        Approaching, // Moving towards target
        Combat,     // In combat range, attacking
        Retreating  // Moving away from danger
    }
}