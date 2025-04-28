// Create DebugManager.cs - Hệ thống debug toàn diện

using System.Collections.Generic;
using System.Text;
using Components;
using Components.Squad;
using Core.ECS;
using Core.Grid;
using Managers;
using UnityEngine;

namespace Debug_Tool
{
    public class DebugManager : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private KeyCode toggleDebugKey = KeyCode.F1;
        
        [Header("Colors")]
        [SerializeField] private Color playerSquadColor = Color.blue;
        [SerializeField] private Color enemySquadColor = Color.red;
        [SerializeField] private Color formationColor = Color.green;
        [SerializeField] private Color pathColor = Color.yellow;
        
        private GameManager _gameManager;
        private WorldManager _worldManager;
        private GridManager _gridManager;
        
        // Debug info
        private Dictionary<int, SquadDebugInfo> _squadDebugInfo = new Dictionary<int, SquadDebugInfo>();
        private bool _debugEnabled = true;
        private Rect _debugWindowRect = new Rect(10, 10, 300, 500);
        
        private void Start()
        {
            _gameManager = GameManager.Instance;
            _worldManager = FindObjectOfType<WorldManager>();
            _gridManager = GridManager.Instance;
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(toggleDebugKey))
            {
                _debugEnabled = !_debugEnabled;
            }
            
