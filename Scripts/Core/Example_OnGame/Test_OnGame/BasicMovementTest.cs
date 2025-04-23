using System.Collections.Generic;
using Troop;
using UnityEngine;

namespace Core.Example_OnGame
{
    public class BasicMovementTest : MonoBehaviour
    {
        public TroopFactory troopFactory;
        public Transform[] waypoints;
        private List<TroopController> troops = new List<TroopController>();
        private int currentWaypointIndex = 0;
    
        void Start()
        {
            // Tìm TroopFactory
            troopFactory = FindObjectOfType<TroopFactory>();
        
            // Tạo waypoints nếu chưa có
            if (waypoints == null || waypoints.Length == 0)
            {
                CreateWaypoints();
            }
        
            // Tạo 5 infantry troops
            for (int i = 0; i < 5; i++)
            {
                Vector3 position = transform.position + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
                TroopController troop = troopFactory.CreateTroopByType(TroopClassType.Infantry, "Player", position, Quaternion.identity);
                troops.Add(troop);
            }
        }
    
        void Update()
        {
            // Kiểm tra xem tất cả troops đã đến waypoint hiện tại chưa
            bool allTroopsReached = true;
            foreach (var troop in troops)
            {
                if (troop == null) continue;
            
                float distance = Vector3.Distance(troop.GetPosition(), waypoints[currentWaypointIndex].position);
                if (distance > 1.0f)
                {
                    allTroopsReached = false;
                    break;
                }
            }
        
            // Nếu tất cả đã đến, di chuyển đến waypoint tiếp theo
            if (allTroopsReached)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            
                // Đặt target mới cho tất cả troops
                foreach (var troop in troops)
                {
                    if (troop == null) continue;
                    troop.SetTargetPosition(waypoints[currentWaypointIndex].position);
                }
            }
        }
    
        void CreateWaypoints()
        {
            // Tạo 4 waypoints hình vuông
            waypoints = new Transform[4];
        
            float distance = 10f;
        
            for (int i = 0; i < 4; i++)
            {
                GameObject waypointObj = new GameObject($"Waypoint_{i}");
                waypointObj.transform.SetParent(transform);
            
                float angle = i * (Mathf.PI / 2); // 90 degrees
                float x = Mathf.Cos(angle) * distance;
                float z = Mathf.Sin(angle) * distance;
            
                waypointObj.transform.position = transform.position + new Vector3(x, 0, z);
            
                // Tạo visual marker
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.transform.SetParent(waypointObj.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
                // Đặt material để dễ nhìn
                Renderer renderer = marker.GetComponent<Renderer>();
                renderer.material.color = Color.yellow;
            
                waypoints[i] = waypointObj.transform;
            }
        }
    }
}