using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VikingRaven.Combat.Components;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Events;
using VikingRaven.Core.Factory;
using VikingRaven.Units.Components;
using VikingRaven.Units.Systems;

namespace VikingRaven.SystemDebugger_Tool
{
    public class DebugConsole : MonoBehaviour
    {
        [SerializeField] private GameObject _consolePanel;
        [SerializeField] private TextMeshProUGUI _consoleText;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private TMP_InputField _commandInput;
        [SerializeField] private int _maxMessages = 100;
        
        private List<string> _messages = new List<string>();
        private Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>();
        
        // References to managers and registries
        [SerializeField] private EntityRegistry _entityRegistry;
        
        public static DebugConsole Instance { get; private set; }
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // Initialize console
            if (_consolePanel != null)
            {
                _consolePanel.SetActive(false);
            }
            
            // Register commands
            RegisterCommands();
            
            // Subscribe to events
            SubscribeToEvents();
            
            // Initial log
            Log("Debug Console initialized. Press ~ to toggle console.");
        }
        
        private void RegisterCommands()
        {
            // Help command
            _commands["help"] = (args) => {
                Log("Available Commands:");
                Log("  help - Show this help message");
                Log("  list_entities - List all entities");
                Log("  list_squad [squadId] - List entities in a squad");
                Log("  inspect_entity [entityId] - Show entity details");
                Log("  set_behavior [entityId] [behaviorName] [weight] - Set behavior weight");
                Log("  set_formation [squadId] [formationType] - Set squad formation");
                Log("  spawn_squad [type] [count] [x] [z] - Spawn a new squad");
                Log("  clear - Clear console");
            };
            
            // List entities command
            _commands["list_entities"] = (args) => {
                if (_entityRegistry == null)
                {
                    Log("ERROR: EntityRegistry not found.");
                    return;
                }
                
                var entities = _entityRegistry.GetAllEntities();
                Log($"Found {entities.Count} entities:");
                
                foreach (var entity in entities)
                {
                    string entityInfo = $"Entity {entity.Id}";
                    
                    // Get unit type if available
                    var unitTypeComponent = entity.GetComponent<UnitTypeComponent>();
                    if (unitTypeComponent != null)
                    {
                        entityInfo += $" - {unitTypeComponent.UnitType}";
                    }
                    
                    // Get squad info if available
                    var formationComponent = entity.GetComponent<FormationComponent>();
                    if (formationComponent != null)
                    {
                        entityInfo += $" - Squad {formationComponent.SquadId}";
                    }
                    
                    Log(entityInfo);
                }
            };
            
            // List squad entities command
            _commands["list_squad"] = (args) => {
                if (args.Length < 1)
                {
                    Log("ERROR: Missing squadId parameter.");
                    return;
                }
                
                if (_entityRegistry == null)
                {
                    Log("ERROR: EntityRegistry not found.");
                    return;
                }
                
                if (!int.TryParse(args[0], out int squadId))
                {
                    Log("ERROR: Invalid squadId.");
                    return;
                }
                
                var entities = _entityRegistry.GetEntitiesWithComponent<FormationComponent>();
                List<IEntity> squadEntities = new List<IEntity>();
                
                foreach (var entity in entities)
                {
                    var formationComponent = entity.GetComponent<FormationComponent>();
                    if (formationComponent != null && formationComponent.SquadId == squadId)
                    {
                        squadEntities.Add(entity);
                    }
                }
                
                Log($"Found {squadEntities.Count} entities in Squad {squadId}:");
                
                foreach (var entity in squadEntities)
                {
                    string entityInfo = $"Entity {entity.Id}";
                    
                    // Get unit type if available
                    var unitTypeComponent = entity.GetComponent<UnitTypeComponent>();
                    if (unitTypeComponent != null)
                    {
                        entityInfo += $" - {unitTypeComponent.UnitType}";
                    }
                    
                    // Get health info if available
                    var healthComponent = entity.GetComponent<HealthComponent>();
                    if (healthComponent != null)
                    {
                        entityInfo += $" - HP: {healthComponent.CurrentHealth}/{healthComponent.MaxHealth}";
                    }
                    
                    Log(entityInfo);
                }
            };
            
            // Inspect entity command
            _commands["inspect_entity"] = (args) => {
                if (args.Length < 1)
                {
                    Log("ERROR: Missing entityId parameter.");
                    return;
                }
                
                if (_entityRegistry == null)
                {
                    Log("ERROR: EntityRegistry not found.");
                    return;
                }
                
                if (!int.TryParse(args[0], out int entityId))
                {
                    Log("ERROR: Invalid entityId.");
                    return;
                }
                
                var entity = _entityRegistry.GetEntity(entityId);
                
                if (entity == null)
                {
                    Log($"ERROR: Entity {entityId} not found.");
                    return;
                }
                
                Log($"Entity {entityId} details:");
                
                // Basic info
                Log($"  IsActive: {entity.IsActive}");
                
                // Transform component
                var transformComponent = entity.GetComponent<TransformComponent>();
                if (transformComponent != null)
                {
                    Log($"  Position: {transformComponent.Position}");
                    Log($"  Rotation: {transformComponent.Rotation.eulerAngles}");
                }
                
                // Unit type component
                var unitTypeComponent = entity.GetComponent<UnitTypeComponent>();
                if (unitTypeComponent != null)
                {
                    Log($"  UnitType: {unitTypeComponent.UnitType}");
                }
                
                // Formation component
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent != null)
                {
                    Log($"  SquadId: {formationComponent.SquadId}");
                    Log($"  FormationType: {formationComponent.CurrentFormationType}");
                    Log($"  FormationSlotIndex: {formationComponent.FormationSlotIndex}");
                    Log($"  FormationOffset: {formationComponent.FormationOffset}");
                }
                
                // Health component
                var healthComponent = entity.GetComponent<HealthComponent>();
                if (healthComponent != null)
                {
                    Log($"  Health: {healthComponent.CurrentHealth}/{healthComponent.MaxHealth} ({healthComponent.HealthPercentage * 100}%)");
                    Log($"  IsDead: {healthComponent.IsDead}");
                }
                
                // Combat component
                var combatComponent = entity.GetComponent<CombatComponent>();
                if (combatComponent != null)
                {
                    Log($"  AttackDamage: {combatComponent.AttackDamage}");
                    Log($"  AttackRange: {combatComponent.AttackRange}");
                    Log($"  CanAttack: {combatComponent.CanAttack()}");
                }
                
                // State component
                var stateComponent = entity.GetComponent<StateComponent>();
                if (stateComponent != null && stateComponent.CurrentState != null)
                {
                    Log($"  CurrentState: {stateComponent.CurrentState.GetType().Name}");
                }
                
                // Tactical component
                var tacticalComponent = entity.GetComponent<TacticalComponent>();
                if (tacticalComponent != null)
                {
                    Log($"  TacticalRole: {tacticalComponent.AssignedRole}");
                    Log($"  CurrentObjective: {tacticalComponent.CurrentObjective}");
                    Log($"  ObjectivePosition: {tacticalComponent.ObjectivePosition}");
                }
            };
            
