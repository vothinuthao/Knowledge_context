using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Models;

namespace VikingRaven.Units.Controllers
{
    /// <summary>
    /// Controller quản lý realtime squad: chọn leader, điều phối di chuyển, và quản lý đội hình động
    /// Script này hoạt động độc lập với FormationSystem để cung cấp điều khiển linh hoạt hơn
    /// </summary>
    [System.Serializable]
    public class SquadController : MonoBehaviour
    {
        #region Inspector Fields

        [TitleGroup("Squad Management")]
        [Tooltip("Squad Model được quản lý bởi controller này")]
        [SerializeField, ReadOnly]
        private int _squadId = -1;

        [Tooltip("Leader hiện tại của squad")]
        [SerializeField, ReadOnly] 
        private int _currentLeaderId = -1;

        [TitleGroup("Leader Selection")]
        [Tooltip("Tự động chọn leader mới khi leader hiện tại chết")]
        [SerializeField, ToggleLeft]
        private bool _autoSelectNewLeader = true;

        [Tooltip("Ưu tiên loại unit nào làm leader")]
        [SerializeField, EnumToggleButtons]
        private UnitType _preferredLeaderType = UnitType.Infantry;

        [Tooltip("Khoảng thời gian kiểm tra leader (giây)")]
        [SerializeField, Range(0.1f, 2f)]
        private float _leaderCheckInterval = 0.5f;

        [TitleGroup("Movement Coordination")]
        [Tooltip("Khoảng cách tối đa units có thể cách xa leader")]
        [SerializeField, Range(2f, 10f), ProgressBar(2, 10)]
        private float _maxDistanceFromLeader = 5f;

        [Tooltip("Tốc độ theo kịp leader (multiplier)")]
        [SerializeField, Range(0.8f, 1.5f)]
        private float _followSpeedMultiplier = 1.1f;

        [Tooltip("Khoảng cách để bắt đầu follow leader")]
        [SerializeField, Range(1f, 5f)]
        private float _followStartDistance = 2f;

        [TitleGroup("Formation Control")]
        [Tooltip("Tự động điều chỉnh đội hình khi di chuyển")]
        [SerializeField, ToggleLeft]
        private bool _dynamicFormationAdjustment = true;

        [Tooltip("Loại đội hình mặc định khi không có lệnh cụ thể")]
        [SerializeField, EnumToggleButtons]
        private FormationType _defaultFormationType = FormationType.Line;

        [Tooltip("Thời gian chờ trước khi tự động điều chỉnh đội hình")]
        [SerializeField, Range(1f, 5f)]
        private float _formationAdjustmentDelay = 2f;

        [TitleGroup("Combat Coordination")]
        [Tooltip("Tự động phân công target cho units trong squad")]
        [SerializeField, ToggleLeft]
        private bool _autoTargetAssignment = true;

        [Tooltip("Phạm vi tìm kiếm target chung cho squad")]
        [SerializeField, Range(5f, 20f)]
        private float _squadDetectionRange = 12f;

        [TitleGroup("Debug Information")]
        [ShowInInspector, ReadOnly, PropertySpace(SpaceBefore = 10)]
        private string SquadStatus => _squadModel != null 
            ? $"Squad {_squadId}: {_squadModel.UnitCount} units, Leader: {_currentLeaderId}"
            : "No Squad Assigned";

        [ShowInInspector, ReadOnly]
        private Vector3 SquadCenter => _squadModel?.CurrentPosition ?? Vector3.zero;

        [ShowInInspector, ReadOnly]
        private string LeaderStatus => _currentLeader != null 
            ? $"Leader: {_currentLeader.DisplayName} (ID: {_currentLeaderId})"
            : "No Leader";

        [ShowInInspector, ReadOnly]
        private int ActiveFollowersCount => _followers?.Count ?? 0;

        #endregion

        #region Private Fields

        // Core references
        private SquadModel _squadModel;
        private UnitModel _currentLeader;
        private List<UnitModel> _followers = new List<UnitModel>();
        private List<UnitModel> _allUnits = new List<UnitModel>();