            if (_debugEnabled)
            {
                UpdateDebugInfo();
            }
        }
        
        private void OnGUI()
        {
            if (!_debugEnabled || !showDebugUI) return;
            
            _debugWindowRect = GUI.Window(0, _debugWindowRect, DrawDebugWindow, "Debug Info");
        }
        
        private void DrawDebugWindow(int windowID)
        {
            GUILayout.Label($"FPS: {1.0f / Time.deltaTime:F0}");
            GUILayout.Label($"Entities: {_worldManager?.World?.GetEntityCount() ?? 0}");
            
            GUILayout.Space(10);
            GUILayout.Label("=== SQUADS ===");
            
            if (_worldManager != null && _worldManager.World != null)
            {
                foreach (var squadEntity in _worldManager.World.GetEntitiesWith<SquadComponent>())
                {
                    DrawSquadInfo(squadEntity);
                }
            }
            
            GUILayout.Space(10);
            if (GUILayout.Button("Spawn Test Squad"))
            {
                SpawnTestSquad();
            }
            
            if (GUILayout.Button("Check Squad Status"))
            {
                CheckSquadStatus();
            }
            
            GUI.DragWindow();
        }
        
        private void DrawSquadInfo(Entity squadEntity)
        {
            var squad = squadEntity.GetComponent<SquadComponent>();
            var position = squadEntity.GetComponent<PositionComponent>();
            
            GUILayout.Label($"Squad {squadEntity.Id}:");
            GUILayout.Label($"  - State: {squad.State}");
            GUILayout.Label($"  - Formation: {squad.Formation}");
            GUILayout.Label($"  - Members: {squad.MemberIds.Count}");
            GUILayout.Label($"  - Position: {position?.Position}");
            
            if (squad.State == SquadState.MOVING)
            {
                GUILayout.Label($"  - Target: {squad.TargetPosition}");
                GUILayout.Label($"  - Path Length: {squad.CurrentPath.Count}");
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!_debugEnabled || !showGizmos) return;
            if (_worldManager == null || _worldManager.World == null) return;
            
            foreach (var squadEntity in _worldManager.World.GetEntitiesWith<SquadComponent>())
            {
                DrawSquadGizmos(squadEntity);
            }
        }
        
        private void DrawSquadGizmos(Entity squadEntity)
        {
            var squad = squadEntity.GetComponent<SquadComponent>();
            var position = squadEntity.GetComponent<PositionComponent>();
            
            if (position == null) return;
            
            // Draw squad position
            Gizmos.color = playerSquadColor;
            Gizmos.DrawWireSphere(position.Position, 1.0f);
            
            // Draw formation
            Gizmos.color = formationColor;
            foreach (var offset in squad.MemberOffsets)
            {
                Vector3 worldPos = position.Position + squad.FormationRotation * offset;
                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.3f);
            }
            
            // Draw path
            if (squad.State == SquadState.MOVING && squad.CurrentPath.Count > 0)
            {
                Gizmos.color = pathColor;
                Vector3 lastPos = position.Position;
                
                for (int i = squad.PathIndex; i < squad.CurrentPath.Count; i++)
                {
                    Vector3 cellPos = _gridManager.GetCellCenter(squad.CurrentPath[i]);
                    Gizmos.DrawLine(lastPos, cellPos);
                    lastPos = cellPos;
                }
            }
            
            // Draw troops
            foreach (var memberId in squad.MemberIds)
            {
                Entity memberEntity = _worldManager.World.GetEntityById(memberId);
                if (memberEntity != null && memberEntity.HasComponent<PositionComponent>())
                {
                    Vector3 memberPos = memberEntity.GetComponent<PositionComponent>().Position;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(memberPos, 0.2f);
                }
            }
        }
        
        private void UpdateDebugInfo()
        {
            // Update debug info for all squads
            _squadDebugInfo.Clear();
            
            if (_worldManager != null && _worldManager.World != null)
            {
                foreach (var squadEntity in _worldManager.World.GetEntitiesWith<SquadComponent>())
                {
                    _squadDebugInfo[squadEntity.Id] = GatherSquadDebugInfo(squadEntity);
                }
            }
        }
        
        private SquadDebugInfo GatherSquadDebugInfo(Entity squadEntity)
        {
            var squad = squadEntity.GetComponent<SquadComponent>();
            var position = squadEntity.GetComponent<PositionComponent>();
            
            var info = new SquadDebugInfo
            {
                SquadId = squadEntity.Id,
                State = squad.State,
                Formation = squad.Formation,
                MemberCount = squad.MemberIds.Count,
                Position = position?.Position ?? Vector3.zero,
                TargetPosition = squad.TargetPosition,
                PathLength = squad.CurrentPath.Count
            };
            
            return info;
        }
        
        private void SpawnTestSquad()
        {
            if (_gameManager != null)
            {
                Vector3 spawnPos = _gridManager.GetCellCenter(new Vector2Int(10, 10));
                var squadEntity = _gameManager.CreateSquad(null, spawnPos, Faction.PLAYER);
                
                Debug.Log($"Spawned test squad with ID: {squadEntity?.Id ?? -1}");
            }
        }
        
        private void CheckSquadStatus()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== SQUAD STATUS REPORT ===");
            
            if (_worldManager != null && _worldManager.World != null)
            {
                var squads = _worldManager.World.GetEntitiesWith<SquadComponent>();
                int squadCount = 0;
                
                foreach (var squadEntity in squads)
                {
                    squadCount++;
                    var squad = squadEntity.GetComponent<SquadComponent>();
                    sb.AppendLine($"Squad {squadEntity.Id}:");
                    sb.AppendLine($"  - Members: {squad.MemberIds.Count}");
                    sb.AppendLine($"  - State: {squad.State}");
                    sb.AppendLine($"  - Formation: {squad.Formation}");
                    
                    // Check each member
                    int validMembers = 0;
                    foreach (var memberId in squad.MemberIds)
                    {
                        Entity memberEntity = _worldManager.World.GetEntityById(memberId);
                        if (memberEntity != null)
                        {
                            validMembers++;
                        }
                    }
                    
                    sb.AppendLine($"  - Valid Members: {validMembers}/{squad.MemberIds.Count}");
                }
                
                sb.AppendLine($"Total Squads: {squadCount}");
            }
            else
            {
                sb.AppendLine("World or WorldManager is null!");
            }
            
            Debug.Log(sb.ToString());
        }
    }
    
    // Debug info struct
    public struct SquadDebugInfo
    {
        public int SquadId;
        public SquadState State;
        public FormationType Formation;
        public int MemberCount;
        public Vector3 Position;
        public Vector3 TargetPosition;
        public int PathLength;
    }
}