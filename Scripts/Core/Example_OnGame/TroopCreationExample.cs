using System.Collections.Generic;
using UnityEngine;
using Troop;
using SteeringBehavior;

public class TroopCreationExample : MonoBehaviour
{
    [Header("Factories")]
    public TroopFactory troopFactory;
    
    [Header("Spawn Settings")]
    public Transform playerSpawnPoint;
    public Transform enemySpawnPoint;
    
    [Header("Squad References")]
    public GameObject squadPrefab;
    
    private List<TroopController> _playerTroops = new List<TroopController>();
    private List<TroopController> _enemyTroops = new List<TroopController>();
    private List<SquadSystem> _playerSquads = new List<SquadSystem>();
    private List<SquadSystem> _enemySquads = new List<SquadSystem>();
    
    // Khi bắt đầu, tạo một số troop mẫu
    void Start()
    {
        if (troopFactory == null)
        {
            troopFactory = FindObjectOfType<TroopFactory>();
        }
        
        if (troopFactory == null)
        {
            Debug.LogError("TroopCreationExample: Không tìm thấy TroopFactory!");
            return;
        }
        
        // Tạo một squad cho player
        CreatePlayerSquad();
        
        // Tạo một squad cho enemy
        CreateEnemySquad();
    }
    
    // Tạo một squad cho player
    public void CreatePlayerSquad()
    {
        if (playerSpawnPoint == null || squadPrefab == null) return;
        
        // Tạo squad GameObject
        GameObject squadObject = Instantiate(squadPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
        squadObject.name = "PlayerSquad_" + _playerSquads.Count;
        
        // Lấy SquadSystem component
        SquadSystem squad = squadObject.GetComponent<SquadSystem>();
        if (squad == null)
        {
            Debug.LogError("TroopCreationExample: Squad prefab không có SquadSystem component!");
            Destroy(squadObject);
            return;
        }
        
        _playerSquads.Add(squad);
        
        // Tạo các loại troop khác nhau cho squad
        CreateTroopsForPlayerSquad(squad);
    }
    
    // Tạo các loại troop khác nhau cho player squad
    private void CreateTroopsForPlayerSquad(SquadSystem squad)
    {
        // Vị trí spawn random xung quanh squad
        Vector3 squadPosition = squad.transform.position;
        
        // Tạo HeavyInfantry cho đội hình phòng thủ
        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPos = squadPosition + Random.insideUnitSphere * 3f;
            spawnPos.y = 0;
            
            TroopController troop = troopFactory.CreateTroopByType(
                TroopClassType.HeavyInfantry, 
                "Player", 
                spawnPos, 
                Quaternion.identity);
            
            if (troop != null)
            {
                // Thêm vào squad
                squad.AddTroop(troop);
                
                // Thêm vào danh sách quản lý
                _playerTroops.Add(troop);
                
                // Đăng ký với TroopManager
                TroopManager.Instance.RegisterTroop(troop);
            }
        }
        
        // Tạo Berserker cho tấn công
        for (int i = 0; i < 2; i++)
        {
            Vector3 spawnPos = squadPosition + Random.insideUnitSphere * 3f;
            spawnPos.y = 0;
            
            TroopController troop = troopFactory.CreateTroopByType(
                TroopClassType.Berserker, 
                "Player", 
                spawnPos, 
                Quaternion.identity);
            
            if (troop != null)
            {
                // Thêm vào squad
                squad.AddTroop(troop);
                
                // Thêm vào danh sách quản lý
                _playerTroops.Add(troop);
                
                // Đăng ký với TroopManager
                TroopManager.Instance.RegisterTroop(troop);
            }
        }
        
        // Tạo Archer cho hỗ trợ tầm xa
        for (int i = 0; i < 2; i++)
        {
            Vector3 spawnPos = squadPosition + Random.insideUnitSphere * 3f;
            spawnPos.y = 0;
            
            TroopController troop = troopFactory.CreateTroopByType(
                TroopClassType.Archer, 
                "Player", 
                spawnPos, 
                Quaternion.identity);
            
            if (troop != null)
            {
                // Thêm vào squad
                squad.AddTroop(troop);
                
                // Thêm vào danh sách quản lý
                _playerTroops.Add(troop);
                
                // Đăng ký với TroopManager
                TroopManager.Instance.RegisterTroop(troop);
            }
        }
        
        // Tạo Commander để hỗ trợ đồng đội
        Vector3 commanderPos = squadPosition + Random.insideUnitSphere * 2f;
        commanderPos.y = 0;
        
        TroopController commander = troopFactory.CreateTroopByType(
            TroopClassType.Commander, 
            "Player", 
            commanderPos, 
            Quaternion.identity);
        
        if (commander != null)
        {
            // Thêm vào squad
            squad.AddTroop(commander);
            
            // Thêm vào danh sách quản lý
            _playerTroops.Add(commander);
            
            // Đăng ký với TroopManager
            TroopManager.Instance.RegisterTroop(commander);
        }
    }
    
