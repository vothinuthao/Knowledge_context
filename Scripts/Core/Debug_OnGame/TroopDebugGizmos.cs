using System.Collections.Generic;
using SteeringBehavior;
using Troop;
using UnityEngine;

namespace Core.Debug_OnGame
{
    public class TroopDebugGizmos : MonoBehaviour
    {
        [Header("Debug Settings")]
        public bool enableDebug = true;
        
        [Header("Behavior Visualization")]
        public bool showSteeringForces = true;
        public bool showTargetLines = true;
        public bool showDetectionRadius = true;
        public bool showStateLabels = true;
        public bool showHealth = true;
        
        [Header("Force Visualization")]
        public float forceScale = 2.0f;
        public Color seekColor = new Color(0, 1, 0, 0.7f);       // Green
        public Color fleeColor = new Color(1, 0, 0, 0.7f);       // Red
        public Color separationColor = new Color(1, 1, 0, 0.7f); // Yellow
        public Color cohesionColor = new Color(0, 1, 1, 0.7f);   // Cyan
        public Color alignmentColor = new Color(0, 0, 1, 0.7f);  // Blue
        public Color resultantColor = new Color(1, 1, 1, 1.0f);  // White
        
        [Header("State Visualization")]
        public Color idleStateColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);   // Gray
        public Color movingStateColor = new Color(0, 0.8f, 0, 0.7f);      // Green
        public Color attackingStateColor = new Color(0.8f, 0, 0, 0.7f);    // Red
        public Color defendingStateColor = new Color(0, 0, 0.8f, 0.7f);    // Blue
        public Color fleeingStateColor = new Color(1, 0.5f, 0, 0.7f);     // Orange
        public Color stunnedStateColor = new Color(0.8f, 0, 0.8f, 0.7f);   // Purple
        
        // Dictionary để lưu trữ debug data cho mỗi troop
        private Dictionary<TroopController, TroopDebugData> _troopDebugData = new Dictionary<TroopController, TroopDebugData>();
        
        // Font cho labels
        private GUIStyle _labelStyle;
        private GUIStyle _healthStyle;
        
        // Logo của mỗi state
        private Dictionary<TroopState, string> _stateIcons = new Dictionary<TroopState, string>();
        
        private void Start()
        {
            // Khởi tạo style cho labels
            _labelStyle = new GUIStyle();
            _labelStyle.normal.textColor = Color.white;
            _labelStyle.fontSize = 12;
            _labelStyle.fontStyle = FontStyle.Bold;
            _labelStyle.alignment = TextAnchor.MiddleCenter;
            _labelStyle.normal.background = Texture2D.blackTexture;
            
            // Style cho health bar
            _healthStyle = new GUIStyle();
            _healthStyle.normal.background = Texture2D.whiteTexture;
            _healthStyle.fontSize = 10;
            _healthStyle.fontStyle = FontStyle.Bold;
            _healthStyle.alignment = TextAnchor.MiddleCenter;
            
            // Khởi tạo icons cho mỗi state
            _stateIcons[TroopState.Idle] = "⚋";
            _stateIcons[TroopState.Moving] = "➡";
            _stateIcons[TroopState.Attacking] = "⚔";
            _stateIcons[TroopState.Defending] = "🛡";
            _stateIcons[TroopState.Fleeing] = "⚡";
            _stateIcons[TroopState.Stunned] = "✱";
            _stateIcons[TroopState.Knockback] = "↩";
            _stateIcons[TroopState.Dead] = "✝";
        }
        
        private void LateUpdate()
        {
            if (!enableDebug) return;
            
            // Lấy tất cả troops trong scene
            TroopController[] allTroops = FindObjectsOfType<TroopController>();
            
            // Cập nhật debug data cho mỗi troop
            foreach (var troop in allTroops)
            {
                if (!troop || !troop.IsAlive()) continue;
                
                if (!_troopDebugData.ContainsKey(troop))
                {
                    _troopDebugData[troop] = new TroopDebugData();
                }
                
                // Lấy debug data của troop
                TroopDebugData debugData = _troopDebugData[troop];
                
                // Lấy model và context
                TroopModel model = troop.GetModel();
                SteeringContext context = troop.SteeringContext;
                
                if (model == null || context == null) continue;
                
                // Update debug data
                debugData.position = troop.GetPosition();
                debugData.velocity = model.Velocity;
                debugData.targetPosition = troop.GetTargetPosition();
                debugData.state = troop.GetState();
                debugData.health = model.CurrentHealth;
                debugData.maxHealth = model.MaxHealth;
                
                // Lấy các force từ các behavior riêng lẻ
                debugData.behaviorForces.Clear();
                
                if (model.SteeringBehavior != null)
                {
                    // Lấy từng behavior và force tương ứng
                    foreach (ISteeringBehavior behavior in model.SteeringBehavior.GetSteeringBehaviors())
                    {
                        if (behavior.IsEnabled())
                        {
                            // Tính force của behavior này
                            Vector3 force = behavior.Execute(context);
                            
                            // Thêm vào dictionary
                            debugData.behaviorForces[behavior.GetName()] = force;
                        }
                    }
                }
            }
            
            // Xóa debug data của các troop đã chết hoặc bị hủy
            List<TroopController> troopsToRemove = new List<TroopController>();
            
            foreach (var troop in _troopDebugData.Keys)
            {
                if (troop == null || !troop.IsAlive())
                {
                    troopsToRemove.Add(troop);
                }
            }
            
            foreach (var troop in troopsToRemove)
            {
                _troopDebugData.Remove(troop);
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!enableDebug || !Application.isPlaying) return;
            
            // Vẽ gizmos cho mỗi troop
            foreach (var kvp in _troopDebugData)
            {
                TroopController troop = kvp.Key;
                TroopDebugData data = kvp.Value;
                
                if (troop == null) continue;
                
                // Vẽ detection radius
                if (showDetectionRadius)
                {
                    DrawDetectionRadius(data);
                }
                
                // Vẽ target line
                if (showTargetLines)
                {
                    DrawTargetLine(data);
                }
                
                // Vẽ steering forces
                if (showSteeringForces)
                {
                    DrawSteeringForces(data);
                }
                
                // Vẽ state circle
                DrawStateCircle(data);
            }
        }
        
