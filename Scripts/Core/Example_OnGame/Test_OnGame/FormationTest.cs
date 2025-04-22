using System.Collections.Generic;
using Troop;
using UnityEngine;

namespace Core.Example_OnGame
{
    public class FormationTest : MonoBehaviour
    {
        public TroopFactory troopFactory;
        public Transform targetPoint;
        public Transform enemyPoint;
    
        private List<TroopController> phalanxTroops = new List<TroopController>();
        private List<TroopController> testudoTroops = new List<TroopController>();
        private List<TroopController> enemyTroops = new List<TroopController>();
    
        private bool isDefending = false;
    
        void Start()
        {
            // Tìm TroopFactory
            troopFactory = FindObjectOfType<TroopFactory>();
        
            // Tạo target point
            if (targetPoint == null)
            {
                GameObject targetObj = new GameObject("TargetPoint");
                targetObj.transform.SetParent(transform);
                targetObj.transform.position = transform.position + new Vector3(0, 0, 10);
            
                // Visual marker
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.transform.SetParent(targetObj.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = new Vector3(1, 0.1f, 1);
                marker.GetComponent<Renderer>().material.color = Color.blue;
            
                targetPoint = targetObj.transform;
            }
        
            // Tạo enemy point
            if (enemyPoint == null)
            {
                GameObject enemyObj = new GameObject("EnemyPoint");
                enemyObj.transform.SetParent(transform);
                enemyObj.transform.position = transform.position + new Vector3(0, 0, 20);
            
                // Visual marker
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.transform.SetParent(enemyObj.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = new Vector3(1, 0.1f, 1);
                marker.GetComponent<Renderer>().material.color = Color.red;
            
                enemyPoint = enemyObj.transform;
            }
        
            // Tạo phalanx squad (5 HeavyInfantry)
            Vector3 phalanxPosition = transform.position + new Vector3(-5, 0, 0);
            for (int i = 0; i < 5; i++)
            {
                Vector3 spawnPos = phalanxPosition + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                TroopController troop = troopFactory.CreateTroopByType(TroopClassType.HeavyInfantry, "Player", spawnPos, Quaternion.identity);
                phalanxTroops.Add(troop);
            }
        
            // Tạo testudo squad (9 Defender)
            Vector3 testudoPosition = transform.position + new Vector3(5, 0, 0);
            for (int i = 0; i < 9; i++)
            {
                Vector3 spawnPos = testudoPosition + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                TroopController troop = troopFactory.CreateTroopByType(TroopClassType.Defender, "Player", spawnPos, Quaternion.identity);
                testudoTroops.Add(troop);
            }
        
            // Tạo enemy squad
            for (int i = 0; i < 10; i++)
            {
                Vector3 spawnPos = enemyPoint.position + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
                TroopController enemy = troopFactory.CreateTroopByType(TroopClassType.Infantry, "Enemy", spawnPos, Quaternion.identity);
                enemyTroops.Add(enemy);
            }
        }
    
        void OnGUI()
        {
            // Nút để chuyển đổi giữa di chuyển và phòng thủ
            if (GUI.Button(new Rect(10, 10, 150, 30), isDefending ? "Switch to Moving" : "Switch to Defending"))
            {
                isDefending = !isDefending;
            
                if (isDefending)
                {
                    // Vào trạng thái phòng thủ
                    SetDefendingFormation();
                }
                else
                {
                    // Vào trạng thái di chuyển
                    SetMovingFormation();
                }
            }
        
            // Nút để kích hoạt cuộc tấn công
            if (GUI.Button(new Rect(10, 50, 150, 30), "Trigger Enemy Attack"))
            {
                TriggerEnemyAttack();
            }
        }
    
        void SetDefendingFormation()
        {
            // Cho tất cả troops ở trạng thái phòng thủ
            foreach (var troop in phalanxTroops)
            {
                if (troop == null) continue;
                troop.StateMachine.ChangeState<DefendingState>();
            
                // Kích hoạt Phalanx behavior
                troop.EnableBehavior("Phalanx", true);
            }
        
            foreach (var troop in testudoTroops)
            {
                if (troop == null) continue;
                troop.StateMachine.ChangeState<DefendingState>();
            
                // Kích hoạt Testudo behavior
                troop.EnableBehavior("Testudo", true);
            }
        }
    
        void SetMovingFormation()
        {
            // Cho tất cả troops di chuyển đến target
            foreach (var troop in phalanxTroops)
            {
                if (troop == null) continue;
                troop.SetTargetPosition(targetPoint.position);
                troop.StateMachine.ChangeState<MovingState>();
            
                // Vô hiệu hóa Phalanx behavior
                troop.EnableBehavior("Phalanx", false);
            }
        
            foreach (var troop in testudoTroops)
            {
                if (troop == null) continue;
                troop.SetTargetPosition(targetPoint.position);
                troop.StateMachine.ChangeState<MovingState>();
            
                // Vô hiệu hóa Testudo behavior
                troop.EnableBehavior("Testudo", false);
            }
        }
    
        void TriggerEnemyAttack()
        {
            // Cho tất cả enemy tấn công
            foreach (var enemy in enemyTroops)
            {
                if (enemy == null) continue;
            
                // Chọn target (lấy phalanx hoặc testudo ngẫu nhiên)
                Vector3 targetPos;
                if (Random.value < 0.5f && phalanxTroops.Count > 0)
                {
                    int index = Random.Range(0, phalanxTroops.Count);
                    targetPos = phalanxTroops[index].GetPosition();
                }
                else if (testudoTroops.Count > 0)
                {
                    int index = Random.Range(0, testudoTroops.Count);
                    targetPos = testudoTroops[index].GetPosition();
                }
                else
                {
                    targetPos = transform.position;
                }
            
                // Set target position và chuyển sang trạng thái tấn công
                enemy.SetTargetPosition(targetPos);
                enemy.StateMachine.ChangeState<AttackingState>();
            }
        }
    }
}