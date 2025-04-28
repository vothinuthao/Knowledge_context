// Debug script để kiểm tra vấn đề spawn squad

using Components;
using Components.Squad;
using Core.ECS;
using Core.Grid;
using Managers;
using Squad;
using UnityEngine;

namespace Debug_Tool
{
    public class SquadSpawnDebugger : MonoBehaviour
    {
        [SerializeField] private bool debugMode = true;
    
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                TestSquadSpawn();
            }
        
            if (Input.GetKeyDown(KeyCode.F3))
            {
                CheckSquadStatus();
            }
        }
    
        private void TestSquadSpawn()
        {
            Debug.Log("=== TESTING SQUAD SPAWN ===");
        
            // Check managers
            if (!GameManager.Instance)
            {
                Debug.LogError("GameManager is null!");
                return;
            }
        
            if (!WorldManager.Instance)
            {
                Debug.LogError("WorldManager is null!");
                return;
            }
        
            if (!GridManager.Instance)
            {
                Debug.LogError("GridManager is null!");
                return;
            }
        
            // Spawn test squad
            Vector3 spawnPos = GridManager.Instance.GetCellCenter(new Vector2Int(10, 10));
            Debug.Log($"Spawning squad at position: {spawnPos}");
        
            Entity squadEntity = GameManager.Instance.CreateSquad(null, spawnPos, Faction.PLAYER);
        
            if (squadEntity != null)
            {
                Debug.Log($"Squad created with ID: {squadEntity.Id}");
            
                // Check squad component
                var squadComponent = squadEntity.GetComponent<SquadComponent>();
                if (squadComponent != null)
                {
                    Debug.Log($"Squad has {squadComponent.MemberIds.Count} members");
                
                    // Check each member
                    for (int i = 0; i < squadComponent.MemberIds.Count; i++)
                    {
                        int memberId = squadComponent.MemberIds[i];
                        Entity memberEntity = WorldManager.Instance.World.GetEntityById(memberId);
                    
                        if (memberEntity != null)
                        {
                            Debug.Log($"Member {i} (ID: {memberId}) exists");
                        
                            // Check components
                            bool hasTroopComponent = memberEntity.HasComponent<TroopComponent>();
                            bool hasPositionComponent = memberEntity.HasComponent<PositionComponent>();
                            bool hasSquadMemberComponent = memberEntity.HasComponent<SquadMemberComponent>();
                        
                            Debug.Log($"  - Has TroopComponent: {hasTroopComponent}");
                            Debug.Log($"  - Has PositionComponent: {hasPositionComponent}");
                            Debug.Log($"  - Has SquadMemberComponent: {hasSquadMemberComponent}");
                        
                            if (hasPositionComponent)
                            {
                                Vector3 pos = memberEntity.GetComponent<PositionComponent>().Position;
                                Debug.Log($"  - Position: {pos}");
                            }
                        }
                        else
                        {
                            Debug.LogError($"Member {i} (ID: {memberId}) not found!");
                        }
                    }
                }
                else
                {
                    Debug.LogError("Squad component is null!");
                }
            }
            else
            {
                Debug.LogError("Failed to create squad!");
            }
        }
    
        private void CheckSquadStatus()
        {
            Debug.Log("=== CHECKING SQUAD STATUS ===");
        
            if (WorldManager.Instance == null || WorldManager.Instance.World == null)
            {
                Debug.LogError("World is not initialized!");
                return;
            }
        
            int squadCount = 0;
            int totalTroops = 0;
        
            foreach (var squadEntity in WorldManager.Instance.World.GetEntitiesWith<SquadComponent>())
            {
                squadCount++;
                var squadComponent = squadEntity.GetComponent<SquadComponent>();
            
                Debug.Log($"Squad {squadEntity.Id}:");
                Debug.Log($"  - Members: {squadComponent.MemberIds.Count}");
                Debug.Log($"  - State: {squadComponent.State}");
                Debug.Log($"  - Formation: {squadComponent.Formation}");
            
                totalTroops += squadComponent.MemberIds.Count;
            
                // Validate each member
                int validMembers = 0;
                foreach (var memberId in squadComponent.MemberIds)
                {
                    Entity memberEntity = WorldManager.Instance.World.GetEntityById(memberId);
                    if (memberEntity != null)
                    {
                        validMembers++;
                    }
                }
            
                Debug.Log($"  - Valid members: {validMembers}/{squadComponent.MemberIds.Count}");
            }
        
            Debug.Log($"Total squads: {squadCount}");
            Debug.Log($"Total troops: {totalTroops}");
        }
    
        private void OnGUI()
        {
            if (!debugMode) return;
        
            float y = 10;
            GUI.Box(new Rect(10, y, 200, 120), "Squad Debug");
        
            y += 25;
            if (GUI.Button(new Rect(20, y, 180, 25), "Spawn Test Squad (F2)"))
            {
                TestSquadSpawn();
            }
        
            y += 30;
            if (GUI.Button(new Rect(20, y, 180, 25), "Check Status (F3)"))
            {
                CheckSquadStatus();
            }
        
            y += 30;
            if (WorldManager.Instance != null && WorldManager.Instance.World != null)
            {
                int squadCount = 0;
                foreach (var squad in WorldManager.Instance.World.GetEntitiesWith<SquadComponent>())
                {
                    squadCount++;
                }
                GUI.Label(new Rect(20, y, 180, 20), $"Active Squads: {squadCount}");
            }
        }
    }
}