        private void OnGUI()
        {
            if (!enableDebug || !showStateLabels && !showHealth) return;
            
            // Vẽ labels cho mỗi troop
            foreach (var kvp in _troopDebugData)
            {
                TroopController troop = kvp.Key;
                TroopDebugData data = kvp.Value;
                
                if (troop == null) continue;
                
                // Chuyển đổi vị trí 3D sang 2D trên màn hình
                Vector3 screenPos = Camera.main.WorldToScreenPoint(data.position + Vector3.up * 1.5f);
                
                // Skip nếu troop không nằm trong viewport
                if (screenPos.z < 0) continue;
                
                // Vẽ state label
                if (showStateLabels)
                {
                    DrawStateLabel(data, screenPos);
                }
                
                // Vẽ health bar
                if (showHealth)
                {
                    DrawHealthBar(data, screenPos);
                }
            }
        }
        
        private void DrawDetectionRadius(TroopDebugData data)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            
            // Detection radius mặc định
            float radius = 5f;
            
            // Vẽ circle
            DrawCircle(data.position, radius, 16);
        }
        
        private void DrawTargetLine(TroopDebugData data)
        {
            // Vẽ line từ troop đến target
            Gizmos.color = Color.white;
            Gizmos.DrawLine(data.position, data.targetPosition);
            
            // Vẽ marker tại target position
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(data.targetPosition, 0.2f);
        }
        
        private void DrawSteeringForces(TroopDebugData data)
        {
            // Vẽ vector velocity
            Gizmos.color = resultantColor;
            Gizmos.DrawRay(data.position, data.velocity * forceScale);
            
            // Vẽ từng loại force
            foreach (var kvp in data.behaviorForces)
            {
                string behaviorName = kvp.Key;
                Vector3 force = kvp.Value;
                
                // Set màu dựa trên loại behavior
                Gizmos.color = GetBehaviorColor(behaviorName);
                
                // Vẽ force vector
                Gizmos.DrawRay(data.position, force * forceScale);
            }
        }
        
        private void DrawStateCircle(TroopDebugData data)
        {
            // Lấy màu tương ứng với state
            Gizmos.color = GetStateColor(data.state);
            
            // Vẽ circle để hiển thị state
            Gizmos.DrawSphere(data.position + Vector3.up * 0.1f, 0.3f);
        }
        
        private void DrawStateLabel(TroopDebugData data, Vector3 screenPos)
        {
            // Lấy icon cho state
            string stateIcon = _stateIcons.ContainsKey(data.state) ? _stateIcons[data.state] : "?";
            string stateText = data.state.ToString();
            
            // Set màu cho label
            _labelStyle.normal.textColor = GetStateColor(data.state);
            
            // Vẽ label
            GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 30, 100, 20), 
                stateIcon + " " + stateText, _labelStyle);
        }
        
        private void DrawHealthBar(TroopDebugData data, Vector3 screenPos)
        {
            // Tính tỷ lệ health
            float healthRatio = data.health / data.maxHealth;
            
            // Vẽ health bar background
            GUI.color = Color.red;
            GUI.Box(new Rect(screenPos.x - 25, Screen.height - screenPos.y - 10, 50, 5), "", _healthStyle);
            
            // Vẽ health bar foreground
            GUI.color = Color.green;
            GUI.Box(new Rect(screenPos.x - 25, Screen.height - screenPos.y - 10, 50 * healthRatio, 5), "", _healthStyle);
        }
        
        private Color GetBehaviorColor(string behaviorName)
        {
            // Chọn màu dựa trên tên behavior
            if (behaviorName.Contains("Seek")) return seekColor;
            if (behaviorName.Contains("Flee")) return fleeColor;
            if (behaviorName.Contains("Separation")) return separationColor;
            if (behaviorName.Contains("Cohesion")) return cohesionColor;
            if (behaviorName.Contains("Alignment")) return alignmentColor;
            
            // Màu mặc định
            return new Color(0.8f, 0.8f, 0.8f, 0.7f);
        }
        
        private Color GetStateColor(TroopState state)
        {
            switch (state)
            {
                case TroopState.Idle: return idleStateColor;
                case TroopState.Moving: return movingStateColor;
                case TroopState.Attacking: return attackingStateColor;
                case TroopState.Defending: return defendingStateColor;
                case TroopState.Fleeing: return fleeingStateColor;
                case TroopState.Stunned: return stunnedStateColor;
                default: return Color.white;
            }
        }
        
        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            // Vẽ circle bằng cách nối các đoạn thẳng
            float angle = 0f;
            float angleStep = 2 * Mathf.PI / segments;
            
            Vector3 previousPoint = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            
            for (int i = 0; i <= segments; i++)
            {
                angle += angleStep;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }
        }
        
        // Class lưu trữ debug data cho mỗi troop
        private class TroopDebugData
        {
            public Vector3 position;
            public Vector3 velocity;
            public Vector3 targetPosition;
            public TroopState state;
            public float health;
            public float maxHealth;
            public Dictionary<string, Vector3> behaviorForces = new Dictionary<string, Vector3>();
        }
    }
}
    