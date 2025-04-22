using System.Collections.Generic;
using SteeringBehavior;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

namespace Troop
{
    public class TroopController : MonoBehaviour
    {
        [SerializeField] private TroopView view;
        [SerializeField] private PathComponent pathComponent;

        private Transform _cachedTransform;
        private SteeringContext _steeringContext;
        private TroopModel _model;
        public TroopStateMachine StateMachine { get; private set; }
        
        private float _stunTimer = 0f;
        private float _knockbackTimer = 0f;
        private float _attackTimer = 0f;
        
        private LayerMask _groundLayer;
        private LayerMask _enemyLayer;
        
        private Vector3 _targetPosition = Vector3.zero;
        
        private Dictionary<string, bool> _behaviorEnabled = new Dictionary<string, bool>();
        
        private TroopController _currentTarget;
        
        [Header("Behavior Settings")]
        [SerializeField] private float _aggroRange = 8f;
        [SerializeField] private float _stunResistance = 0.5f;
        [SerializeField] private float _knockbackResistance = 0.5f;
        
        
        // public size 
        public float StunTimer => _stunTimer;
        public float KnockbackTimer => _knockbackTimer;
        public float AttackTimer => _attackTimer;
        public SteeringContext SteeringContext => _steeringContext;
        public TroopView TroopView => view;
    
        private void Awake()
        {
            _cachedTransform = transform;
            view = GetComponent<TroopView>();
            if (view == null)
            {
                view = gameObject.AddComponent<TroopView>();
            }
            pathComponent = GetComponent<PathComponent>();
            _steeringContext = new SteeringContext();
            StateMachine = new TroopStateMachine(this);
        }
        public void Initialize(TroopConfigSO config)
        {
            if (!config) return;
            
            _model = new TroopModel(config, gameObject);
            if (view)
            {
                if (config.animatorController)
                {
                    view.SetAnimatorController(config.animatorController);
                }
                SetupVisuals();
            }
            _model.Position = _cachedTransform.position;
            _model.Rotation = _cachedTransform.rotation;
            
            _behaviorEnabled = new Dictionary<string, bool>();
            foreach (var behavior in _model.SteeringBehavior.GetSteeringBehaviors())
            {
                _behaviorEnabled[behavior.GetName()] = true;
            }
            _targetPosition = _cachedTransform.position;
            UpdateView();
            UpdateSteeringContext();
            
            StateMachine.ChangeState<IdleState>();
            if (TroopManager.Instance)
            {
                TroopManager.Instance.RegisterTroop(this);
            }
        }
        private void SetupVisuals()
        {
            
        }
    
        private void Update()
        {
            if (_model == null) return;
            if (_model.CurrentState == TroopState.Dead) return;
            if (_stunTimer > 0) _stunTimer -= Time.deltaTime;
            if (_knockbackTimer > 0) _knockbackTimer -= Time.deltaTime;
            if (_attackTimer > 0) _attackTimer -= Time.deltaTime;
            
            StateMachine.Update();
            
            UpdateSteeringContext();
            if (_stunTimer <= 0 && _knockbackTimer <= 0)
            {
                Vector3 steeringForce = CalculateSteeringForces();
                ApplySteeringForce(steeringForce, Time.deltaTime);
            }
            UpdateView();
        }
        
        private void UpdateSteeringContext()
        {
            if (_model == null) return;

            // Set basic context properties
            _steeringContext.TroopModel = _model;
            _steeringContext.TargetPosition = _targetPosition;
            _steeringContext.DeltaTime = Time.deltaTime;
    
            if (_steeringContext.NearbyAllies == null || _steeringContext.NearbyEnemies == null)
            {
                if (TroopManager.Instance)
                {
                    _steeringContext.NearbyAllies = TroopManager.Instance.GetNearbyAllies(this);
                    _steeringContext.NearbyEnemies = TroopManager.Instance.GetNearbyEnemies(this);
                }
            }
    
            var squadExtensions = TroopControllerSquadExtensions.Instance;
            if (squadExtensions != null)
            {
                var squad = squadExtensions.GetSquad(this);
                if (squad)
                {
                    var squadPos = squadExtensions.GetSquadPosition(this);
                    Vector3 desiredSquadPos = squad.GetPositionForTroop(squad, squadPos.x, squadPos.y);
                    _steeringContext.DesiredSquadPosition = desiredSquadPos;
                }
            }
            if (pathComponent)
            {
                _steeringContext.CurrentPath = pathComponent.CurrentBehaviorPath;
            }
        }
        
        private Vector3 CalculateSteeringForces()
        {
            CompositeSteeringBehavior activeBehaviors = new CompositeSteeringBehavior();
            
            foreach (var behavior in _model.SteeringBehavior.GetSteeringBehaviors())
            {
                if (_behaviorEnabled.TryGetValue(behavior.GetName(), out bool isEnabled) && isEnabled)
                {
                    activeBehaviors.AddStrategy(behavior);
                }
            }
            return activeBehaviors.Execute(_steeringContext);
        }
    
