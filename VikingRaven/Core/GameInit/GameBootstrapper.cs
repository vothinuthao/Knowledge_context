// GameBootstrapper.cs - cập nhật để sử dụng Singleton thay vì DI
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Game;
using VikingRaven.Core.Factory;

namespace VikingRaven.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private bool _autoStartLevel = true;
        
        // References to singletons
        private SystemRegistry SystemRegistry => SystemRegistry.Instance;
        private EntityRegistry EntityRegistry => EntityRegistry.Instance;
        private UnitFactory UnitFactory => UnitFactory.Instance;
        private SquadFactory SquadFactory => SquadFactory.Instance;
        private GameManager GameManager => GameManager.Instance;
        private LevelManager LevelManager => LevelManager.Instance;
        
        private void Awake()
        {
            Debug.Log("GameBootstrapper.Awake() - Starting game initialization");
            Debug.Log($"EntityRegistry initialized: {EntityRegistry.Instance != null}");
            Debug.Log($"SystemRegistry initialized: {SystemRegistry.Instance != null}");
            Debug.Log($"UnitFactory initialized: {UnitFactory.Instance != null}");
            Debug.Log($"SquadFactory initialized: {SquadFactory.Instance != null}");
            Debug.Log($"GameManager initialized: {GameManager.Instance != null}");
            Debug.Log($"LevelManager initialized: {LevelManager.Instance != null}");
        }
        
        private void Start()
        {
            Debug.Log("GameBootstrapper: Initializing game...");
            
            VerifySingletons();
            if (SystemRegistry != null)
            {
                SystemRegistry.InitializeAllSystems();
                Debug.Log("All systems initialized");
            }
            else
            {
                Debug.LogError("GameBootstrapper: SystemRegistry is null, cannot initialize systems");
            }
            
            // Start level if auto-start is enabled
            if (_autoStartLevel && LevelManager != null)
            {
                Debug.Log("Auto-starting level");
                LevelManager.StartLevel();
            }
        }
        
        private void Update()
        {
            // Execute all systems every frame
            SystemRegistry?.ExecuteAllSystems();
        }
        
        private void VerifySingletons()
        {
            bool allValid = true;
            
            if (SystemRegistry == null)
            {
                Debug.LogError("GameBootstrapper: SystemRegistry singleton is not available");
                allValid = false;
            }
            
            if (EntityRegistry == null)
            {
                Debug.LogError("GameBootstrapper: EntityRegistry singleton is not available");
                allValid = false;
            }
            
            if (UnitFactory == null)
            {
                Debug.LogError("GameBootstrapper: UnitFactory singleton is not available");
                allValid = false;
            }
            
            if (SquadFactory == null)
            {
                Debug.LogError("GameBootstrapper: SquadFactory singleton is not available");
                allValid = false;
            }
            
            if (GameManager == null)
            {
                Debug.LogError("GameBootstrapper: GameManager singleton is not available");
                allValid = false;
            }
            
            if (LevelManager == null)
            {
                Debug.LogError("GameBootstrapper: LevelManager singleton is not available");
                allValid = false;
            }
            
            if (allValid)
            {
                Debug.Log("GameBootstrapper: All core singletons are available");
            }
            else
            {
                Debug.LogError("GameBootstrapper: Some core singletons are missing, game may not function correctly");
            }
        }
    }
}