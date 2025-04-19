using System;
using System.Collections.Generic;
using UnityEngine;
using Troop;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;

public class ManualSquadCreator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab của squad")]
    public GameObject squadPrefab;
    
    [Tooltip("TroopFactory để tạo troop")]
    public TroopFactory troopFactory;
    
    [Tooltip("GameManager để kết nối squad")]
    public GameManager gameManager;
    
    [Tooltip("IslandGenerator để tìm vị trí valid")]
    public IslandGenerator islandGenerator;
    
    [Header("Troop Configs")]
    [Tooltip("Config của troop cho player")]
    public TroopConfigSO playerTroopConfig;
    
    [Tooltip("Config của troop cho enemy")]
    public TroopConfigSO enemyTroopConfig;
    
    [Header("UI Elements (Optional)")]
    [Tooltip("Button để tạo player squad")]
    public Button createPlayerSquadButton;
    
    [Tooltip("Button để tạo enemy squad")]
    public Button createEnemySquadButton;
    
    [Tooltip("Dropdown để chọn số lượng troop")]
    public TMP_Dropdown troopCountDropdown;
    
    [Header("Debug Settings")]
    [Tooltip("Enable debug logs")]
    public bool enableDebug = true;
    
    [Header("Keyboard Controls")]
    [Tooltip("Phím để spawn player squad")]
    public KeyCode spawnPlayerSquadKey = KeyCode.Alpha1;

    [Tooltip("Phím để spawn enemy squad")]
    public KeyCode spawnEnemySquadKey = KeyCode.Alpha2;

    [Tooltip("Bật/tắt điều khiển bàn phím")]
    public bool enableKeyboardControls = true;
    
    private void Start()
    {
        // Tìm references nếu chưa được gán
        FindReferences();
        
        // Setup UI nếu có
        SetupUI();
    }

    private void Update()
    {
        if (enableKeyboardControls)
        {
            // Nhấn phím 1 để spawn player squad
            if (Input.GetKeyDown(spawnPlayerSquadKey))
            {
                if (enableDebug)
                    Debug.Log("ManualSquadCreator: Phím " + spawnPlayerSquadKey + " được nhấn, spawn player squad");
                CreatePlayerSquad();
            }
    
            // Nhấn phím 2 để spawn enemy squad
            if (Input.GetKeyDown(spawnEnemySquadKey))
            {
                if (enableDebug)
                    Debug.Log("ManualSquadCreator: Phím " + spawnEnemySquadKey + " được nhấn, spawn enemy squad");
                CreateEnemySquad();
            }
        }
    }

    private void FindReferences()
    {
        if (troopFactory == null)
            troopFactory = FindObjectOfType<TroopFactory>();
            
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
            
        if (islandGenerator == null)
            islandGenerator = FindObjectOfType<IslandGenerator>();
    }
    
    private void SetupUI()
    {
        if (createPlayerSquadButton != null)
            createPlayerSquadButton.onClick.AddListener(CreatePlayerSquad);
            
        if (createEnemySquadButton != null)
            createEnemySquadButton.onClick.AddListener(CreateEnemySquad);
            
        if (troopCountDropdown != null && troopCountDropdown.options.Count == 0)
        {
            troopCountDropdown.ClearOptions();
            
            List<string> options = new List<string>();
            for (int i = 1; i <= 9; i++)
                options.Add(i.ToString());
                
            troopCountDropdown.AddOptions(options);
            troopCountDropdown.value = 4; // Default 5 troops
        }
    }
    
    // Tạo squad cho player bằng thủ công
    public void CreatePlayerSquad()
    {
        CreateSquad(true);
    }
    
    // Tạo squad cho enemy bằng thủ công
    public void CreateEnemySquad()
    {
        CreateSquad(false);
    }
    
    // Hàm tạo squad chung
    public void CreateSquad(bool isPlayer)
    {
        if (squadPrefab == null)
        {
            Debug.LogError("ManualSquadCreator: Squad prefab không được tìm thấy!");
            return;
        }
        
        if (islandGenerator == null)
        {
            Debug.LogError("ManualSquadCreator: IslandGenerator không được tìm thấy!");
            return;
        }
        
        // Tìm vị trí valid để tạo squad
        List<Vector3> validPositions = islandGenerator.GetValidPositions();
        if (validPositions.Count == 0)
        {
            Debug.LogError("ManualSquadCreator: Không tìm thấy vị trí valid nào!");
            return;
        }
        
        // Lọc vị trí phù hợp (west cho player, east cho enemy)
        List<Vector3> filteredPositions = new List<Vector3>();
        
        // Giả định điểm giữa ở x=0
        foreach (Vector3 pos in validPositions)
        {
            if ((isPlayer && pos.x < 0) || (!isPlayer && pos.x > 0))
                filteredPositions.Add(pos);
        }
        
        // Nếu không có vị trí phù hợp, sử dụng bất kỳ vị trí nào
        if (filteredPositions.Count == 0)
            filteredPositions = validPositions;
        
        // Chọn ngẫu nhiên một vị trí
        Vector3 squadPosition = filteredPositions[Random.Range(0, filteredPositions.Count)];
        
        // Tạo squad
        GameObject squadObj = Instantiate(squadPrefab, squadPosition, Quaternion.identity);
        
        // Đặt tên cho squad
        string teamType = isPlayer ? "Player" : "Enemy";
        int squadCount = isPlayer 
            ? (gameManager != null ? gameManager.GetPlayerSquads().Count : 0) 
            : (gameManager != null ? gameManager.GetEnemySquads().Count : 0);
        squadObj.name = teamType + "Squad_Manual_" + squadCount;
        
        // Lấy component SquadSystem
        SquadSystem squad = squadObj.GetComponent<SquadSystem>();
        if (squad == null)
        {
            Debug.LogError("ManualSquadCreator: Squad không có component SquadSystem!");
            Destroy(squadObj);
            return;
        }
        
        // Xác định số lượng troop cần tạo
        int troopCount = 5; // Mặc định là 5 troop
        if (troopCountDropdown != null)
            troopCount = troopCountDropdown.value + 1; // +1 vì dropdown bắt đầu từ 0
        
        // Lấy config phù hợp
        TroopConfigSO config = isPlayer ? playerTroopConfig : enemyTroopConfig;
        if (config == null)
        {
            Debug.LogError("ManualSquadCreator: Không tìm thấy config cho " + teamType);
            return;
        }
        
        // Tạo troops và thêm vào squad
        for (int i = 0; i < troopCount; i++)
        {
            // Tạo troop
            TroopController troop = troopFactory.CreateTroop(
                config,
                squad.transform.position + Random.insideUnitSphere * 2f,
                Quaternion.identity
            );
            
            if (troop == null)
            {
                Debug.LogError("ManualSquadCreator: Không thể tạo troop!");
                continue;
            }
            
            // Set tag
            troop.gameObject.tag = teamType;
            
            // Thêm vào squad
            squad.AddTroop(troop);
            
            // Đăng ký với TroopManager nếu có
            TroopManager troopManager = FindObjectOfType<TroopManager>();
            if (troopManager != null)
            {
                troopManager.RegisterTroop(troop);
            }
            
            if (enableDebug)
                Debug.Log("ManualSquadCreator: Đã tạo troop " + troop.name + " cho " + squadObj.name);
        }
        
        // Kết nối với GameManager
        if (gameManager != null)
        {
            if (isPlayer)
                gameManager.GetPlayerSquads().Add(squad);
            else
                gameManager.GetEnemySquads().Add(squad);
                
            if (enableDebug)
                Debug.Log("ManualSquadCreator: Đã kết nối squad với GameManager");
        }
        
        if (enableDebug)
            Debug.Log("ManualSquadCreator: Đã tạo " + squadObj.name + " tại vị trí " + squadPosition);
    }
    
    // Phương thức tiện ích để thêm một troop vào squad đã tồn tại
    public void AddTroopToSquad(SquadSystem targetSquad, bool isPlayer)
    {
        if (targetSquad == null)
        {
            Debug.LogError("ManualSquadCreator: Squad không tồn tại!");
            return;
        }
        
        if (targetSquad.IsFull())
        {
            Debug.LogError("ManualSquadCreator: Squad đã đầy!");
            return;
        }
        
        // Lấy config phù hợp
        TroopConfigSO config = isPlayer ? playerTroopConfig : enemyTroopConfig;
        if (config == null)
        {
            Debug.LogError("ManualSquadCreator: Không tìm thấy config!");
            return;
        }
        
        // Tạo troop
        TroopController troop = troopFactory.CreateTroop(
            config,
            targetSquad.transform.position + Random.insideUnitSphere * 2f,
            Quaternion.identity
        );
        
        if (troop == null)
        {
            Debug.LogError("ManualSquadCreator: Không thể tạo troop!");
            return;
        }
        
        // Set tag
        string teamType = isPlayer ? "Player" : "Enemy";
        troop.gameObject.tag = teamType;
        
        // Thêm vào squad
        bool success = targetSquad.AddTroop(troop);
        
        if (success)
        {
            // Đăng ký với TroopManager
            TroopManager troopManager = FindObjectOfType<TroopManager>();
            if (troopManager != null)
            {
                troopManager.RegisterTroop(troop);
            }
            
            if (enableDebug)
                Debug.Log("ManualSquadCreator: Đã thêm troop " + troop.name + " vào " + targetSquad.name);
        }
        else
        {
            // Không thêm được vào squad, hủy troop
            Destroy(troop.gameObject);
            Debug.LogError("ManualSquadCreator: Không thể thêm troop vào squad!");
        }
    }
    
    // Dành cho gọi từ Inspector hoặc UI Button
    public void AddTroopToSelectedSquad()
    {
        if (gameManager == null)
        {
            Debug.LogError("ManualSquadCreator: GameManager không tồn tại!");
            return;
        }
        
        SquadSystem selectedSquad = gameManager.GetSelectedSquad();
        if (selectedSquad == null)
        {
            Debug.LogError("ManualSquadCreator: Không có squad nào được chọn!");
            return;
        }
        
        // Kiểm tra squad thuộc team nào
        bool isPlayerSquad = gameManager.GetPlayerSquads().Contains(selectedSquad);
        
        // Thêm troop vào squad được chọn
        AddTroopToSquad(selectedSquad, isPlayerSquad);
    }
}