using System.Collections.Generic;
using SteeringBehavior;
using UnityEngine;
using Utils;

namespace Troop
{
    public class TroopController : MonoBehaviour
    {
        private TroopModel _model;
        private TroopView _view;
        private Transform _cachedTransform;
        private SteeringContext _steeringContext;
        private PathComponent _pathComponent;
        
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
        public TroopView TroopView => _view;
    
        private void Awake()
        {
            _cachedTransform = transform;
            _view = GetComponent<TroopView>();
            if (_view == null)
            {
                _view = gameObject.AddComponent<TroopView>();
            }
            
            _pathComponent = GetComponent<PathComponent>();
            _steeringContext = new SteeringContext();
            StateMachine = new TroopStateMachine(this);
            if (_groundLayer == 0)
                _groundLayer = LayerMask.GetMask("Ground");
                
            if (_enemyLayer == 0)
                _enemyLayer = LayerMask.GetMask("Enemy");
        }
    
        // public void Initialize(TroopConfigSO config)
        // {
        //     _model = new TroopModel(config, gameObject);
        //     
        //     if (config.animatorController)
        //     {
        //         _view.SetAnimatorController(config.animatorController);
        //     }
        //     
        //     // Thiết lập transform
        //     _model.Position = _cachedTransform.position;
        //     _model.Rotation = _cachedTransform.rotation;
        //     
        //     // Khởi tạo behavior enabled status
        //     foreach (var behavior in _model.SteeringBehavior.GetStrategies())
        //     {
        //         _behaviorEnabled[behavior.GetName()] = true;
        //     }
        //     
        //     // Khởi tạo state machine
        //     StateMachine.ChangeState<IdleState>();
        //     
        //     // Cập nhật steering context
        //     UpdateSteeringContext();
        // }
        public void Initialize(TroopConfigSO config)
        {
            _model = new TroopModel(config);
    
            if (config.animatorController != null)
            {
                _view.SetAnimatorController(config.animatorController);
            }
    
            // Thiết lập renderer màu sắc theo team (thêm vào)
            SetupVisuals();
    
            _model.Position = _cachedTransform.position;
            _model.Rotation = _cachedTransform.rotation;
    
            // *** THÊM DÒNG NÀY: Cập nhật view ngay lập tức ***
            UpdateView();
    
            // Cập nhật steering context
            UpdateSteeringContext();
        }
        private void SetupVisuals()
        {
            if (_view != null)
            {
                Renderer renderer = GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    // Màu dựa trên team
                    if (gameObject.CompareTag("Player"))
                    {
                        renderer.material.color = Color.blue;
                    }
                    else if (gameObject.CompareTag("Enemy"))
                    {
                        renderer.material.color = Color.red;
                    }
                }
            }
        }
    
        private void Update()
        {
            if (_model == null) return;
            if (_model.CurrentState == TroopState.Dead) return;
            
            // Cập nhật timers
            if (_stunTimer > 0) _stunTimer -= Time.deltaTime;
            if (_knockbackTimer > 0) _knockbackTimer -= Time.deltaTime;
            if (_attackTimer > 0) _attackTimer -= Time.deltaTime;
            
            // Cập nhật state machine
            StateMachine.Update();
            
            // Cập nhật steering context
            UpdateSteeringContext();
            
            // Tính toán các lực steering (nếu không bị stun hoặc knockback)
            if (_stunTimer <= 0 && _knockbackTimer <= 0)
            {
                Vector3 steeringForce = CalculateSteeringForces();
                ApplySteeringForce(steeringForce, Time.deltaTime);
            }
            
            // Cập nhật view
            UpdateView();
        }
        
        private void UpdateSteeringContext()
        {
            if (_model == null) return;
    
            _steeringContext.TroopModel = _model;
            _steeringContext.TargetPosition = _targetPosition;
            _steeringContext.DeltaTime = Time.deltaTime;
    
            if (_pathComponent)
                _steeringContext.CurrentPath = _pathComponent.CurrentBehaviorPath;
        }
        
