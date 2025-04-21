using SteeringBehavior;
using UnityEngine;
using Utils;

namespace Troop
{
    public class TroopController : MonoBehaviour
    {
        [SerializeField]
        private TroopModel _model;
        private TroopView _view;
        private Transform _cachedTransform;
        private SteeringContext _steeringContext;
        private PathComponent _pathComponent;
        public LayerMask groundLayer;
    
        private void Awake()
        {
            _cachedTransform = transform;
            _view = GetComponent<TroopView>();
            _pathComponent = GetComponent<PathComponent>();
            _steeringContext = new SteeringContext();
            if (groundLayer == 0)
                groundLayer = LayerMask.GetMask("Ground");
        }
    
        public void Initialize(TroopConfigSO config)
        {
            _model = new TroopModel(config);
            if (config.animatorController != null)
            {
                _view.SetAnimatorController(config.animatorController);
            }
            _model.Position = _cachedTransform.position;
            _model.Rotation = _cachedTransform.rotation;
            UpdateSteeringContext();
        }
    
        private void Update()
        {
            if (_model == null) return;
            if (_model.CurrentState == TroopState.Dead) return;
        
            // Cập nhật steering context
            UpdateSteeringContext();
        
            // Tính toán lực steering từ behaviors
            Vector3 steeringForce = CalculateSteeringForces();
        
            // Áp dụng lực vào model
            ApplySteeringForce(steeringForce, Time.deltaTime);
            UpdateView();
        }
        private void UpdateSteeringContext()
        {
            if (_model == null) return;
    
            _steeringContext.TroopModel = _model;
            _steeringContext.TargetPosition = _targetPosition; // Thêm dòng này để lưu target position
    
            if (_pathComponent)
                _steeringContext.CurrentPath = _pathComponent.CurrentBehaviorPath;
        
            // Thêm các dòng này
            _steeringContext.DeltaTime = Time.deltaTime;
    
            // Nếu troop đang di chuyển, cập nhật currentState
            if ((_targetPosition - _model.Position).magnitude > 0.1f) 
            {
                _model.CurrentState = TroopState.Moving;
            }
        }
        private Vector3 _targetPosition = Vector3.zero;
        
    
        // Tính toán lực steering từ tất cả behaviors
        private Vector3 CalculateSteeringForces()
        {
            // Sử dụng composite behavior để tính lực
            return _model.SteeringBehavior.Execute(_steeringContext);
        }
    
        // Áp dụng lực steering để cập nhật vị trí và góc quay
        private void ApplySteeringForce(Vector3 force, float deltaTime)
        {
            // Áp dụng lực vào gia tốc
            _model.Acceleration = force;
        
            // Cập nhật vận tốc
            _model.Velocity += _model.Acceleration * deltaTime;
        
            // Giới hạn tốc độ nếu cần
            if (_model.Velocity.magnitude > _model.MoveSpeed)
            {
                _model.Velocity = _model.Velocity.normalized * _model.MoveSpeed;
            }

            // Cập nhật vị trí
            _model.Velocity = new Vector3(_model.Velocity.x, 0, _model.Velocity.z);
            Vector3 newPosition = _model.Position + _model.Velocity * deltaTime;
    
            // Raycast để tìm ground height
            RaycastHit hit;
            if (Physics.Raycast(newPosition + Vector3.up * 5, Vector3.down, out hit, 10f, groundLayer))
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
        
            // Cập nhật state nếu cần
            if (_model.Velocity.magnitude > 0.1f && _model.CurrentState != TroopState.Attacking && _model.CurrentState != TroopState.Fleeing)
            {
                _model.CurrentState = TroopState.Moving;
            }
            else if (_model.Velocity.magnitude <= 0.1f && _model.CurrentState == TroopState.Moving)
            {
                _model.CurrentState = TroopState.Idle;
            }
        }
    
        // Cập nhật view dựa trên model
        private void UpdateView()
        {
            // Cập nhật vị trí và góc quay
            _view.UpdateTransform(_model.Position, _model.Rotation);
        
            // Cập nhật animation state
            _view.UpdateAnimation(_model.CurrentState, _model.Velocity.magnitude / _model.MoveSpeed);
        }
    
        // Phương thức public để nhận damage
        public void TakeDamage(float amount)
        {
            _model.TakeDamage(amount);
        
            // Kích hoạt animation bị đánh
            _view.TriggerAnimation("Hit");
        }
    
        // Phương thức public để thay đổi state
        public void SetState(TroopState newState)
        {
            _model.CurrentState = newState;
        }
    
        // Phương thức public để lấy vị trí hiện tại
        public Vector3 GetPosition()
        {
            return _model.Position;
        }
    
        // Phương thức public để lấy state hiện tại
        public TroopState GetState()
        {
            return _model.CurrentState;
        }
    
        // Phương thức public để kiểm tra troop còn sống không
        public bool IsAlive()
        {
            return _model.CurrentState != TroopState.Dead;
        }
    
        // Phương thức public để thiết lập target position
        public void SetTargetPosition(Vector3 position)
        {
            _targetPosition = position;
            _steeringContext.TargetPosition = position;
        }
    
        // Phương thức public để thiết lập các troops gần đó
        public void SetNearbyTroops(TroopController[] allies, TroopController[] enemies)
        {
            _steeringContext.NearbyAllies = allies;
            _steeringContext.NearbyEnemies = enemies;
        }
    }
}