            // Set behavior weight command
            _commands["set_behavior"] = (args) => {
                if (args.Length < 3)
                {
                    Log("ERROR: Missing parameters. Format: set_behavior [entityId] [behaviorName] [weight]");
                    return;
                }
                
                if (_entityRegistry == null)
                {
                    Log("ERROR: EntityRegistry not found.");
                    return;
                }
                
                if (!int.TryParse(args[0], out int entityId))
                {
                    Log("ERROR: Invalid entityId.");
                    return;
                }
                
                string behaviorName = args[1];
                
                if (!float.TryParse(args[2], out float weight))
                {
                    Log("ERROR: Invalid weight.");
                    return;
                }
                
                var entity = _entityRegistry.GetEntity(entityId);
                
                if (entity == null)
                {
                    Log($"ERROR: Entity {entityId} not found.");
                    return;
                }
                
                var behaviorComponent = entity.GetComponent<WeightedBehaviorComponent>();
                if (behaviorComponent == null || behaviorComponent.BehaviorManager == null)
                {
                    Log("ERROR: Entity doesn't have a WeightedBehaviorComponent.");
                    return;
                }
                
                // Find behavior by name using reflection
                var behaviorManager = behaviorComponent.BehaviorManager;
                var behaviorsField = behaviorManager.GetType().GetField("_behaviors", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (behaviorsField != null)
                {
                    var behaviors = behaviorsField.GetValue(behaviorManager) as List<Core.Behavior.IBehavior>;
                    
                    if (behaviors != null)
                    {
                        bool found = false;
                        
                        foreach (var behavior in behaviors)
                        {
                            if (behavior.Name.Equals(behaviorName, StringComparison.OrdinalIgnoreCase))
                            {
                                behavior.Weight = weight;
                                found = true;
                                Log($"Set {behaviorName} weight to {weight} for Entity {entityId}");
                                break;
                            }
                        }
                        
                        if (!found)
                        {
                            Log($"ERROR: Behavior '{behaviorName}' not found.");
                        }
                    }
                }
            };
            
            // Set formation command
            _commands["set_formation"] = (args) => {
                if (args.Length < 2)
                {
                    Log("ERROR: Missing parameters. Format: set_formation [squadId] [formationType]");
                    return;
                }
                
                if (!int.TryParse(args[0], out int squadId))
                {
                    Log("ERROR: Invalid squadId.");
                    return;
                }
                
                if (!Enum.TryParse<FormationType>(args[1], true, out FormationType formationType))
                {
                    Log("ERROR: Invalid formationType. Valid values: None, Line, Column, Phalanx, Testudo, Circle");
                    return;
                }
                
                // Find SquadCoordinationSystem
                var squadCoordinationSystem = FindObjectOfType<SquadCoordinationSystem>();
                
                if (squadCoordinationSystem == null)
                {
                    Log("ERROR: SquadCoordinationSystem not found.");
                    return;
                }
                
                // Set formation
                squadCoordinationSystem.SetSquadFormation(squadId, formationType);
                
                Log($"Set Squad {squadId} formation to {formationType}");
            };
            
            // Spawn squad command
            _commands["spawn_squad"] = (args) => {
                if (args.Length < 4)
                {
                    Log("ERROR: Missing parameters. Format: spawn_squad [type] [count] [x] [z]");
                    return;
                }
                
                if (!Enum.TryParse<UnitType>(args[0], true, out UnitType unitType))
                {
                    Log("ERROR: Invalid unitType. Valid values: Infantry, Archer, Pike");
                    return;
                }
                
                if (!int.TryParse(args[1], out int count) || count <= 0)
                {
                    Log("ERROR: Invalid count.");
                    return;
                }
                
                if (!float.TryParse(args[2], out float x))
                {
                    Log("ERROR: Invalid x coordinate.");
                    return;
                }
                
                if (!float.TryParse(args[3], out float z))
                {
                    Log("ERROR: Invalid z coordinate.");
                    return;
                }
                
                // Find SquadFactory
                var squadFactory = FindObjectOfType<SquadFactory>();
                
                if (squadFactory == null)
                {
                    Log("ERROR: SquadFactory not found.");
                    return;
                }
                
                // Spawn squad
                Vector3 position = new Vector3(x, 0f, z);
                var entities = squadFactory.CreateSquad(unitType, count, position, Quaternion.identity);
                
                // Get squad ID from first entity
                int newSquadId = -1;
                
                if (entities.Count > 0)
                {
                    var formationComponent = entities[0].GetComponent<FormationComponent>();
                    if (formationComponent != null)
                    {
                        newSquadId = formationComponent.SquadId;
                    }
                }
                
                Log($"Spawned Squad {newSquadId} with {count} {unitType} units at ({x}, {z})");
            };
            
            // Clear console command
            _commands["clear"] = (args) => {
                _messages.Clear();
                UpdateConsoleText();
            };
        }
        
