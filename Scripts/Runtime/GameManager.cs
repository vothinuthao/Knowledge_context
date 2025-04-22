using UnityEngine;
using System.Collections.Generic;
using Core.Patterns.Singleton;
using Generate;
using Troop;

public class GameManager : ManualSingletonMono<GameManager>
{
    [Header("References")]
    [Tooltip("Factory để tạo troop")]
    public TroopFactory troopFactory;
    
    [Tooltip("Quản lý troop")]
    public TroopManager troopManager;
    
    [Header("Gameplay Settings")]
    [Tooltip("Prefab của squad")]
    public GameObject squadPrefab;
    
    [Tooltip("Vị trí spawn các squad ban đầu")]
    public Transform[] squadSpawnPoints;
    
    [Tooltip("Troop config cho player troops")]
    public TroopConfigSO playerTroopConfig;
    
    [Tooltip("Troop config cho enemy troops")]
    public TroopConfigSO enemyTroopConfig;
    
    [Header("References")]
    [Tooltip("Factory để tạo troop")]
    public IslandGenerator islandGen;
    
    [Header("Debug")]
    [Tooltip("Enable debug visualization")]
    public bool enableDebug = true;

    public LayerMask groundLayer;
    // Runtime data
    private List<SquadSystem> playerSquads = new List<SquadSystem>();
    private List<SquadSystem> enemySquads = new List<SquadSystem>();
    private SquadSystem selectedSquad;
    
    
    private void Awake()
    {
        
        // Khởi tạo singleton managers nếu chưa có
        // if (troopFactory == null)
        // {
        //     troopFactory = FindObjectOfType<TroopFactory>();
        //     if (troopFactory == null)
        //     {
        //         GameObject factoryObj = new GameObject("TroopFactory");
        //         troopFactory = factoryObj.AddComponent<TroopFactory>();
        //     }
        // }
        //
        // if (troopManager == null)
        // {
        //     troopManager = FindObjectOfType<TroopManager>();
        //     if (troopManager == null)
        //     {
        //         GameObject managerObj = new GameObject("TroopManager");
        //         troopManager = managerObj.AddComponent<TroopManager>();
        //     }
        // }
    }
    
    private void Start()
    {
    }
    
    private void Update()
    {
        // Xử lý input và game logic
        HandleInput();
    }
    
    // Xử lý input người chơi
    private void HandleInput()
    {
        // Xử lý chuột phải để chọn squad
        if (Input.GetMouseButtonDown(0))
        {
            SelectSquadUnderMouse();
        }
        
        // Xử lý chuột trái để di chuyển squad đã chọn
        if (Input.GetMouseButtonDown(1) && selectedSquad != null)
        {
            MoveSelectedSquad();
        }
    }
    
    // Tạo các squad ban đầu
    // Tạo các squad ban đầu
private void CreateInitialSquads()
{
    // Kiểm tra các điều kiện
    if (squadPrefab == null)
    {
        Debug.LogError("GameManager: Không tìm thấy squadPrefab!");
        return;
    }
    
    if (playerTroopConfig == null)
    {
        Debug.LogError("GameManager: Không tìm thấy playerTroopConfig!");
        return;
    }
    
    // Lấy danh sách vị trí valid từ IslandGenerator
    if (islandGen == null)
    {
        Debug.LogError("GameManager: Không tìm thấy IslandGenerator!");
        return;
    }
    
    List<Vector3> validPositions = islandGen.GetValidPositions();
    if (validPositions.Count < 4)
    {
        Debug.LogError("GameManager: Không đủ vị trí valid để tạo squad!");
        return;
    }
    
    // Chia validPositions thành 2 khu vực: west (player) và east (enemy)
    List<Vector3> westPositions = new List<Vector3>();
    List<Vector3> eastPositions = new List<Vector3>();
    
    float midX = 0f; // Giả định điểm giữa ở x=0
    foreach (Vector3 pos in validPositions)
    {
        if (pos.x < midX)
            westPositions.Add(pos);
        else
            eastPositions.Add(pos);
    }
    
    // Tạo các squad cho người chơi (2 squad ở phía west)
    int playerSquadCount = Mathf.Min(2, westPositions.Count);
    for (int i = 0; i < playerSquadCount && westPositions.Count > 0; i++)
    {
        // Chọn ngẫu nhiên một vị trí từ west
        int randomIndex = Random.Range(0, westPositions.Count);
        Vector3 squadPos = westPositions[randomIndex];
        westPositions.RemoveAt(randomIndex); // Loại bỏ để không chọn lại
        
        // Tạo squad
        GameObject squadObj = Instantiate(squadPrefab, squadPos, Quaternion.identity);
        squadObj.name = "PlayerSquad_" + i;
        
        SquadSystem squad = squadObj.GetComponent<SquadSystem>();
        if (squad != null)
        {
            playerSquads.Add(squad);
            
            // Thêm troops vào squad (spawn tại vị trí của squad)
            PopulateSquad(squad, playerTroopConfig, "Player", 5);
        }
    }
    
    // Tạo các squad cho kẻ địch nếu có config (2 squad ở phía east)
    if (enemyTroopConfig != null && eastPositions.Count > 0)
    {
        int enemySquadCount = Mathf.Min(2, eastPositions.Count);
        for (int i = 0; i < enemySquadCount && eastPositions.Count > 0; i++)
        {
            // Chọn ngẫu nhiên một vị trí từ east
            int randomIndex = Random.Range(0, eastPositions.Count);
            Vector3 squadPos = eastPositions[randomIndex];
            eastPositions.RemoveAt(randomIndex); // Loại bỏ để không chọn lại
            
            // Tạo squad
            GameObject squadObj = Instantiate(squadPrefab, squadPos, Quaternion.identity);
            squadObj.name = "EnemySquad_" + i;
            
            SquadSystem squad = squadObj.GetComponent<SquadSystem>();
            if (squad != null)
            {
                enemySquads.Add(squad);
                
                // Thêm troops vào squad (spawn tại vị trí của squad)
                PopulateSquad(squad, enemyTroopConfig, "Enemy", 5);
            }
        }
    }
}
    
