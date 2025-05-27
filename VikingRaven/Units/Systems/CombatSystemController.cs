// using System.Collections.Generic;
// using Sirenix.OdinInspector;
// using UnityEngine;
// using VikingRaven.Core.Factory;
// using VikingRaven.Units.Components;
// using VikingRaven.Units.Models;
//
// namespace VikingRaven.Units.Systems
// {
//     /// <summary>
//     /// Combat system controller that handles squad movement and combat operations
//     /// Acts as a bridge between UI controllers and the squad model system
//     /// </summary>
//     public class CombatSystemController : MonoBehaviour
//     {
//         [TitleGroup("Factory References")]
//         [Tooltip("Reference to the enhanced unit factory")]
//         [SerializeField] private UnitFactory _unitFactory;
//         
//         [Tooltip("Update interval for processing squad states")]
//         [SerializeField] private float _updateInterval = 0.2f;
//         
//         [TitleGroup("Combat Settings")]
//         [Tooltip("Maximum aggro range for auto-detection")]
//         [SerializeField, Range(1f, 20f)] private float _maxAggroRange = 10f;
//         
//         [Tooltip("Auto-engage enemies when in range")]
//         [SerializeField] private bool _autoEngageEnemies = true;
//         
//         [Tooltip("Enable formation-based combat bonuses")]
//         [SerializeField] private bool _enableFormationBonuses = true;
//         
//         [TitleGroup("Debug")]
//         [Tooltip("Enable debug visualization")]
//         [SerializeField] private bool _debugVisualization = false;
//         
//         [Tooltip("Enable debug logging")]
//         [SerializeField] private bool _debugLogging = false;
//         
//         // Internal references
//         private SquadFactory _squadFactory;
//         private float _nextUpdateTime = 0f;
//         
//         // Cached data
//         private Dictionary<int, bool> _squadEngagementState = new Dictionary<int, bool>();
//         
//         private void Start()
//         {
//             // Get references
//             InitializeReferences();
//             
//             Debug.Log("CombatSystemController: Initialized and ready");
//         }
//         
//         /// <summary>
//         /// Initialize required references
//         /// </summary>
//         private void InitializeReferences()
//         {
//             // Find EnhancedUnitFactory if not assigned
//             if (_unitFactory == null)
//             {
//                 _unitFactory = FindObjectOfType<UnitFactory>();
//                 if (_unitFactory == null)
//                 {
//                     Debug.LogError("CombatSystemController: EnhancedUnitFactory not found!");
//                 }
//             }
//             
//             // Get singleton reference
//             _squadFactory = SquadFactory.Instance;
//             if (_squadFactory == null)
//             {
//                 Debug.LogError("CombatSystemController: EnhancedSquadFactory singleton not found!");
//             }
//         }
//         
//         private void Update()
//         {
//             // Throttle updates for performance
//             if (Time.time < _nextUpdateTime)
//                 return;
//                 
//             _nextUpdateTime = Time.time + _updateInterval;
//             
//             // Update combat state
//             UpdateCombatState();
//         }
//         
//         /// <summary>
//         /// Update the combat state of all squads
//         /// </summary>
//         private void UpdateCombatState()
//         {
//             if (_squadFactory == null)
//                 return;
//                 
//             // Get all squad models
//             List<SquadModel> allSquads = _squadFactory.GetAllSquads();
//             if (allSquads.Count == 0)
//                 return;
//                 
//             // Sort squads into player and enemy
//             List<SquadModel> playerSquads = new List<SquadModel>();
//             List<SquadModel> enemySquads = new List<SquadModel>();
//             
//             foreach (var squad in allSquads)
//             {
//                 if (squad.Data != null && squad.Data.Faction == "Enemy")
//                 {
//                     enemySquads.Add(squad);
//                 }
//                 else
//                 {
//                     playerSquads.Add(squad);
//                 }
//             }
//             
//             // Check for combat engagement between player and enemy squads
//             if (_autoEngageEnemies)
//             {
//                 CheckForAutoEngagement(playerSquads, enemySquads);
//             }
//             
//             // Apply formation bonuses if enabled
//             if (_enableFormationBonuses)
//             {
//                 ApplyFormationBonuses(allSquads);
//             }
//             
//             // Debug visualization
//             if (_debugVisualization)
//             {
//                 VisualizeSquadStates(allSquads);
//             }
//         }
//         
//         /// <summary>
//         /// Check if any squads should automatically engage enemies
//         /// </summary>
//         private void CheckForAutoEngagement(List<SquadModel> playerSquads, List<SquadModel> enemySquads)
//         {
//             // Simple implementation: Check distance between squads
//             foreach (var playerSquad in playerSquads)
//             {
//                 if (playerSquad.IsMoving || playerSquad.IsEngaged)
//                     continue; // Skip squads that are already moving or engaged
//                     
//                 foreach (var enemySquad in enemySquads)
//                 {
//                     float distance = Vector3.Distance(playerSquad.CurrentPosition, enemySquad.CurrentPosition);
//                     
//                     // If within aggro range, engage
//                     if (distance < _maxAggroRange)
//                     {
//                         // Store engagement state
//                         _squadEngagementState[playerSquad.SquadId] = true;
//                         
//                         if (_debugLogging)
//                         {
//                             Debug.Log($"CombatSystemController: Squad {playerSquad.SquadId} auto-engaging enemy squad {enemySquad.SquadId} at distance {distance:F1}");
//                         }
//                         
//                         // In a more complex implementation, we would calculate attack vectors, etc.
//                         break;
//                     }
//                 }
//             }
//         }
//         
//         /// <summary>
//         /// Apply bonuses based on formation types
//         /// </summary>
//         private void ApplyFormationBonuses(List<SquadModel> squads)
//         {
//             foreach (var squad in squads)
//             {
//                 // Skip squads that are moving
//                 if (squad.IsMoving)
//                     continue;
//                     
//                 switch (squad.CurrentFormation)
//                 {
//                     case FormationType.Phalanx:
//                         // In a real implementation, we would apply combat bonuses to the units
//                         if (_debugLogging)
//                         {
//                             Debug.Log($"CombatSystemController: Squad {squad.SquadId} in Phalanx formation - Defensive bonus active");
//                         }
//                         break;
//                         
//                     case FormationType.Testudo:
//                         // Testudo provides strong defense
//                         if (_debugLogging)
//                         {
//                             Debug.Log($"CombatSystemController: Squad {squad.SquadId} in Testudo formation - Strong defensive bonus active");
//                         }
//                         break;
//                         
//                     case FormationType.Circle:
//                         // Circle provides all-around defense
//                         if (_debugLogging)
//                         {
//                             Debug.Log($"CombatSystemController: Squad {squad.SquadId} in Circle formation - 360° defensive bonus active");
//                         }
//                         break;
//                         
//                     case FormationType.Line:
//                         // Line provides offensive bonus
//                         if (_debugLogging)
//                         {
//                             Debug.Log($"CombatSystemController: Squad {squad.SquadId} in Line formation - Offensive bonus active");
//                         }
//                         break;
//                 }
//             }
//         }
//         
//         /// <summary>
//         /// Debug visualization for squad states
//         /// </summary>
//         private void VisualizeSquadStates(List<SquadModel> squads)
//         {
//             foreach (var squad in squads)
//             {
//                 // Visualize squad position and state
//                 Color debugColor = Color.white;
//                 
//                 if (squad.IsEngaged)
//                 {
//                     debugColor = Color.red; // Combat
//                 }
//                 else if (squad.IsMoving)
//                 {
//                     debugColor = Color.yellow; // Moving
//                 }
//                 else if (squad.IsReforming)
//                 {
//                     debugColor = Color.cyan; // Reforming
//                 }
//                 else if (squad.Data != null && squad.Data.Faction == "Enemy")
//                 {
//                     debugColor = new Color(1f, 0.5f, 0.5f); // Enemy
//                 }
//                 else
//                 {
//                     debugColor = new Color(0.5f, 1f, 0.5f); // Friendly
//                 }
//                 
//                 // Draw a sphere at squad position
//                 Debug.DrawLine(squad.CurrentPosition, squad.CurrentPosition + Vector3.up * 2f, debugColor);
//                 
//                 // Draw a line in the direction the squad is facing
//                 Vector3 forward = squad.CurrentRotation * Vector3.forward;
//                 Debug.DrawLine(squad.CurrentPosition, squad.CurrentPosition + forward * 2f, debugColor);
//             }
//         }
//         
//         #region Public Interface for TileSquadController
//         
//         /// <summary>
//         /// Move a squad to a position
//         /// </summary>
//         /// <param name="squadId">ID of the squad to move</param>
//         /// <param name="position">Target position</param>
//         /// <returns>True if the move command was accepted</returns>
//         public bool MoveSquad(int squadId, Vector3 position)
//         {
//             if (_squadFactory == null)
//                 return false;
//                 
//             SquadModel squad = _squadFactory.GetSquad(squadId);
//             if (squad == null)
//                 return false;
//                 
//             // Set the target position on the squad model
//             squad.SetTargetPosition(position);
//             
//             // Clear any engagement state
//             _squadEngagementState.Remove(squadId);
//             
//             if (_debugLogging)
//             {
//                 Debug.Log($"CombatSystemController: Moving squad {squadId} to position {position}");
//             }
//             
//             return true;
//         }
//         
//         /// <summary>
//         /// Change a squad's formation
//         /// </summary>
//         /// <param name="squadId">ID of the squad</param>
//         /// <param name="formation">New formation type</param>
//         /// <returns>True if the formation change was accepted</returns>
//         public bool SetSquadFormation(int squadId, FormationType formation)
//         {
//             if (_squadFactory == null)
//                 return false;
//                 
//             SquadModel squad = _squadFactory.GetSquad(squadId);
//             if (squad == null)
//                 return false;
//                 
//             // Set the formation on the squad model
//             squad.SetFormation(formation);
//             
//             if (_debugLogging)
//             {
//                 Debug.Log($"CombatSystemController: Changed squad {squadId} formation to {formation}");
//             }
//             
//             return true;
//         }
//         
//         /// <summary>
//         /// Order a squad to attack a target squad
//         /// </summary>
//         /// <param name="attackerSquadId">ID of the attacking squad</param>
//         /// <param name="targetSquadId">ID of the target squad</param>
//         /// <returns>True if the attack order was accepted</returns>
//         public bool OrderSquadAttack(int attackerSquadId, int targetSquadId)
//         {
//             if (_squadFactory == null)
//                 return false;
//                 
//             SquadModel attackerSquad = _squadFactory.GetSquad(attackerSquadId);
//             SquadModel targetSquad = _squadFactory.GetSquad(targetSquadId);
//             
//             if (attackerSquad == null || targetSquad == null)
//                 return false;
//                 
//             // In a full implementation, we would calculate attack vectors and set up the engagement
//             // For now, we'll just move the attacker towards the target
//             
//             // Record that this squad is engaging
//             _squadEngagementState[attackerSquadId] = true;
//             
//             // Move attacker to target's position (with a small offset)
//             Vector3 attackPosition = targetSquad.CurrentPosition - (targetSquad.CurrentPosition - attackerSquad.CurrentPosition).normalized * 2f;
//             attackerSquad.SetTargetPosition(attackPosition);
//             
//             // Set appropriate formation based on unit types
//             if (HasUnitType(attackerSquad, UnitType.Pike))
//             {
//                 attackerSquad.SetFormation(FormationType.Phalanx);
//             }
//             else if (HasUnitType(attackerSquad, UnitType.Archer))
//             {
//                 attackerSquad.SetFormation(FormationType.Line);
//             }
//             else
//             {
//                 attackerSquad.SetFormation(FormationType.Line);
//             }
//             
//             if (_debugLogging)
//             {
//                 Debug.Log($"CombatSystemController: Squad {attackerSquadId} ordered to attack squad {targetSquadId}");
//             }
//             
//             return true;
//         }
//         
//         /// <summary>
//         /// Check if a squad contains a specific unit type
//         /// </summary>
//         private bool HasUnitType(SquadModel squad, UnitType unitType)
//         {
//             return squad.HasUnitType(unitType);
//         }
//         
//         /// <summary>
//         /// Get the health percentage of a squad
//         /// </summary>
//         /// <param name="squadId">ID of the squad</param>
//         /// <returns>Health percentage (0-1) or -1 if squad not found</returns>
//         public float GetSquadHealth(int squadId)
//         {
//             if (_squadFactory == null)
//                 return -1f;
//                 
//             SquadModel squad = _squadFactory.GetSquad(squadId);
//             if (squad == null)
//                 return -1f;
//                 
//             return squad.GetAverageHealthPercentage();
//         }
//         
//         /// <summary>
//         /// Heal all units in a squad
//         /// </summary>
//         /// <param name="squadId">ID of the squad</param>
//         /// <param name="amount">Amount to heal each unit</param>
//         /// <returns>True if the heal command was executed</returns>
//         public bool HealSquad(int squadId, float amount)
//         {
//             if (_squadFactory == null)
//                 return false;
//                 
//             SquadModel squad = _squadFactory.GetSquad(squadId);
//             if (squad == null)
//                 return false;
//                 
//             foreach (var unit in squad.Units)
//             {
//                 // var healthComponent = unit.GetComponent<HealthComponent>();
//                 // if (healthComponent != null)
//                 // {
//                 //     healthComponent.Heal(amount);
//                 // }
//             }
//             
//             if (_debugLogging)
//             {
//                 Debug.Log($"CombatSystemController: Healed squad {squadId} for {amount} points");
//             }
//             
//             return true;
//         }
//         
//         /// <summary>
//         /// Damage all units in a squad
//         /// </summary>
//         /// <param name="squadId">ID of the squad</param>
//         /// <param name="amount">Amount to damage each unit</param>
//         /// <returns>True if the damage command was executed</returns>
//         public bool DamageSquad(int squadId, float amount)
//         {
//             if (_squadFactory == null)
//                 return false;
//                 
//             SquadModel squad = _squadFactory.GetSquad(squadId);
//             if (squad == null)
//                 return false;
//                 
//             foreach (var unit in squad.Units)
//             {
//                 // var healthComponent = unit.GetComponent<HealthComponent>();
//                 // if (healthComponent != null)
//                 // {
//                 //     healthComponent.TakeDamage(amount, null);
//                 // }
//             }
//             
//             if (_debugLogging)
//             {
//                 Debug.Log($"CombatSystemController: Damaged squad {squadId} for {amount} points");
//             }
//             
//             return true;
//         }
//         
//         #endregion
//         
//         #region Editor Debug Methods
//         
//         #if UNITY_EDITOR
//         [Button("Check Factory References")]
//         private void CheckFactoryReferences()
//         {
//             InitializeReferences();
//             
//             if (_unitFactory != null && _squadFactory != null)
//             {
//                 Debug.Log("CombatSystemController: All factory references are valid");
//             }
//         }
//         
//         [Button("Log Squad Stats")]
//         private void LogSquadStats()
//         {
//             if (_squadFactory == null)
//                 return;
//                 
//             List<SquadModel> allSquads = _squadFactory.GetAllSquads();
//             
//             Debug.Log($"--- Combat System Status ---");
//             Debug.Log($"Total Squads: {allSquads.Count}");
//             
//             foreach (var squad in allSquads)
//             {
//                 string faction = squad.Data != null ? squad.Data.Faction : "Unknown";
//                 string state = squad.IsEngaged ? "Engaged" : (squad.IsMoving ? "Moving" : "Idle");
//                 
//                 Debug.Log($"Squad {squad.SquadId} ({faction}): {squad.UnitCount} units, {state}, " +
//                           $"Health: {squad.GetAverageHealthPercentage() * 100:F0}%, " +
//                           $"Formation: {squad.CurrentFormation}");
//             }
//         }
//         #endif
//         #endregion
//     }
// }