using UnityEngine;

namespace RavenDeckbuilding.Systems.Cards
{
    /// <summary>
    /// Runtime instance of a card
    /// </summary>
    public class CardInstance
    {
        public CardData data;
        public string instanceId;
        public float lastUsedTime;
        public bool isOnCooldown;
        
        public CardInstance(CardData cardData)
        {
            data = cardData;
            instanceId = System.Guid.NewGuid().ToString();
            lastUsedTime = 0f;
            isOnCooldown = false;
        }
        
        public bool CanUse()
        {
            if (isOnCooldown) return false;
            if (Time.time - lastUsedTime < data.cooldown) return false;
            return true;
        }
        
        public void MarkUsed()
        {
            lastUsedTime = Time.time;
            isOnCooldown = true;
        }
        
        public void UpdateCooldown()
        {
            if (isOnCooldown && Time.time - lastUsedTime >= data.cooldown)
            {
                isOnCooldown = false;
            }
        }
    }
}