    // Thêm troops vào squad
    private void PopulateSquad(SquadSystem squad, TroopConfigSO troopConfig, string tag, int count)
    {
        count = Mathf.Min(count, 9);
        
        for (int i = 0; i < count; i++)
        {
            TroopController troop = troopFactory.CreateTroop(
                troopConfig,
                squad.transform.position + Random.insideUnitSphere * 2f,
                Quaternion.identity
            );
            troop.gameObject.tag = tag;
            squad.AddTroop(troop);
            troopManager.RegisterTroop(troop);
        }
    }
    
    // Chọn squad dưới chuột
    private void SelectSquadUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            // Kiểm tra xem hit có phải là squad không
            SquadSystem hitSquad = hit.transform.GetComponent<SquadSystem>();
            if (hitSquad != null && playerSquads.Contains(hitSquad))
            {
                // Bỏ chọn squad cũ
                if (selectedSquad != null)
                {
                    // Có thể làm highlight effct ở đây
                }
                
                // Chọn squad mới
                selectedSquad = hitSquad;
                
                // Có thể làm highlight effect ở đây
                
                Debug.Log("Selected squad: " + selectedSquad.name);
            }
        }
    }
    
    // Di chuyển squad đã chọn đến vị trí chuột
    private void MoveSelectedSquad()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
    
        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            // Di chuyển squad đến vị trí hit
            Vector3 targetPosition = hit.point;
        
            // Tính hướng nhìn về điểm đến (chỉ xoay trên mặt phẳng XZ)
            Vector3 direction = targetPosition - selectedSquad.transform.position;
            direction.y = 0; // Giữ góc quay trên mặt phẳng XZ
        
            if (direction != Vector3.zero)
            {
                // Hiệu ứng visual cho move command (optional)
                // CreateMoveMarker(hit.point);
            
                // Di chuyển và quay squad
                selectedSquad.MoveToPosition(targetPosition);
                selectedSquad.RotateToDirection(direction);
            
                Debug.Log("Moving squad to: " + targetPosition);
            }
        }
    }// Optional: Tạo marker để thể hiện lệnh di chuyển
    // private void CreateMoveMarker(Vector3 position)
    // {
    //     if (_moveMarkerPrefab != null)
    //     {
    //         GameObject marker = Instantiate(moveMarkerPrefab, position, Quaternion.identity);
    //         Destroy(marker, 2f); // Tự hủy sau 2 giây
    //     }
    // }
    
    
    // Lấy các squad của người chơi
    public List<SquadSystem> GetPlayerSquads()
    {
        return playerSquads;
    }
    
    // Lấy các squad của kẻ địch
    public List<SquadSystem> GetEnemySquads()
    {
        return enemySquads;
    }
    
    // Lấy squad đã chọn
    public SquadSystem GetSelectedSquad()
    {
        return selectedSquad;
    }
    
    public bool AddTroopToSquad(TroopController troop, SquadSystem squad)
    {
        if (troop == null || squad == null)
            return false;
        SquadSystem oldSquad = TroopControllerSquadExtensions.Instance.GetSquad(troop);
        if (oldSquad != null)
        {
            oldSquad.RemoveTroop(troop);
        }
        
        // Thêm vào squad mới
        return squad.AddTroop(troop);
    }
    
    // OnGUI cho debug
    private void OnGUI()
    {
        if (!enableDebug) return;
        
        // Hiển thị thông tin debug
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        GUILayout.Label("Game Debug Info", GUI.skin.box);
        
        GUILayout.Label("Player Squads: " + playerSquads.Count);
        GUILayout.Label("Enemy Squads: " + enemySquads.Count);
        
        if (selectedSquad != null)
        {
            GUILayout.Label("Selected Squad: " + selectedSquad.name);
            GUILayout.Label("Troop Count: " + selectedSquad.GetTroopCount());
        }
        else
        {
            GUILayout.Label("No Squad Selected");
        }
        
        GUILayout.Label("Controls:");
        GUILayout.Label("Left Click: Select Squad");
        GUILayout.Label("Right Click: Move Selected Squad");
        
        GUILayout.EndArea();
    }
}