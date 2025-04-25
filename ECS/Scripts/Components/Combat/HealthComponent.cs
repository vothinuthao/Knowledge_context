using Core.ECS;

namespace Components.Combat
{
    public class HealthComponent : IComponent
    {
        public float MaxHealth { get; set; }
        public float CurrentHealth { get; set; }
        public bool IsDead => CurrentHealth <= 0;
        
        public HealthComponent(float maxHealth)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }
        
        public void TakeDamage(float amount)
        {
            CurrentHealth -= amount;
            if (CurrentHealth < 0)
                CurrentHealth = 0;
        }
        
        public void Heal(float amount)
        {
            CurrentHealth += amount;
            if (CurrentHealth > MaxHealth)
                CurrentHealth = MaxHealth;
        }
    }
}