        private Vector3 CalculateSteeringForces()
        {
            // Tạo một composite behavior tạm thời chỉ với các behavior đang enabled
            CompositeSteeringBehavior activeBehaviors = new CompositeSteeringBehavior();
            
            foreach (var behavior in _model.SteeringBehavior.GetStrategies())
            {
                if (_behaviorEnabled.TryGetValue(behavior.GetName(), out bool isEnabled) && isEnabled)
                {
                    activeBehaviors.AddStrategy(behavior);
                }
            }
            
            // Thực thi các behavior đang active
            return activeBehaviors.Execute(_steeringContext);
        }
    
        private void ApplySteeringForce(Vector3 force, float deltaTime)
        {
            // Áp dụng lực vào gia tốc
            _model.Acceleration = force;
        
            // Cập nhật vận tốc
            _model.Velocity += _model.Acceleration * deltaTime;
        
            // Giới hạn tốc độ
            float maxSpeed = _model.GetModifiedMoveSpeed();
            if (_model.Velocity.magnitude > maxSpeed)
            {
                _model.Velocity = _model.Velocity.normalized * maxSpeed;
            }

            // Cập nhật vị trí (ignore Y)
            _model.Velocity = new Vector3(_model.Velocity.x, 0, _model.Velocity.z);
            Vector3 newPosition = _model.Position + _model.Velocity * deltaTime;
    
            // Raycast để tìm ground height
            RaycastHit hit;
            if (Physics.Raycast(newPosition + Vector3.up * 5, Vector3.down, out hit, 10f, _groundLayer))
            {
                // Điều chỉnh y coordinate để troop đứng trên ground
                newPosition.y = hit.point.y;
            }
    
            _model.Position = newPosition;
        
            // Cập nhật góc quay nếu đang di chuyển
            if (_model.Velocity.sqrMagnitude > 0.01f)
            {
                // Tạo vector nhìn về hướng di chuyển
                Vector3 lookDirection = _model.Velocity.normalized;
                if (lookDirection != Vector3.zero)
                {
                    // Tạo góc quay nhìn về hướng di chuyển
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    _model.Rotation = Quaternion.Slerp(_model.Rotation, targetRotation, 10f * deltaTime);
                }
            }
        }
    
        private void UpdateView()
        {
            // Cập nhật vị trí và góc quay
            _view.UpdateTransform(_model.Position, _model.Rotation);
        
            // Cập nhật animation state
            _view.UpdateAnimation(_model.CurrentState, _model.Velocity.magnitude / _model.MoveSpeed);
        }
    
        #region Public API
        
        // Enable or disable a specific behavior
        public void EnableBehavior(string behaviorName, bool enable)
        {
            if (_behaviorEnabled.ContainsKey(behaviorName))
            {
                _behaviorEnabled[behaviorName] = enable;
            }
        }
        
        // Check if a behavior is enabled
        public bool IsBehaviorEnabled(string behaviorName)
        {
            if (_behaviorEnabled.TryGetValue(behaviorName, out bool isEnabled))
            {
                return isEnabled;
            }
            return false;
        }
        
        // Apply stun
        public void ApplyStun(float duration)
        {
            // Apply stun resistance
            duration *= (1f - _stunResistance);
            _stunTimer = Mathf.Max(_stunTimer, duration);
        }
        
        // Apply knockback
        public void ApplyKnockback(Vector3 direction, float force, float duration)
        {
            // Apply knockback resistance
            force *= (1f - _knockbackResistance);
            duration *= (1f - _knockbackResistance);
            
            // Apply force directly to velocity
            _model.Velocity = direction.normalized * force;
            _knockbackTimer = Mathf.Max(_knockbackTimer, duration);
        }
        
        // Take damage with potential stun and knockback
        public void TakeDamage(float amount, Vector3 impactDirection = default, float knockbackForce = 0, float stunChance = 0)
        {
            // Apply damage
            _model.TakeDamage(amount);
            
            // Check for death
            if (_model.CurrentHealth <= 0)
            {
                StateMachine.ChangeState<DeadState>();
                return;
            }
            
            // Apply knockback if any
            if (knockbackForce > 0)
            {
                ApplyKnockback(impactDirection, knockbackForce, 0.5f);
            }
            
            // Apply stun based on chance
            if (stunChance > 0 && Random.value < stunChance)
            {
                ApplyStun(1.0f);
            }
            
            // Trigger hit animation
            _view.TriggerAnimation("Hit");
        }
        
        // Attack target
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
                    _view.TriggerAnimation("Attack");
                    
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