    // Tạo một squad cho enemy
    public void CreateEnemySquad()
    {
        if (enemySpawnPoint == null || squadPrefab == null) return;
        
        // Tạo squad GameObject
        GameObject squadObject = Instantiate(squadPrefab, enemySpawnPoint.position, enemySpawnPoint.rotation);
        squadObject.name = "EnemySquad_" + _enemySquads.Count;
        
        // Lấy SquadSystem component
        SquadSystem squad = squadObject.GetComponent<SquadSystem>();
        if (squad == null)
        {
            Debug.LogError("TroopCreationExample: Squad prefab không có SquadSystem component!");
            Destroy(squadObject);
            return;
        }
        
        _enemySquads.Add(squad);
        
        // Tạo các loại troop khác nhau cho squad
        CreateTroopsForEnemySquad(squad);
    }
    
    // Tạo các loại troop khác nhau cho enemy squad
    private void CreateTroopsForEnemySquad(SquadSystem squad)
    {
        // Vị trí spawn random xung quanh squad
        Vector3 squadPosition = squad.transform.position;
        
        // Tạo Infantry cơ bản
        for (int i = 0; i < 4; i++)
        {
            Vector3 spawnPos = squadPosition + Random.insideUnitSphere * 3f;
            spawnPos.y = 0;
            
            TroopController troop = troopFactory.CreateTroopByType(
                TroopClassType.Infantry, 
                "Enemy", 
                spawnPos, 
                Quaternion.identity);
            
            if (troop != null)
            {
                // Thêm vào squad
                squad.AddTroop(troop);
                
                // Thêm vào danh sách quản lý
                _enemyTroops.Add(troop);
                
                // Đăng ký với TroopManager
                TroopManager.Instance.RegisterTroop(troop);
            }
        }
        
        // Tạo Assassin để tấn công nhanh
        for (int i = 0; i < 2; i++)
        {
            Vector3 spawnPos = squadPosition + Random.insideUnitSphere * 3f;
            spawnPos.y = 0;
            
            TroopController troop = troopFactory.CreateTroopByType(
                TroopClassType.Assassin, 
                "Enemy", 
                spawnPos, 
                Quaternion.identity);
            
            if (troop != null)
            {
                // Thêm vào squad
                squad.AddTroop(troop);
                
                // Thêm vào danh sách quản lý
                _enemyTroops.Add(troop);
                
                // Đăng ký với TroopManager
                TroopManager.Instance.RegisterTroop(troop);
            }
        }
        
        // Tạo Scout để trinh sát
        for (int i = 0; i < 2; i++)
        {
            Vector3 spawnPos = squadPosition + Random.insideUnitSphere * 3f;
            spawnPos.y = 0;
            
            TroopController troop = troopFactory.CreateTroopByType(
                TroopClassType.Scout, 
                "Enemy", 
                spawnPos, 
                Quaternion.identity);
            
            if (troop != null)
            {
                // Thêm vào squad
                squad.AddTroop(troop);
                
                // Thêm vào danh sách quản lý
                _enemyTroops.Add(troop);
                
                // Đăng ký với TroopManager
                TroopManager.Instance.RegisterTroop(troop);
            }
        }
    }
    
