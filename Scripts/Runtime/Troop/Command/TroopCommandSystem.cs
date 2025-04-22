using System.Collections.Generic;
using UnityEngine;

namespace Troop
{
    /// <summary>
    /// System that manages commands for troops and squads
    /// </summary>
    public class TroopCommandSystem : MonoBehaviour
    {
        [Header("Squad Management")]
        [SerializeField] private SquadSystem[] squads;
        [SerializeField] private bool autoDetectSquads = true;
    
        [Header("Command Flags")]
        [SerializeField] private bool troopsStandStillByDefault = true;
        [SerializeField] private bool troopsOnlyMoveWithCommands = true;
        [SerializeField] private bool autodetectEnemies = true;
    
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
    
        // Track command state for each squad
        private Dictionary<SquadSystem, SquadCommandState> squadCommands = new Dictionary<SquadSystem, SquadCommandState>();
    
        // Class to track command state for a squad
        private class SquadCommandState
        {
            public Vector3 moveTarget = Vector3.zero;
            public bool isMoving = false;
            public bool isDefending = false;
            public bool isAttacking = false;
            public TroopController targetEnemy = null;
        }
    
        private void Awake()
        {
            if (autoDetectSquads)
            {
                squads = FindObjectsOfType<SquadSystem>();
            }
        
            // Initialize command states for all squads
            foreach (var squad in squads)
            {
                squadCommands[squad] = new SquadCommandState();
            }
        }
    
        private void Start()
        {
            // If we want troops to stand still by default, disable their movement behaviors
            if (troopsStandStillByDefault)
            {
                foreach (var squad in squads)
                {
                    DisableMovementForSquad(squad);
                }
            }
        }
    
        private void Update()
        {
            // Process commands for each squad
            foreach (var squad in squads)
            {
                ProcessSquadCommands(squad);
            }
        
            // Auto-detect enemies if enabled
            if (autodetectEnemies)
            {
                DetectEnemiesForAllSquads();
            }
        }
    
        // Disable movement behaviors for all troops in a squad
        private void DisableMovementForSquad(SquadSystem squad)
        {
            if (squad == null) return;
        
            var troops = squad.GetTroops();
            foreach (var troop in troops)
            {
                if (troop != null)
                {
                    // Disable movement behaviors but keep other behaviors like separation active
                    troop.EnableBehavior("Seek", false);
                    troop.EnableBehavior("Arrival", false);
                
                    // Still keep these behaviors that don't cause proactive movement
                    troop.EnableBehavior("Separation", true);
                    troop.EnableBehavior("Cohesion", true);
                    troop.EnableBehavior("Alignment", true);
                }
            }
        }
    
        // Enable movement behaviors for all troops in a squad
        private void EnableMovementForSquad(SquadSystem squad)
        {
            if (squad == null) return;
        
            var troops = squad.GetTroops();
            foreach (var troop in troops)
            {
                if (troop != null)
                {
                    // Enable movement behaviors
                    troop.EnableBehavior("Seek", true);
                    troop.EnableBehavior("Arrival", true);
                }
            }
        }
    
        // Process commands for a squad
        private void ProcessSquadCommands(SquadSystem squad)
        {
            if (squad == null || !squadCommands.TryGetValue(squad, out SquadCommandState state))
                return;
        
            // If the squad is moving, ensure movement behaviors are enabled
            if (state.isMoving)
            {
                EnableMovementForSquad(squad);
            
                // Check if the squad has reached its destination
                float distanceToTarget = Vector3.Distance(squad.transform.position, state.moveTarget);
                if (distanceToTarget < 1.0f)
                {
                    // Stop movement when destination is reached
                    state.isMoving = false;
                
                    if (troopsOnlyMoveWithCommands)
                    {
                        DisableMovementForSquad(squad);
                    }
                
                    if (debugMode)
                    {
                        Debug.Log($"Squad reached destination: {state.moveTarget}");
                    }
                }
            }
        
            // If the squad is attacking, update troop states accordingly
            if (state.isAttacking && state.targetEnemy != null)
            {
                var troops = squad.GetTroops();
                foreach (var troop in troops)
                {
                    if (troop != null && troop.IsAlive())
                    {
                        // If troop is close enough to the enemy, switch to attacking state
                        float distanceToEnemy = Vector3.Distance(troop.GetPosition(), state.targetEnemy.GetPosition());
                        if (distanceToEnemy <= troop.GetModel().AttackRange * 1.5f)
                        {
                            if (troop.GetState() != TroopState.Attacking)
                            {
                                troop.StateMachine.ChangeState<AttackingState>();
                            }
                        }
                        else
                        {
                            // If not in range, ensure movement is enabled to approach the enemy
                            troop.EnableBehavior("Seek", true);
                            troop.EnableBehavior("Arrival", true);
                            troop.SetTargetPosition(state.targetEnemy.GetPosition());
                        }
                    }
                }
            }
        
            // If the squad is defending, update troop states accordingly
            if (state.isDefending)
            {
                var troops = squad.GetTroops();
                foreach (var troop in troops)
                {
                    if (troop != null && troop.IsAlive())
                    {
                        // Switch to defending state and enable relevant behaviors
                        if (troop.GetState() != TroopState.Defending)
                        {
                            troop.StateMachine.ChangeState<DefendingState>();
                        }
                    
                        // Ensure troops maintain position
                        troop.EnableBehavior("Seek", false);
                        troop.EnableBehavior("Arrival", true);
                    
                        // Enable defensive formation behaviors if available
                        if (troop.IsBehaviorEnabled("Phalanx"))
                            troop.EnableBehavior("Phalanx", true);
                    
                        if (troop.IsBehaviorEnabled("Testudo"))
                            troop.EnableBehavior("Testudo", true);
                    
                        if (troop.IsBehaviorEnabled("Protect"))
                            troop.EnableBehavior("Protect", true);
                    }
                }
            }
        }
    