        private void SubscribeToEvents()
        {
            // Subscribe to important events for debugging
            EventManager.Instance.RegisterListener<SquadCreatedEvent>(OnSquadCreated);
            EventManager.Instance.RegisterListener<DeathEvent>(OnUnitDeath);
            EventManager.Instance.RegisterListener<UnitStateChangedEvent>(OnUnitStateChanged);
            EventManager.Instance.RegisterListener<FormationChangedEvent>(OnFormationChanged);
        }
        
        private void OnSquadCreated(SquadCreatedEvent squadEvent)
        {
            Log($"[EVENT] Squad {squadEvent.SquadId} created with {squadEvent.Units.Count} units");
        }
        
        private void OnUnitDeath(DeathEvent deathEvent)
        {
            string unitId = deathEvent.Unit?.Id.ToString() ?? "Unknown";
            string killerId = deathEvent.Killer?.Id.ToString() ?? "Unknown";
            
            Log($"[EVENT] Unit {unitId} killed by {killerId}");
        }
        
        private void OnUnitStateChanged(UnitStateChangedEvent stateEvent)
        {
            string unitId = stateEvent.Unit?.Id.ToString() ?? "Unknown";
            string oldState = stateEvent.OldState?.Name ?? "Unknown";
            string newState = stateEvent.NewState?.Name ?? "Unknown";
            
            Log($"[EVENT] Unit {unitId} state changed from {oldState} to {newState}");
        }
        
