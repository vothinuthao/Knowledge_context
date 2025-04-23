using Troop;
using UnityEditor;
using UnityEngine;

namespace Core.Debug_OnGame
{
    public class TroopDebugInfo : MonoBehaviour
    {
        [Header("Debug Settings")]
        public bool showDebugInfo = true;
        
        private TroopController _troopController;
        private Vector2 _scrollPosition;
        
        private void Start()
        {
            _troopController = GetComponent<TroopController>();
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo || !_troopController) return;
            
            // Vẽ debug window
            GUI.Window(GetInstanceID(), new Rect(10, 10, 300, 300), DrawDebugWindow, $"Debug: {gameObject.name}");
        }
        
        private void DrawDebugWindow(int windowID)
        {
            // Bắt đầu scroll view
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            
            GUILayout.Label($"State: {_troopController.GetState()}");
            GUILayout.Label($"Health: {_troopController.GetModel().CurrentHealth}/{_troopController.GetModel().MaxHealth}");
            GUILayout.Label($"Position: {_troopController.GetPosition()}");
            GUILayout.Label($"Velocity: {_troopController.GetModel().Velocity.magnitude.ToString("F2")}");
            GUILayout.Label($"Target Position: {_troopController.GetTargetPosition()}");
            
            // Hiển thị behaviors
            GUILayout.Label("Behaviors:", EditorStyles.boldLabel);
            
            foreach (var behavior in _troopController.GetModel().SteeringBehavior.GetSteeringBehaviors())
            {
                string status = behavior.IsEnabled() ? "Enabled" : "Disabled";
                GUILayout.Label($"- {behavior.GetName()}: {status} (Weight: {behavior.GetWeight()})");
            }
            
            GUILayout.EndScrollView();
            
            // Thêm nút để bật/tắt behaviors
            if (GUILayout.Button("Toggle Debug Mode"))
            {
                showDebugInfo = !showDebugInfo;
            }
            
            // Cho phép kéo window
            GUI.DragWindow();
        }
    }
    }
