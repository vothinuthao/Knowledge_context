using System.Collections.Generic;
using Core.Patterns.Singleton;
using UnityEngine;

namespace Troop
{
    public class TroopManager : ManualSingletonMono<TroopManager>
    {
        [Tooltip("Bán kính phát hiện các troop gần đó")]
        public float nearbyTroopDetectionRadius = 10f;
    
        [Tooltip("Tần suất cập nhật danh sách troop gần đó (giây)")]
        public float nearbyTroopsUpdateInterval = 0.5f;
        
        private List<TroopController> _allTroops = new List<TroopController>();
        private Dictionary<TroopController, TroopController[]> _nearbyAlliesCache = new Dictionary<TroopController, TroopController[]>();
        private Dictionary<TroopController, TroopController[]> _nearbyEnemiesCache = new Dictionary<TroopController, TroopController[]>();
        private float _nearbyTroopsTimer = 0f;
        
        public void RegisterTroop(TroopController troop)
        {
            if (!_allTroops.Contains(troop))
            {
                _allTroops.Add(troop);
            }
        }
    
        public void UnregisterTroop(TroopController troop)
        {
            _allTroops.Remove(troop);
            _nearbyAlliesCache.Remove(troop);
            _nearbyEnemiesCache.Remove(troop);
        }
    
        private void Update()
        {
            _nearbyTroopsTimer += Time.deltaTime;
            if (_nearbyTroopsTimer >= nearbyTroopsUpdateInterval)
            {
                UpdateNearbyTroopsCache();
                _nearbyTroopsTimer = 0f;
            }
        
            // Cập nhật thông tin cho mỗi troop
            foreach (var troop in _allTroops)
            {
                if (troop == null) continue;
            
                // Lấy allies và enemies gần đó
                TroopController[] nearbyAllies = GetNearbyAllies(troop);
                TroopController[] nearbyEnemies = GetNearbyEnemies(troop);
            
                // Cập nhật thông tin cho troop
                troop.SetNearbyTroops(nearbyAllies, nearbyEnemies);
            }
        }
    
        // Cập nhật cache các troop gần đó
        private void UpdateNearbyTroopsCache()
        {
            _nearbyAlliesCache.Clear();
            _nearbyEnemiesCache.Clear();
        
            foreach (var troop in _allTroops)
            {
                if (troop == null) continue;
            
                // Tìm kiếm allies và enemies gần đó
                List<TroopController> allies = new List<TroopController>();
                List<TroopController> enemies = new List<TroopController>();
            
                foreach (var other in _allTroops)
                {
                    if (other == null || other == troop) continue;
                
                    // Tính khoảng cách
                    float distance = Vector3.Distance(troop.GetPosition(), other.GetPosition());
                
                    if (distance <= nearbyTroopDetectionRadius)
                    {
                        // Kiểm tra xem có phải đồng minh không (đơn giản là cùng tag)
                        if (other.gameObject.CompareTag(troop.gameObject.tag))
                        {
                            allies.Add(other);
                        }
                        else
                        {
                            enemies.Add(other);
                        }
                    }
                }
            
                // Lưu vào cache
                _nearbyAlliesCache[troop] = allies.ToArray();
                _nearbyEnemiesCache[troop] = enemies.ToArray();
            }
        }
    
        // Lấy danh sách đồng minh gần đó
        public TroopController[] GetNearbyAllies(TroopController troop)
        {
            if (_nearbyAlliesCache.TryGetValue(troop, out TroopController[] allies))
            {
                return allies;
            }
            return new TroopController[0];
        }
    
        // Lấy danh sách kẻ địch gần đó
        public TroopController[] GetNearbyEnemies(TroopController troop)
        {
            if (_nearbyEnemiesCache.TryGetValue(troop, out TroopController[] enemies))
            {
                return enemies;
            }
            return new TroopController[0];
        }
    
        // Lấy tất cả troop trong vùng chỉ định
        public TroopController[] GetTroopsInArea(Vector3 center, float radius)
        {
            List<TroopController> troopsInArea = new List<TroopController>();
        
            foreach (var troop in _allTroops)
            {
                if (troop == null) continue;
            
                float distance = Vector3.Distance(center, troop.GetPosition());
                if (distance <= radius)
                {
                    troopsInArea.Add(troop);
                }
            }
        
            return troopsInArea.ToArray();
        }
    }
}