        private void OnFormationChanged(FormationChangedEvent formationEvent)
        {
            Log($"[EVENT] Squad {formationEvent.SquadId} formation changed from {formationEvent.OldFormation} to {formationEvent.NewFormation}");
        }
        
        private void Update()
        {
            // Toggle console with tilde key
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                ToggleConsole();
            }
            
            // Submit command with Enter key when input field is focused
            if (_consolePanel.activeSelf && Input.GetKeyDown(KeyCode.Return) && _commandInput.isFocused)
            {
                ExecuteCommand(_commandInput.text);
                _commandInput.text = "";
            }
        }
        
        private void ToggleConsole()
        {
            _consolePanel.SetActive(!_consolePanel.activeSelf);
            
            if (_consolePanel.activeSelf)
            {
                _commandInput.Select();
                _commandInput.ActivateInputField();
            }
        }
        
        public void Log(string message)
        {
            // Add timestamp
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{timestamp}] {message}";
            
            // Add message to the list
            _messages.Add(formattedMessage);
            
            // Trim list if it gets too long
            while (_messages.Count > _maxMessages)
            {
                _messages.RemoveAt(0);
            }
            
            // Update console text
            UpdateConsoleText();
            
            // Print to Unity console for redundancy
            UnityEngine.Debug.Log($"[DebugConsole] {message}");
        }
        
        private void UpdateConsoleText()
        {
            if (_consoleText != null)
            {
                _consoleText.text = string.Join("\n", _messages);
                
                // Scroll to bottom after frame update
                Canvas.ForceUpdateCanvases();
                if (_scrollRect != null)
                {
                    _scrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }
        
        public void ExecuteCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;
                
            Log($"> {input}");
            
            // Parse command and arguments
            string[] parts = input.Split(' ');
            string command = parts[0].ToLower();
            string[] args = new string[parts.Length - 1];
            
            for (int i = 1; i < parts.Length; i++)
            {
                args[i - 1] = parts[i];
            }
            
            // Execute command if it exists
            if (_commands.TryGetValue(command, out Action<string[]> action))
            {
                try
                {
                    action(args);
                }
                catch (Exception ex)
                {
                    Log($"ERROR: {ex.Message}");
                }
            }
            else
            {
                Log($"ERROR: Unknown command '{command}'. Type 'help' for a list of commands.");
            }
        }
    }
}