        // Timing and state
        private float _lastLeaderCheckTime;
        private float _lastFormationAdjustmentTime;
        private bool _isInitialized = false;
        private bool _isMoving = false;

        // Target management
        private IEntity _squadTarget;
        private Dictionary<int, IEntity> _unitTargets = new Dictionary<int, IEntity>();

        // Formation state
        private FormationType _currentFormationType;
        private Vector3 _lastSquadPosition;
        private bool _formationNeedsAdjustment = false;

        // Performance optimization
        private int _updateFrameCounter = 0;
        private const int UPDATE_FREQUENCY = 3; // Update every 3 frames

        #endregion

        #region Events

        public delegate void SquadLeaderEvent(UnitModel newLeader, UnitModel oldLeader);
        public delegate void SquadMovementEvent(Vector3 targetPosition);
        public delegate void SquadFormationEvent(FormationType newFormation);

        public event SquadLeaderEvent OnLeaderChanged;
        public event SquadMovementEvent OnSquadMoveCommand;
        public event SquadFormationEvent OnFormationChanged;
        public event Action<SquadController> OnSquadDisbanded;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("SquadController: Chưa được khởi tạo với squad. Gọi InitializeWithSquad() trước.");
            }
        }

        private void Update()
        {
            if (!_isInitialized || _squadModel == null) return;

            // Performance optimization - không update mỗi frame
            _updateFrameCounter++;
            if (_updateFrameCounter % UPDATE_FREQUENCY != 0) return;

            // Core update cycle
            UpdateSquadState();
            CheckLeaderStatus();
            UpdateFollowerMovement();
            UpdateFormationIfNeeded();
            UpdateCombatCoordination();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Khởi tạo SquadController với SquadModel
        /// </summary>
        public void InitializeWithSquad(SquadModel squadModel)
        {
            if (squadModel == null)
            {
                Debug.LogError("SquadController: SquadModel không thể null");
                return;
            }

            _squadModel = squadModel;
            _squadId = squadModel.SquadId;
            _currentFormationType = squadModel.Data?.DefaultFormationType ?? _defaultFormationType;

            // Subscribe to squad events
            SubscribeToSquadEvents();

            // Setup initial state
            RefreshUnitList();
            SelectInitialLeader();

            // Position controller at squad center
            transform.position = squadModel.CurrentPosition;

            _isInitialized = true;
            _lastSquadPosition = squadModel.CurrentPosition;

            Debug.Log($"SquadController: Đã khởi tạo cho squad {_squadId} với {_allUnits.Count} units");
        }

        /// <summary>
        /// Subscribe to squad events
        /// </summary>
        private void SubscribeToSquadEvents()
        {
            if (_squadModel == null) return;

            _squadModel.OnUnitAdded += HandleUnitAdded;
            _squadModel.OnUnitRemoved += HandleUnitRemoved;
            _squadModel.OnUnitDied += HandleUnitDied;
            _squadModel.OnPositionChanged += HandleSquadPositionChanged;
            _squadModel.OnFormationChanged += HandleFormationChanged;
        }

        /// <summary>
        /// Unsubscribe from squad events
        /// </summary>
        private void UnsubscribeFromSquadEvents()
        {
            if (_squadModel == null) return;

            _squadModel.OnUnitAdded -= HandleUnitAdded;
            _squadModel.OnUnitRemoved -= HandleUnitRemoved;
            _squadModel.OnUnitDied -= HandleUnitDied;
            _squadModel.OnPositionChanged -= HandleSquadPositionChanged;
            _squadModel.OnFormationChanged -= HandleFormationChanged;
        }

        #endregion

        #region Leader Management

        /// <summary>
        /// Chọn leader ban đầu cho squad
        /// </summary>
        [Button("Select Initial Leader"), TitleGroup("Leader Control")]
        private void SelectInitialLeader()
        {
            if (_allUnits.Count == 0)
            {
                Debug.LogWarning("SquadController: Không có units để chọn leader");
                return;
            }

            UnitModel newLeader = FindBestLeaderCandidate();
            SetLeader(newLeader);
        }

        /// <summary>
        /// Tìm candidate tốt nhất để làm leader
        /// </summary>
        private UnitModel FindBestLeaderCandidate()
        {
            // Ưu tiên theo loại unit được setting
            var preferredTypeUnits = _allUnits.Where(u => u.UnitType == _preferredLeaderType).ToList();
            if (preferredTypeUnits.Count > 0)
            {
                // Chọn unit có máu nhiều nhất trong loại ưu tiên
                return preferredTypeUnits.OrderByDescending(u => u.CurrentHealth).First();
            }

            // Nếu không có loại ưu tiên, chọn unit khỏe nhất
            return _allUnits.OrderByDescending(u => u.CurrentHealth).First();
        }

        /// <summary>
        /// Set unit làm leader
        /// </summary>
        private void SetLeader(UnitModel newLeader)
        {
            if (newLeader == null || newLeader.Entity == null) return;

            UnitModel oldLeader = _currentLeader;
            _currentLeader = newLeader;
            _currentLeaderId = newLeader.Entity.Id;

            // Update followers list
            RefreshFollowersList();

            // Mark leader in formation component
            var leaderFormationComponent = newLeader.Entity.GetComponent<FormationComponent>();
            if (leaderFormationComponent != null)
            {
                // Leader thường ở vị trí trung tâm đội hình
                leaderFormationComponent.SetFormationSlot(0);
            }

            // Trigger event
            OnLeaderChanged?.Invoke(newLeader, oldLeader);

            Debug.Log($"SquadController: Đã chọn {newLeader.DisplayName} (ID: {_currentLeaderId}) làm leader cho squad {_squadId}");
        }

        /// <summary>
        /// Kiểm tra trạng thái leader và chọn leader mới nếu cần
        /// </summary>
        private void CheckLeaderStatus()
        {
            if (Time.time - _lastLeaderCheckTime < _leaderCheckInterval) return;
            _lastLeaderCheckTime = Time.time;

            // Kiểm tra leader hiện tại có còn sống không
            if (_currentLeader == null || _currentLeader.CurrentHealth <= 0)
            {
                if (_autoSelectNewLeader && _allUnits.Count > 0)
                {
                    Debug.Log($"SquadController: Leader squad {_squadId} đã chết, đang chọn leader mới...");
                    SelectInitialLeader();
                }
                else
                {
                    _currentLeader = null;
                    _currentLeaderId = -1;
                }
            }
        }

        #endregion

        #region Movement Coordination

        /// <summary>
        /// Update movement của followers để follow leader
        /// </summary>
        private void UpdateFollowerMovement()
        {
            if (_currentLeader == null || _followers.Count == 0) return;

            Vector3 leaderPosition = _currentLeader.Position;
            
            foreach (var follower in _followers)
            {
                if (follower?.Entity == null) continue;

                UpdateFollowerPosition(follower, leaderPosition);
            }
        }

        /// <summary>
        /// Update vị trí của một follower cụ thể
        /// </summary>
        private void UpdateFollowerPosition(UnitModel follower, Vector3 leaderPosition)
        {
            float distanceToLeader = Vector3.Distance(follower.Position, leaderPosition);
            
            // Chỉ update nếu follower quá xa leader
            if (distanceToLeader <= _followStartDistance) return;

            var navigationComponent = follower.Entity.GetComponent<NavigationComponent>();
            if (navigationComponent == null) return;

            Vector3 targetPosition = CalculateFollowerTargetPosition(follower, leaderPosition);

            if (distanceToLeader > _maxDistanceFromLeader)
            {
                navigationComponent.SetDestination(targetPosition, NavigationCommandPriority.High);
            }
            else
            {
                navigationComponent.SetDestination(targetPosition, NavigationCommandPriority.Normal);
            }
        }

        /// <summary>
        /// Tính toán vị trí target cho follower dựa trên đội hình
        /// </summary>
        private Vector3 CalculateFollowerTargetPosition(UnitModel follower, Vector3 leaderPosition)
        {
            var formationComponent = follower.Entity.GetComponent<FormationComponent>();
            if (formationComponent != null)
            {
                // Sử dụng formation offset nếu có
                Vector3 formationOffset = formationComponent.FormationOffset;
                return leaderPosition + formationOffset;
            }

            // Fallback: đặt follower xung quanh leader
            int followerIndex = _followers.IndexOf(follower);
            float angle = (followerIndex * 2 * Mathf.PI) / _followers.Count;
            float distance = _followStartDistance;
            
            Vector3 offset = new Vector3(
                Mathf.Sin(angle) * distance,
                0,
                Mathf.Cos(angle) * distance
            );

            return leaderPosition + offset;
        }

        /// <summary>
        /// Lệnh di chuyển cho toàn squad
        /// </summary>
        public void MoveSquadTo(Vector3 targetPosition)
        {
            if (_currentLeader?.Entity == null) return;

            // Di chuyển leader trước
            var leaderNavigation = _currentLeader.Entity.GetComponent<NavigationComponent>();
            if (leaderNavigation != null)
            {
                leaderNavigation.SetDestination(targetPosition, NavigationCommandPriority.High);
            }

            // Mark as moving
            _isMoving = true;
            _formationNeedsAdjustment = true;

            // Trigger event
            OnSquadMoveCommand?.Invoke(targetPosition);

            Debug.Log($"SquadController: Squad {_squadId} di chuyển đến {targetPosition}");
        }

        #endregion

        #region Formation Management

        /// <summary>
        /// Update formation nếu cần thiết
        /// </summary>
        private void UpdateFormationIfNeeded()
        {
            if (!_dynamicFormationAdjustment || !_formationNeedsAdjustment) return;
            if (Time.time - _lastFormationAdjustmentTime < _formationAdjustmentDelay) return;

            // Kiểm tra xem có cần điều chỉnh đội hình không
            if (ShouldAdjustFormation())
            {
                AdjustFormationDynamically();
                _lastFormationAdjustmentTime = Time.time;
                _formationNeedsAdjustment = false;
            }
        }

        /// <summary>
        /// Kiểm tra xem có nên điều chỉnh đội hình không
        /// </summary>
        private bool ShouldAdjustFormation()
        {
            // Điều chỉnh nếu squad đang di chuyển và units quá rải rác
            if (_isMoving)
            {
                float averageDistanceFromCenter = CalculateAverageDistanceFromCenter();
                return averageDistanceFromCenter > _maxDistanceFromLeader * 0.8f;
            }

            return false;
        }

        /// <summary>
        /// Tính khoảng cách trung bình từ center
        /// </summary>
        private float CalculateAverageDistanceFromCenter()
        {
            if (_allUnits.Count == 0) return 0f;

            Vector3 center = CalculateSquadCenter();
            float totalDistance = 0f;

            foreach (var unit in _allUnits)
            {
                totalDistance += Vector3.Distance(unit.Position, center);
            }

            return totalDistance / _allUnits.Count;
        }

        /// <summary>
        /// Tính toán center của squad
        /// </summary>
        private Vector3 CalculateSquadCenter()
        {
            if (_allUnits.Count == 0) return transform.position;

            Vector3 sum = Vector3.zero;
            foreach (var unit in _allUnits)
            {
                sum += unit.Position;
            }

            return sum / _allUnits.Count;
        }

        /// <summary>
        /// Điều chỉnh đội hình động
        /// </summary>
        private void AdjustFormationDynamically()
        {
            // Chọn formation type phù hợp với tình huống
            FormationType newFormation = DetermineOptimalFormation();
            
            if (newFormation != _currentFormationType)
            {
                ChangeFormation(newFormation);
            }
        }

        /// <summary>
        /// Xác định formation tối ưu dựa trên tình huống
        /// </summary>
        private FormationType DetermineOptimalFormation()
        {
            // Logic đơn giản: nếu đang di chuyển thì dùng Column, nếu đứng yên thì dùng Line
            if (_isMoving)
            {
                return FormationType.Column;
            }
            else if (_squadTarget != null)
            {
                // Nếu có target thì dùng formation tấn công
                return FormationType.Line;
            }

            return _defaultFormationType;
        }

        /// <summary>
        /// Thay đổi đội hình
        /// </summary>
        [Button("Change Formation"), TitleGroup("Formation Control")]
        public void ChangeFormation(FormationType newFormation)
        {
            if (_currentFormationType == newFormation) return;

            FormationType oldFormation = _currentFormationType;
            _currentFormationType = newFormation;

            // Áp dụng formation mới cho tất cả units
            foreach (var unit in _allUnits)
            {
                var formationComponent = unit.Entity?.GetComponent<FormationComponent>();
                if (formationComponent != null)
                {
                    formationComponent.SetFormationType(newFormation, true); // smooth transition
                }
            }

            // Update squad model nếu có
            if (_squadModel != null)
            {
                _squadModel.SetFormation(newFormation);
            }

            // Trigger event
            OnFormationChanged?.Invoke(newFormation);

            Debug.Log($"SquadController: Squad {_squadId} đổi formation từ {oldFormation} sang {newFormation}");
        }

        #endregion

        #region Combat Coordination
        private void UpdateCombatCoordination()
        {
            if (!_autoTargetAssignment) return;
            UpdateSquadTarget();
            AssignTargetsToUnits();
        }

        private void UpdateSquadTarget()
        {
            if (_currentLeader?.Entity != null)
            {
                var nearbyEnemies = FindNearbyEnemies(_currentLeader.Position, _squadDetectionRange);
                
                if (nearbyEnemies.Count > 0)
                {
                    _squadTarget = nearbyEnemies
                        .OrderBy(e => Vector3.Distance(e.Position, _currentLeader.Position))
                        .First().Entity;
                }
                else
                {
                    _squadTarget = null;
                }
            }
        }

        /// <summary>
        /// Tìm enemies gần đó
        /// </summary>
        private List<UnitModel> FindNearbyEnemies(Vector3 position, float range)
        {
            // Placeholder - cần implement logic tìm kiếm enemy
            // Có thể sử dụng physics overlap hoặc spatial partitioning
            return new List<UnitModel>();
        }

        /// <summary>
        /// Phân công targets cho units
        /// </summary>
        private void AssignTargetsToUnits()
        {
            if (_squadTarget == null) return;

            // Assign target chung cho tất cả units
            foreach (var unit in _allUnits)
            {
                if (unit?.Entity == null) continue;

                var combatComponent = unit.Entity.GetComponent<CombatComponent>();
                if (combatComponent != null)
                {
                    // combatComponent.SetTarget(_squadTarget);
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Xử lý khi có unit mới được thêm vào squad
        /// </summary>
        private void HandleUnitAdded(UnitModel newUnit)
        {
            RefreshUnitList();
            
            // Nếu chưa có leader, chọn unit mới làm leader
            if (_currentLeader == null)
            {
                SetLeader(newUnit);
            }
            
            Debug.Log($"SquadController: Unit {newUnit.Entity.Id} được thêm vào squad {_squadId}");
        }

        /// <summary>
        /// Xử lý khi unit bị remove khỏi squad
        /// </summary>
        private void HandleUnitRemoved(UnitModel removedUnit)
        {
            RefreshUnitList();
            
            // Nếu leader bị remove, chọn leader mới
            if (_currentLeader == removedUnit)
            {
                _currentLeader = null;
                _currentLeaderId = -1;
                
                if (_autoSelectNewLeader && _allUnits.Count > 0)
                {
                    SelectInitialLeader();
                }
            }
            
            Debug.Log($"SquadController: Unit {removedUnit.Entity?.Id} bị remove khỏi squad {_squadId}");
        }

        /// <summary>
        /// Xử lý khi unit chết
        /// </summary>
        private void HandleUnitDied(UnitModel deadUnit)
        {
            HandleUnitRemoved(deadUnit); // Cùng logic với remove
        }

        /// <summary>
        /// Xử lý khi squad position thay đổi
        /// </summary>
        private void HandleSquadPositionChanged(Vector3 newPosition)
        {
            transform.position = newPosition;
            _formationNeedsAdjustment = true;
        }

        /// <summary>
        /// Xử lý khi formation thay đổi từ bên ngoài
        /// </summary>
        private void HandleFormationChanged(FormationType newFormation)
        {
            _currentFormationType = newFormation;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Refresh danh sách units và followers
        /// </summary>
        private void RefreshUnitList()
        {
            if (_squadModel == null) return;

            _allUnits.Clear();
            _allUnits.AddRange(_squadModel.Units);

            RefreshFollowersList();
        }

        /// <summary>
        /// Refresh danh sách followers (units không phải leader)
        /// </summary>
        private void RefreshFollowersList()
        {
            _followers.Clear();
            
            foreach (var unit in _allUnits)
            {
                if (unit != _currentLeader)
                {
                    _followers.Add(unit);
                }
            }
        }

        /// <summary>
        /// Update trạng thái squad
        /// </summary>
        private void UpdateSquadState()
        {
            if (_squadModel == null) return;

            // Update moving state
            Vector3 currentPosition = CalculateSquadCenter();
            _isMoving = Vector3.Distance(currentPosition, _lastSquadPosition) > 0.1f;
            _lastSquadPosition = currentPosition;

            // Update squad model position
            _squadModel.SetTargetPosition(currentPosition);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Lấy leader hiện tại
        /// </summary>
        public UnitModel GetCurrentLeader()
        {
            return _currentLeader;
        }

        /// <summary>
        /// Lấy danh sách followers
        /// </summary>
        public List<UnitModel> GetFollowers()
        {
            return new List<UnitModel>(_followers);
        }

        /// <summary>
        /// Kiểm tra squad có đang di chuyển không
        /// </summary>
        public bool IsSquadMoving()
        {
            return _isMoving;
        }

        /// <summary>
        /// Set target cho toàn squad
        /// </summary>
        public void SetSquadTarget(IEntity target)
        {
            _squadTarget = target;
        }

        /// <summary>
        /// Force chọn unit cụ thể làm leader
        /// </summary>
        public void ForceSetLeader(UnitModel newLeader)
        {
            if (newLeader != null && _allUnits.Contains(newLeader))
            {
                SetLeader(newLeader);
            }
        }

        #endregion

        #region Debug Tools

        [Button("Force Select New Leader"), TitleGroup("Debug Tools")]
        public void ForceSelectNewLeader()
        {
            SelectInitialLeader();
        }

        [Button("Test Formation Change"), TitleGroup("Debug Tools")]
        public void TestFormationChange()
        {
            FormationType[] formations = { FormationType.Line, FormationType.Column, FormationType.Circle };
            FormationType randomFormation = formations[UnityEngine.Random.Range(0, formations.Length)];
            ChangeFormation(randomFormation);
        }

        [Button("Show Squad Info"), TitleGroup("Debug Tools")]
        public void ShowSquadInfo()
        {
            string info = $"=== Squad {_squadId} Info ===\n";
            info += $"Total Units: {_allUnits.Count}\n";
            info += $"Leader: {(_currentLeader?.DisplayName ?? "None")}\n";
            info += $"Followers: {_followers.Count}\n";
            info += $"Formation: {_currentFormationType}\n";
            info += $"Is Moving: {_isMoving}\n";
            info += $"Squad Center: {CalculateSquadCenter()}\n";
            
            Debug.Log(info);
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup khi controller bị destroy
        /// </summary>
        private void Cleanup()
        {
            UnsubscribeFromSquadEvents();
            
            _allUnits.Clear();
            _followers.Clear();
            _unitTargets.Clear();
            
            _currentLeader = null;
            _squadModel = null;
            
            OnSquadDisbanded?.Invoke(this);
        }

        #endregion
    }
}