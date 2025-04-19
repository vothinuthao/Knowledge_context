using SteeringBehavior;
using Troop;
using UnityEngine;

public class GameStarter : MonoBehaviour
{
    [Header("Core Managers")]
    [Tooltip("Game Manager")]
    public GameManager gameManager;
    
    [Tooltip("Island Generator")]
    public IslandGenerator islandGenerator;
    
    [Tooltip("Troop Factory")]
    public TroopFactory troopFactory;
    
    [Tooltip("Troop Manager")]
    public TroopManager troopManager;
    
    [Header("Prefabs")]
    [Tooltip("Squad Prefab")]
    public GameObject squadPrefab;
    
    [Header("Sample Config")]
    [Tooltip("Sample Player Troop Config")]
    public TroopConfigSO samplePlayerTroopConfig;
    
    [Tooltip("Sample Enemy Troop Config")]
    public TroopConfigSO sampleEnemyTroopConfig;
    
    private void Awake()
    {
        // Khởi tạo các core managers nếu chưa được gán
        InitializeManagers();
    }
    
    private void Start()
    {
        // Kết nối các thành phần với nhau
        ConnectComponents();
    }
    
    // Khởi tạo các managers
    private void InitializeManagers()
    {
        // Khởi tạo Troop Factory
        if (troopFactory == null)
        {
            troopFactory = FindObjectOfType<TroopFactory>();
            if (troopFactory == null)
            {
                GameObject factoryObj = new GameObject("TroopFactory");
                troopFactory = factoryObj.AddComponent<TroopFactory>();
                Debug.Log("GameStarter: Tạo mới TroopFactory");
            }
        }
        
        // Khởi tạo Troop Manager
        if (troopManager == null)
        {
            troopManager = FindObjectOfType<TroopManager>();
            if (troopManager == null)
            {
                GameObject managerObj = new GameObject("TroopManager");
                troopManager = managerObj.AddComponent<TroopManager>();
                Debug.Log("GameStarter: Tạo mới TroopManager");
            }
        }
        
        // Khởi tạo Island Generator
        if (islandGenerator == null)
        {
            islandGenerator = FindObjectOfType<IslandGenerator>();
            if (islandGenerator == null)
            {
                GameObject islandObj = new GameObject("IslandGenerator");
                islandGenerator = islandObj.AddComponent<IslandGenerator>();
                Debug.Log("GameStarter: Tạo mới IslandGenerator");
            }
        }
        
        // Khởi tạo Game Manager
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                GameObject gameManagerObj = new GameObject("GameManager");
                gameManager = gameManagerObj.AddComponent<GameManager>();
                Debug.Log("GameStarter: Tạo mới GameManager");
            }
        }
    }
    
    // Kết nối các thành phần với nhau
    private void ConnectComponents()
    {
        // Kết nối Game Manager với các thành phần khác
        if (gameManager != null)
        {
            // Kết nối TroopFactory
            gameManager.troopFactory = troopFactory;
            
            // Kết nối TroopManager
            gameManager.troopManager = troopManager;
            
            // Kết nối Squad Prefab
            if (squadPrefab != null)
            {
                gameManager.squadPrefab = squadPrefab;
            }
            
            // Kết nối spawn points từ Island Generator
            if (islandGenerator != null)
            {
                gameManager.squadSpawnPoints = islandGenerator.GetSquadSpawnPoints();
            }
            
            // Kết nối sample troop configs
            if (samplePlayerTroopConfig != null)
            {
                gameManager.playerTroopConfig = samplePlayerTroopConfig;
            }
            
            if (sampleEnemyTroopConfig != null)
            {
                gameManager.enemyTroopConfig = sampleEnemyTroopConfig;
            }
            
            Debug.Log("GameStarter: Đã kết nối GameManager với các thành phần khác");
        }
        
        // Kết nối TroopFactory với danh sách configs
        if (troopFactory != null)
        {
            if (samplePlayerTroopConfig != null && !troopFactory.availableTroopConfigs.Contains(samplePlayerTroopConfig))
            {
                troopFactory.availableTroopConfigs.Add(samplePlayerTroopConfig);
            }
            
            if (sampleEnemyTroopConfig != null && !troopFactory.availableTroopConfigs.Contains(sampleEnemyTroopConfig))
            {
                troopFactory.availableTroopConfigs.Add(sampleEnemyTroopConfig);
            }
            
            Debug.Log("GameStarter: Đã kết nối TroopFactory với configs");
        }
    }
    
    // OnGUI hiển thị hướng dẫn
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 150));
        GUILayout.Label("Wiking Raven Prototype", GUI.skin.box);
        
        GUILayout.Label("Controls:");
        GUILayout.Label("Left Click: Select Squad");
        GUILayout.Label("Right Click: Move Selected Squad");
        
        GUILayout.EndArea();
    }
}