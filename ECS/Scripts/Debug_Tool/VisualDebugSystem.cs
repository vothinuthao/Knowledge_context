// VisualDebugSystem.cs - Hiển thị visual debug trong scene

using Components;
using Components.Squad;
using Core.ECS;
using Managers;
using Squad;
using UnityEngine;

namespace Debug_Tool
{
    public class VisualDebugSystem : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool showSquadGizmos = true;
        [SerializeField] private bool showTroopGizmos = true;
        [SerializeField] private bool showFormationGizmos = true;
        
        [Header("Colors")]
        [SerializeField] private Color squadColor = Color.blue;
        [SerializeField] private Color troopColor = Color.green;
        [SerializeField] private Color formationColor = Color.yellow;
        [SerializeField] private Color connectionColor = Color.red;
        
        private World _world;
        
        private void Start()
        {
            if (WorldManager.Instance != null)
            {
                _world = WorldManager.Instance.World;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (_world == null) return;
            
            if (showSquadGizmos)
            {
                DrawSquadGizmos();
            }
            
            if (showTroopGizmos)
            {
                DrawTroopGizmos();
            }
            
            if (showFormationGizmos)
            {
                DrawFormationGizmos();
            }
        }
        
        private void DrawSquadGizmos()
        {
            foreach (var squadEntity in _world.GetEntitiesWith<SquadComponent, PositionComponent>())
            {
                var squad = squadEntity.GetComponent<SquadComponent>();
                var position = squadEntity.GetComponent<PositionComponent>();
                
                // Draw squad center
                Gizmos.color = squadColor;
                Gizmos.DrawWireSphere(position.Position, 2.0f);
                
                // Draw squad info
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(position.Position + Vector3.up * 3, 
                    $"Squad {squadEntity.Id}\nMembers: {squad.MemberIds.Count}\nState: {squad.State}");
                #endif
            }
        }
        
        private void DrawTroopGizmos()
        {
            foreach (var troopEntity in _world.GetEntitiesWith<TroopComponent, PositionComponent>())
            {
                var troop = troopEntity.GetComponent<TroopComponent>();
                var position = troopEntity.GetComponent<PositionComponent>();
                
                // Draw troop position
                Gizmos.color = troopColor;
                Gizmos.DrawSphere(position.Position, 0.3f);
                
                // Draw troop info
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(position.Position + Vector3.up * 1, 
                    $"Troop {troopEntity.Id}\nSquad: {troop.SquadId}");
                #endif
                
                // Draw line to squad
                if (troop.SquadId != -1)
                {
                    Entity squadEntity = _world.GetEntityById(troop.SquadId);
                    if (squadEntity != null && squadEntity.HasComponent<PositionComponent>())
                    {
                        Vector3 squadPos = squadEntity.GetComponent<PositionComponent>().Position;
                        Gizmos.color = connectionColor;
                        Gizmos.DrawLine(position.Position, squadPos);
                    }
                }
            }
        }
        
        private void DrawFormationGizmos()
        {
            foreach (var squadEntity in _world.GetEntitiesWith<SquadFormationComponent, PositionComponent>())
            {
                var formation = squadEntity.GetComponent<SquadFormationComponent>();
                var position = squadEntity.GetComponent<PositionComponent>();
                
                // Draw formation grid
                Gizmos.color = formationColor;
                
                for (int row = 0; row < formation.Rows; row++)
                {
                    for (int col = 0; col < formation.Columns; col++)
                    {
                        Vector3 gridPos = formation.WorldPositions[row, col];
                        
                        // Draw grid position
                        Gizmos.DrawWireCube(gridPos, Vector3.one * 0.5f);
                        
                        // Mark occupied positions
                        if (formation.OccupiedPositions[row, col])
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawCube(gridPos, Vector3.one * 0.3f);
                            Gizmos.color = formationColor;
                        }
                    }
                }
            }
        }
    }
}