    // Tạo một troop tùy chỉnh với behavior cụ thể
    public TroopController CreateCustomTroop(Vector3 position, Quaternion rotation)
    {
        // Tạo config mới
        TroopConfigSO config = ScriptableObject.CreateInstance<TroopConfigSO>();
        config.troopName = "Custom Viking";
        config.health = 120f;
        config.attackPower = 15f;
        config.moveSpeed = 3.5f;
        config.attackRange = 2f;
        config.attackSpeed = 1.2f;
        
        // Thêm behavior cơ bản
        config.behaviors = new List<SteeringBehaviorSO>();
        
        // Thêm SeekBehavior
        SeekBehaviorSO seekBehavior = ScriptableObject.CreateInstance<SeekBehaviorSO>();
        seekBehavior.weight = 1.0f;
        config.behaviors.Add(seekBehavior);
        
        // Thêm ArrivalBehavior
        ArrivalBehaviorSO arrivalBehavior = ScriptableObject.CreateInstance<ArrivalBehaviorSO>();
        arrivalBehavior.weight = 1.5f;
        arrivalBehavior.slowingDistance = 3f;
        config.behaviors.Add(arrivalBehavior);
        
        // Thêm SeparationBehavior
        SeparationBehaviorSO separationBehavior = ScriptableObject.CreateInstance<SeparationBehaviorSO>();
        separationBehavior.weight = 1.2f;
        separationBehavior.separationRadius = 2f;
        config.behaviors.Add(separationBehavior);
        
        // Thêm ChargeBehavior
        ChargeBehaviorSO chargeBehavior = ScriptableObject.CreateInstance<ChargeBehaviorSO>();
        chargeBehavior.weight = 2.0f;
        chargeBehavior.chargeDistance = 10f;
        chargeBehavior.chargeSpeedMultiplier = 2.5f;
        chargeBehavior.chargeDamageMultiplier = 2f;
        chargeBehavior.chargePreparationTime = 1f;
        chargeBehavior.chargeCooldown = 8f;
        config.behaviors.Add(chargeBehavior);
        
        // Tạo troop
        TroopController troop = troopFactory.CreateTroop(config, position, rotation);
        
        // Set tag
        troop.gameObject.tag = "Player";
        
        // Đăng ký với TroopManager
        TroopManager.Instance.RegisterTroop(troop);
        
        return troop;
    }
    
    // Tạo một squad với chức năng phòng thủ (Phalanx và Testudo)
    public void CreateDefensiveSquad(Vector3 position, Quaternion rotation)
    {
        if (squadPrefab == null) return;
        
        // Tạo squad GameObject
        GameObject squadObject = Instantiate(squadPrefab, position, rotation);
        squadObject.name = "DefensiveSquad_" + _playerSquads.Count;
        
        // Lấy SquadSystem component
        SquadSystem squad = squadObject.GetComponent<SquadSystem>();
        if (squad == null)
        {
            Debug.LogError("TroopCreationExample: Squad prefab không có SquadSystem component!");
            Destroy(squadObject);
            return;
        }
        
        _playerSquads.Add(squad);
        
        // Tạo các troop với behavior phòng thủ
        
        // Tạo HeavyInfantry với Phalanx behavior
        for (int i = 0; i < 5; i++)
        {
            Vector3 spawnPos = position + Random.insideUnitSphere * 3f;
            spawnPos.y = 0;
            
            TroopController troop = troopFactory.CreateTroopByType(
                TroopClassType.HeavyInfantry, 
                "Player", 
                spawnPos, 
                Quaternion.identity);
            
            if (troop != null)
            {
                // Thêm behavior Phalanx thủ công
                PhalanxBehaviorSO phalanxBehavior = ScriptableObject.CreateInstance<PhalanxBehaviorSO>();
                phalanxBehavior.weight = 2.5f;
                phalanxBehavior.formationSpacing = 1.5f;
                phalanxBehavior.movementSpeedMultiplier = 0.7f;
                phalanxBehavior.maxRowsInFormation = 3;
                
                // Thêm behavior vào troop
                troop.GetModel().SteeringBehavior.AddStrategy(phalanxBehavior.Create());
                
                // Thêm vào squad
                squad.AddTroop(troop);
                
                // Thêm vào danh sách quản lý
                _playerTroops.Add(troop);
                
                // Đăng ký với TroopManager
                TroopManager.Instance.RegisterTroop(troop);
            }
        }
        
        // Tạo Defender với Testudo behavior
        for (int i = 0; i < 4; i++)
        {
            Vector3 spawnPos = position + Random.insideUnitSphere * 3f;
            spawnPos.y = 0;
            
            TroopController troop = troopFactory.CreateTroopByType(
                TroopClassType.Defender, 
                "Player", 
                spawnPos, 
                Quaternion.identity);
            
            if (troop != null)
            {
                // Thêm vào squad
                squad.AddTroop(troop);
                
                // Thêm vào danh sách quản lý
                _playerTroops.Add(troop);
                
                // Đăng ký với TroopManager
                TroopManager.Instance.RegisterTroop(troop);
            }
        }
    }
    
