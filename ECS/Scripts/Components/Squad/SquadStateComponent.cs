// // ECS/Scripts/Components/Squad/SquadStateComponent.cs
// using Core.ECS;
// using UnityEngine;
//
// namespace Squad
// {
//     /// <summary>
//     /// Enum for squad states
//     /// </summary>
//     public enum SquadState
//     {
//         Idle,
//         Moving,
//         Attacking,
//         Defending
//     }
//     
//     /// <summary>
//     /// Component for squad state data
//     /// </summary>
//     public class SquadStateComponent : IComponent
//     {
//         // Current state of the squad
//         private SquadState _currentState = SquadState.Idle;
//         
//         // FIX: Thêm event/delegate để thông báo khi state thay đổi
//         public delegate void StateChangedHandler(SquadState oldState, SquadState newState);
//         public event StateChangedHandler OnStateChanged;
//         
//         // FIX: Property với setter được cải thiện để phát event khi thay đổi state
//         public SquadState CurrentState 
//         { 
//             get { return _currentState; }
//             set 
//             {
//                 if (_currentState != value)
//                 {
//                     SquadState oldState = _currentState;
//                     _currentState = value;
//                     
//                     // Phát event khi state thay đổi
//                     OnStateChanged?.Invoke(oldState, _currentState);
//                     
//                     // Reset thời gian trong state khi state thay đổi
//                     TimeInCurrentState = 0f;
//                 }
//             }
//         }
//         
//         // Target position for movement
//         public Vector3 TargetPosition { get; set; }
//         
//         // Target entity ID for attack
//         public int TargetEntityId { get; set; } = -1;
//         
//         // Whether the squad is currently moving
//         public bool IsMoving => CurrentState == SquadState.Moving;
//         
//         // Whether troops should be locked in position
//         public bool ShouldLockTroops => CurrentState == SquadState.Idle;
//         
//         // FIX: Thêm tracking thời gian ở state hiện tại
//         public float TimeInCurrentState { get; set; } = 0f;
//         
//         // FIX: Thêm thông tin về vị trí trước đó và hướng di chuyển
//         public Vector3 PreviousPosition { get; set; } = Vector3.zero;
//         public Vector3 MovementDirection { get; set; } = Vector3.forward;
//         
//         // FIX: Thêm thông tin về đội hình trong các state khác nhau
//         public float FormationSpacing { get; set; } = 1.5f;
//         public float MovingFormationSpacing { get; set; } = 2.0f; // Khoảng cách lớn hơn khi di chuyển
//         public float DefendingFormationSpacing { get; set; } = 1.0f; // Khoảng cách nhỏ hơn khi phòng thủ
//         
//         // FIX: Thêm thông tin về tốc độ di chuyển cho các state khác nhau
//         public float MovementSpeedMultiplier 
//         {
//             get
//             {
//                 switch (CurrentState)
//                 {
//                     case SquadState.Moving: return 1.0f;
//                     case SquadState.Attacking: return 1.2f; // Nhanh hơn khi tấn công
//                     case SquadState.Defending: return 0.5f; // Chậm hơn khi phòng thủ
//                     case SquadState.Idle:
//                     default: return 0.8f;
//                 }
//             }
//         }
//         
//         public SquadStateComponent()
//         {
//             TargetPosition = Vector3.zero;
//         }
//         
//         /// <summary>
//         /// FIX: Cập nhật tracking thời gian trong state
//         /// </summary>
//         public void UpdateTime(float deltaTime)
//         {
//             TimeInCurrentState += deltaTime;
//         }
//         
//         /// <summary>
//         /// FIX: Cập nhật thông tin di chuyển
//         /// </summary>
//         public void UpdateMovementInfo(Vector3 currentPosition)
//         {
//             // Nếu là lần đầu gọi, khởi tạo PreviousPosition
//             if (PreviousPosition == Vector3.zero)
//             {
//                 PreviousPosition = currentPosition;
//                 return;
//             }
//             
//             // Tính toán hướng di chuyển
//             Vector3 movementVector = currentPosition - PreviousPosition;
//             
//             // Chỉ cập nhật hướng di chuyển nếu di chuyển đủ xa
//             if (movementVector.magnitude > 0.1f)
//             {
//                 MovementDirection = movementVector.normalized;
//             }
//             
//             // Cập nhật vị trí trước đó
//             PreviousPosition = currentPosition;
//         }
//         
//         /// <summary>
//         /// FIX: Lấy khoảng cách thích hợp cho đội hình dựa trên state hiện tại
//         /// </summary>
//         public float GetCurrentFormationSpacing()
//         {
//             switch (CurrentState)
//             {
//                 case SquadState.Moving:
//                     return MovingFormationSpacing;
//                 case SquadState.Defending:
//                     return DefendingFormationSpacing;
//                 case SquadState.Idle:
//                 case SquadState.Attacking:
//                 default:
//                     return FormationSpacing;
//             }
//         }
//     }
// }