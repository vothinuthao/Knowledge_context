using System.Numerics;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units
{
    public enum AttackType
    {
        None,
        Melee,
        Ranged,
        Magic,
        Special
    }
    public enum DamageType
    {
        None,
        Physical,    // Basic melee damage
        Piercing,    // Arrows, spear thrusts
        Slashing,    // Sword cuts
        Blunt,       // Mace, hammer impacts
        Magical,     // Magical damage
        Fire,        // Fire damage
        Ice,         // Cold damage
        True         // Ignores all armor
    }
    public class DamageInfo
    {
        public IEntity Attacker;
        public IEntity Target;
        public DamageType DamageType;
        public WeaponType WeaponType;
        public float BaseDamage;
        public float FinalDamage;
        public float ArmorPenetration;
        public float DamageReduction;
        public bool IsSecondaryDamage;
        public bool IsCritical;
        public Vector3 HitPosition;
        public Vector3 HitDirection;
    }
    public enum WeaponType
    {
        None,
        Sword,       // Balanced weapon
        Spear,       // Long reach, piercing
        Bow,         // Ranged, piercing
        Mace,        // High damage, stagger
        Hammer,      // Highest damage, slow
        Dagger,      // Fast, low damage
        Staff        // Magical focus
    }
}