        private void ApplySteeringForce(Vector3 force, float deltaTime)
        {
            _model.Acceleration = force;
            _model.Velocity += _model.Acceleration * deltaTime;
        
            float maxSpeed = _model.GetModifiedMoveSpeed();
            if (_model.Velocity.magnitude > maxSpeed)
            {
                _model.Velocity = _model.Velocity.normalized * maxSpeed;
            }
            _model.Velocity = new Vector3(_model.Velocity.x, 0, _model.Velocity.z);
            Vector3 newPosition = _model.Position + _model.Velocity * deltaTime;
    
            RaycastHit hit;
            if (Physics.Raycast(newPosition + Vector3.up * 5, Vector3.down, out hit, 10f, _groundLayer))
            {
                newPosition.y = hit.point.y;
            }
    
            _model.Position = newPosition;
            if (_model.Velocity.sqrMagnitude > 0.01f)
            {
                Vector3 lookDirection = _model.Velocity.normalized;
                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    _model.Rotation = Quaternion.Slerp(_model.Rotation, targetRotation, 10f * deltaTime);
                }
            }
        }
    
        private void UpdateView()
        {
            view.UpdateTransform(_model.Position, _model.Rotation);
            view.UpdateAnimation(_model.CurrentState, _model.Velocity.magnitude / _model.MoveSpeed);
        }
    
        #region Public API
        public void EnableBehavior(string behaviorName, bool enable)
        {
            if (_behaviorEnabled.ContainsKey(behaviorName))
            {
                _behaviorEnabled[behaviorName] = enable;
            }
        }
        public bool IsBehaviorEnabled(string behaviorName)
        {
            if (_behaviorEnabled.TryGetValue(behaviorName, out bool isEnabled))
            {
                return isEnabled;
            }
            return false;
        }
        
        public void ApplyStun(float duration)
        {
            duration *= (1f - _stunResistance);
            _stunTimer = Mathf.Max(_stunTimer, duration);
        }
        
        public void ApplyKnockback(Vector3 direction, float force, float duration)
        {
            force *= (1f - _knockbackResistance);
            duration *= (1f - _knockbackResistance);
            
            _model.Velocity = direction.normalized * force;
            _knockbackTimer = Mathf.Max(_knockbackTimer, duration);
        }
        
        public void TakeDamage(float amount, Vector3 impactDirection = default, float knockbackForce = 0, float stunChance = 0)
        {
            _model.TakeDamage(amount);
            if (_model.CurrentHealth <= 0)
            {
                StateMachine.ChangeState<DeadState>();
                return;
            }
            if (knockbackForce > 0)
            {
                ApplyKnockback(impactDirection, knockbackForce, 0.5f);
            }
            if (stunChance > 0 && Random.value < stunChance)
            {
                ApplyStun(1.0f);
            }
            view.TriggerAnimation("Hit");
        }
        
        public bool Attack(TroopController target)
        {
            if (_attackTimer <= 0)
            {
                if (target != null && target.IsAlive())
                {
                    // Calculate direction to target for knockback
                    Vector3 direction = (target.GetPosition() - _model.Position).normalized;
                    
                    // Apply damage
                    target.TakeDamage(
                        _model.AttackPower, 
                        direction, 
                        _model.AttackPower * 0.5f, // Knockback force proportional to attack power
                        0.2f); // 20% chance to stun
                    
                    // Reset attack timer
                    _attackTimer = 1f / _model.AttackSpeed;
                    
                    // Trigger attack animation
                    view.TriggerAnimation("Attack");
                    
                    return true;
                }
            }
            
            return false;
        }
        
        // Set target position
        public void SetTargetPosition(Vector3 position)
        {
            _targetPosition = position;
            _steeringContext.TargetPosition = position;
            
            // If in idle and position changed significantly, transition to moving
            if (StateMachine.CurrentStateEnum == TroopState.Idle && 
                Vector3.Distance(_model.Position, _targetPosition) > 0.5f)
            {
                StateMachine.ChangeState<MovingState>();
            }
        }
    
        // Get current position
        public Vector3 GetPosition()
        {
            return _model.Position;
        }
        
        // Get target position
        public Vector3 GetTargetPosition()
        {
            return _targetPosition;
        }
        
        // Get current state
        public TroopState GetState()
        {
            return StateMachine.CurrentStateEnum;
        }
    
        // Check if troop is alive
        public bool IsAlive()
        {
            return StateMachine.CurrentStateEnum != TroopState.Dead;
        }
        
        // Set nearby troops
        public void SetNearbyTroops(TroopController[] allies, TroopController[] enemies)
        {
            _steeringContext.NearbyAllies = allies;
            _steeringContext.NearbyEnemies = enemies;
        }
        
        // Get model
        public TroopModel GetModel()
        {
            return _model;
        }
        
        #endregion
    }
}