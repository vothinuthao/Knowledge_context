// SquadSpawnWizard.cs
using UnityEngine;
using Core.ECS;
using Core.Grid;
using Managers;
using Systems.Squad;
using Systems.Movement;
using Systems.Steering;

namespace Helper_Tool
{
    /// <summary>
    /// Automatic setup wizard for squad spawning
    /// </summary>
    public class SquadSpawnWizard : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool _autoSetupOnStart = true;
        [SerializeField] private KeyCode _setupKey = KeyCode.F9;
        
        private bool _setupComplete = false;
        private string _setupStatus = "";
        
        private void Start()
        {
            if (_autoSetupOnStart)
            {
                PerformAutoSetup();
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(_setupKey))
            {
                PerformAutoSetup();
            }
        }
        
        private void PerformAutoSetup()
        {
            Debug.Log("=== Starting Squad Spawn Setup Wizard ===");
            
            _setupStatus = "Setting up...";
            
            // Step 1: Ensure GameManager exists
            if (!EnsureGameManager())
            {
                _setupStatus = "ERROR: Failed to setup GameManager";
                return;
            }
            
            // Step 2: Ensure WorldManager exists
            if (!EnsureWorldManager())
            {
                _setupStatus = "ERROR: Failed to setup WorldManager";
                return;
            }
            
            // Step 3: Ensure GridManager exists
            if (!EnsureGridManager())
            {
                _setupStatus = "ERROR: Failed to setup GridManager";
                return;
            }
            
            // Step 4: Register essential systems
            if (!RegisterEssentialSystems())
            {
                _setupStatus = "ERROR: Failed to register systems";
                return;
            }
            
            // Step 5: Create test prefabs if needed
            if (!EnsurePrefabs())
            {
                _setupStatus = "ERROR: Failed to setup prefabs";
                return;
            }
            
            _setupComplete = true;
            _setupStatus = "Setup complete! Ready to spawn squads.";
            Debug.Log("=== Squad Spawn Setup Wizard Complete ===");
        }
        
        private bool EnsureGameManager()
        {
            GameManager manager = GameManager.Instance;
            
            if (manager == null)
            {
                Debug.LogWarning("GameManager not found, creating new instance...");
                GameObject go = new GameObject("GameManager");
                manager = go.AddComponent<GameManager>();
                
                // Wait for initialization
                System.DateTime timeout = System.DateTime.Now.AddSeconds(2);
                while (GameManager.Instance == null && System.DateTime.Now < timeout)
                {
                    System.Threading.Thread.Sleep(100);
                }
                
                if (GameManager.Instance == null)
                {
                    Debug.LogError("Failed to initialize GameManager");
                    return false;
                }
            }
            
            Debug.Log("GameManager ready");
            return true;
        }
        
        private bool EnsureWorldManager()
        {
            WorldManager manager = FindObjectOfType<WorldManager>();
            
            if (manager == null)
            {
                Debug.LogWarning("WorldManager not found, creating new instance...");
                GameObject go = new GameObject("WorldManager");
                manager = go.AddComponent<WorldManager>();
            }
            
            // Initialize world if needed
            if (manager.World == null)
            {
                Debug.Log("Initializing World...");
                manager.InitializeWorld();
                
                if (manager.World == null)
                {
                    Debug.LogError("Failed to initialize World");
                    return false;
                }
            }
            
            Debug.Log("WorldManager and World ready");
            return true;
        }
        
        private bool EnsureGridManager()
        {
            GridManager manager = GridManager.Instance;
            
            if (manager == null)
            {
                Debug.LogWarning("GridManager not found, creating new instance...");
                GameObject go = new GameObject("GridManager");
                manager = go.AddComponent<GridManager>();
                
                // Initialize grid
                manager.InitializeGrid();
            }
            
            Debug.Log("GridManager ready");
            return true;
        }
        
        private bool RegisterEssentialSystems()
        {
            WorldManager worldManager = FindObjectOfType<WorldManager>();
            if (worldManager == null || worldManager.World == null)
            {
                Debug.LogError("World not available for system registration");
                return false;
            }
            
            World world = worldManager.World;
            
            // Register systems in correct order
            RegisterSystem<SquadCommandSystem>(world);
            RegisterSystem<EntityDetectionSystem>(world);
            RegisterSystem<SquadFormationSystem>(world);
            
            RegisterSystem<SteeringSystem>(world);
            RegisterSystem<SeekSystem>(world);
            RegisterSystem<SeparationSystem>(world);
            RegisterSystem<ArrivalSystem>(world);
            
            RegisterSystem<MovementSystem>(world);
            RegisterSystem<RotationSystem>(world);
            
            Debug.Log($"Registered {world.GetRegisteredSystemCount()} systems");
            return true;
        }
        
        private void RegisterSystem<T>(World world) where T : ISystem, new()
        {
            if (world.GetSystem<T>() == null)
            {
                world.RegisterSystem(new T());
                Debug.Log($"Registered {typeof(T).Name}");
            }
        }
        
        private bool EnsurePrefabs()
        {
            // Create basic troop prefab if none exists
            GameObject troopPrefab = Resources.Load<GameObject>("Prefabs/TroopPrefab");
            
            if (troopPrefab == null)
            {
                Debug.LogWarning("Troop prefab not found, creating basic prefab...");
                
                troopPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                troopPrefab.name = "TroopPrefab";
                troopPrefab.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
                
                // Add EntityBehaviour
                troopPrefab.AddComponent<EntityBehaviour>();
                
                // Try to save as prefab (Editor only)
                #if UNITY_EDITOR
                string path = "Assets/Resources/Prefabs/TroopPrefab.prefab";
                if (!System.IO.Directory.Exists("Assets/Resources/Prefabs"))
                {
                    System.IO.Directory.CreateDirectory("Assets/Resources/Prefabs");
                }
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(troopPrefab, path);
                #endif
                
                DestroyImmediate(troopPrefab);
            }
            
            Debug.Log("Prefabs ready");
            return true;
        }
        
        private void OnGUI()
        {
            // Status panel
            float width = 300f;
            float height = 100f;
            float x = 10f;
            float y = Screen.height - height - 10f;
            
            GUI.Box(new Rect(x, y, width, height), "Squad Spawn Wizard");
            
            GUIStyle statusStyle = _setupComplete ? GUI.skin.label : GUI.skin.textField;
            if (_setupComplete)
            {
                statusStyle.normal.textColor = Color.green;
            }
            else if (_setupStatus.Contains("ERROR"))
            {
                statusStyle.normal.textColor = Color.red;
            }
            
            GUI.Label(new Rect(x + 10, y + 30, width - 20, 40), _setupStatus, statusStyle);
            
            if (GUI.Button(new Rect(x + 10, y + 70, width - 20, 25), "Run Setup"))
            {
                PerformAutoSetup();
            }
        }
    }
}