        // Detect enemies for all squads
        private void DetectEnemiesForAllSquads()
        {
            foreach (var squad in squads)
            {
                if (squad == null) continue;
            
                var troops = squad.GetTroops();
                if (troops.Count == 0) continue;
            
                // Use the first troop in the squad to get nearby enemies
                var firstTroop = troops[0];
                if (firstTroop == null) continue;
            
                var nearbyEnemies = firstTroop.SteeringContext.NearbyEnemies;
                if (nearbyEnemies == null || nearbyEnemies.Length == 0) continue;
            
                // Find the closest enemy
                TroopController closestEnemy = null;
                float closestDistance = float.MaxValue;
            
                foreach (var enemy in nearbyEnemies)
                {
                    if (enemy == null || !enemy.IsAlive()) continue;
                
                    float distance = Vector3.Distance(squad.transform.position, enemy.GetPosition());
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy;
                    }
                }
            
                // If there's a close enemy and we're not already attacking, start attacking
                if (closestEnemy != null && closestDistance < 10f)
                {
                    if (squadCommands.TryGetValue(squad, out SquadCommandState state))
                    {
                        if (!state.isAttacking)
                        {
                            state.isAttacking = true;
                            state.targetEnemy = closestEnemy;
                        
                            if (debugMode)
                            {
                                Debug.Log($"Squad auto-detected enemy: {closestEnemy.name} at distance {closestDistance}");
                            }
                        }
                    }
                }
            }
        }
    
        // PUBLIC API
    
        // Command a squad to move to a position
        public void CommandSquadMove(SquadSystem squad, Vector3 targetPosition)
        {
            if (squad == null) return;
        
            if (squadCommands.TryGetValue(squad, out SquadCommandState state))
            {
                state.moveTarget = targetPosition;
                state.isMoving = true;
                state.isDefending = false;
                state.isAttacking = false;
            
                // Move the squad to the target position
                squad.MoveToPosition(targetPosition);
            
                // Enable movement behaviors for all troops
                EnableMovementForSquad(squad);
            
                if (debugMode)
                {
                    Debug.Log($"Commanding squad to move to: {targetPosition}");
                }
            }
        }
    
        // Command a squad to attack an enemy
        public void CommandSquadAttack(SquadSystem squad, TroopController enemy)
        {
            if (squad == null || enemy == null) return;
        
            if (squadCommands.TryGetValue(squad, out SquadCommandState state))
            {
                state.targetEnemy = enemy;
                state.isAttacking = true;
                state.isDefending = false;
            
                // Move the squad towards the enemy
                state.moveTarget = enemy.GetPosition();
                state.isMoving = true;
            
                // Enable movement behaviors for all troops
                EnableMovementForSquad(squad);
            
                // Set all troops to target the enemy
                var troops = squad.GetTroops();
                foreach (var troop in troops)
                {
                    if (troop != null)
                    {
                        troop.SetTargetPosition(enemy.GetPosition());
                    
                        // Enable attack behaviors if available
                        if (troop.IsBehaviorEnabled("Charge"))
                            troop.EnableBehavior("Charge", true);
                    
                        if (troop.IsBehaviorEnabled("Jump Attack"))
                            troop.EnableBehavior("Jump Attack", true);
                    }
                }
            
                if (debugMode)
                {
                    Debug.Log($"Commanding squad to attack: {enemy.name}");
                }
            }
        }
    
        // Command a squad to defend its current position
        public void CommandSquadDefend(SquadSystem squad)
        {
            if (squad == null) return;
        
            if (squadCommands.TryGetValue(squad, out SquadCommandState state))
            {
                state.isDefending = true;
                state.isAttacking = false;
                state.isMoving = false;
            
                // Put all troops in defensive state
                var troops = squad.GetTroops();
                foreach (var troop in troops)
                {
                    if (troop != null)
                    {
                        troop.StateMachine.ChangeState<DefendingState>();
                    
                        // Get the troop's designated position in the squad
                        var squadPos = TroopControllerSquadExtensions.Instance.GetSquadPosition(troop);
                        Vector3 squadTargetPos = squad.GetPositionForTroop(squad, squadPos.x, squadPos.y);
                        troop.SetTargetPosition(squadTargetPos);
                    }
                }
            
                if (debugMode)
                {
                    Debug.Log($"Commanding squad to defend current position");
                }
            }
        }
    
        // Stop all commands for a squad
        public void CommandSquadStop(SquadSystem squad)
        {
            if (squad == null) return;
        
            if (squadCommands.TryGetValue(squad, out SquadCommandState state))
            {
                state.isDefending = false;
                state.isAttacking = false;
                state.isMoving = false;
            
                // Disable movement behaviors if we only want troops to move with commands
                if (troopsOnlyMoveWithCommands)
                {
                    DisableMovementForSquad(squad);
                }
            
                // Set all troops to idle state
                var troops = squad.GetTroops();
                foreach (var troop in troops)
                {
                    if (troop != null)
                    {
                        troop.StateMachine.ChangeState<IdleState>();
                    
                        // Disable attack-specific behaviors
                        if (troop.IsBehaviorEnabled("Charge"))
                            troop.EnableBehavior("Charge", false);
                    
                        if (troop.IsBehaviorEnabled("Jump Attack"))
                            troop.EnableBehavior("Jump Attack", false);
                    }
                }
            
                if (debugMode)
                {
                    Debug.Log("Commanding squad to stop all actions");
                }
            }
        }
    }
}