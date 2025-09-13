using UnityEngine;

namespace RavenDeckbuilding.Core
{
    /// <summary>
    /// Represents a player in the game
    /// </summary>
    public class Player : MonoBehaviour
    {
        [Header("Player Settings")]
        [SerializeField] private int playerId;
        [SerializeField] private string playerName;
        [SerializeField] private int health = 100;
        [SerializeField] private int maxMana = 100;
        [SerializeField] private int currentMana = 100;
        [SerializeField] private PlayerTeam playerTeam = PlayerTeam.Player;
        
        public int PlayerId => playerId;
        public string PlayerName => playerName;
        public int Health { get; private set; }
        public int CurrentMana { get; private set; }
        public int MaxMana => maxMana;
        public PlayerTeam PlayerTeam => playerTeam;
        
        void Awake()
        {
            Health = health;
            CurrentMana = currentMana;
        }
        
        public void TakeDamage(int damage)
        {
            Health = Mathf.Max(0, Health - damage);
        }
        
        public void Heal(int amount)
        {
            Health = Mathf.Min(health, Health + amount);
        }
        
        public bool ConsumeMana(int amount)
        {
            if (CurrentMana >= amount)
            {
                CurrentMana -= amount;
                return true;
            }
            return false;
        }
        
        public void RestoreMana(int amount)
        {
            CurrentMana = Mathf.Min(maxMana, CurrentMana + amount);
        }
        
        public void SetMana(int amount)
        {
            CurrentMana = Mathf.Clamp(amount, 0, maxMana);
        }
        
        public bool IsAlive => Health > 0;
    }
    
    public enum PlayerTeam
    {
        Player,
        Enemy,
        Neutral
    }
}