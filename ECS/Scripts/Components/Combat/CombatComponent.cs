using Core.ECS;

namespace Components.Combat
{
    public class CombatComponent : IComponent
    {
        public float AttackPower { get; set; }
        public float AttackRange { get; set; }
        public float AttackCooldown { get; set; }
        public float CurrentCooldown { get; set; }
        public int TargetEntityId { get; set; } = -1;
        
        public CombatComponent(float attackPower, float attackRange, float attackCooldown)
        {
            AttackPower = attackPower;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
            CurrentCooldown = 0;
        }
        
        public bool CanAttack()
        {
            return CurrentCooldown <= 0;
        }
        
        public void ResetCooldown()
        {
            CurrentCooldown = AttackCooldown;
        }
    }
}