    // Command squads to battle
    public void CommandBattle()
    {
        if (_playerSquads.Count == 0 || _enemySquads.Count == 0) return;
        
        // Get first squads
        SquadSystem playerSquad = _playerSquads[0];
        SquadSystem enemySquad = _enemySquads[0];
        
        // Move player squad towards enemy
        Vector3 direction = enemySquad.transform.position - playerSquad.transform.position;
        Vector3 midPoint = playerSquad.transform.position + direction * 0.5f;
        
        playerSquad.MoveToPosition(midPoint);
        
        // For each player troop, set to attacking state
        foreach (TroopController troop in _playerTroops)
        {
            if (troop != null && troop.IsAlive())
            {
                troop.StateMachine.ChangeState<AttackingState>();
            }
        }
        
        // Move enemy squad towards player
        enemySquad.MoveToPosition(midPoint);
        
        // For each enemy troop, set to attacking state
        foreach (TroopController troop in _enemyTroops)
        {
            if (troop != null && troop.IsAlive())
            {
                troop.StateMachine.ChangeState<AttackingState>();
            }
        }
    }
    
    // Command defensive formation
    public void CommandDefensiveFormation()
    {
        if (_playerSquads.Count == 0) return;
        
        // Get first squad
        SquadSystem playerSquad = _playerSquads[0];
        
        // For each player troop, enable defensive behaviors
        foreach (TroopController troop in _playerTroops)
        {
            if (troop != null && troop.IsAlive())
            {
                // Switch to defensive state
                troop.StateMachine.ChangeState<DefendingState>();
                
                // Enable phalanx or testudo behavior if available
                if (troop.IsBehaviorEnabled("Phalanx"))
                {
                    troop.EnableBehavior("Phalanx", true);
                }
                
                if (troop.IsBehaviorEnabled("Testudo"))
                {
                    troop.EnableBehavior("Testudo", true);
                }
                
                if (troop.IsBehaviorEnabled("Protect"))
                {
                    troop.EnableBehavior("Protect", true);
                }
            }
        }
    }
    
    // Chạy trên UI để demo các lệnh khác nhau
    void OnGUI()
    {
        // Tạo các nút UI để thử nghiệm
        if (GUI.Button(new Rect(10, 10, 150, 30), "Create Player Squad"))
        {
            CreatePlayerSquad();
        }
        
        if (GUI.Button(new Rect(10, 50, 150, 30), "Create Enemy Squad"))
        {
            CreateEnemySquad();
        }
        
        if (GUI.Button(new Rect(10, 90, 150, 30), "Create Custom Troop"))
        {
            Vector3 position = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
            CreateCustomTroop(position, Quaternion.identity);
        }
        
        if (GUI.Button(new Rect(10, 130, 150, 30), "Create Defensive Squad"))
        {
            Vector3 position = playerSpawnPoint != null ? playerSpawnPoint.position + Vector3.right * 10 : Vector3.right * 10;
            CreateDefensiveSquad(position, Quaternion.identity);
        }
        
        if (GUI.Button(new Rect(10, 170, 150, 30), "Command Battle"))
        {
            CommandBattle();
        }
        
        if (GUI.Button(new Rect(10, 210, 150, 30), "Defensive Formation"))
        {
            CommandDefensiveFormation();
        }
    }
}