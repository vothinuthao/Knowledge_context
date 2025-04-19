using System.Collections.Generic;
using SteeringBehavior;
using UnityEngine;

namespace Troop
{
    public class TroopModel
    {
        public string TroopName { get; private set; }
        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; private set; }
        public float AttackPower { get; private set; }
        public float MoveSpeed { get; private set; }
        public float AttackRange { get; private set; }
        public float AttackSpeed { get; private set; }
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 Acceleration { get; set; }
        public Quaternion Rotation { get; set; }
        public int SquadID { get; set; }
        public Vector2Int SquadPosition { get; set; }
        public TroopState CurrentState { get; set; }
        public CompositeSteeringBehavior SteeringBehavior { get; private set; }
        public TroopModel(TroopConfigSO config)
        {
            TroopName = config.troopName;
            MaxHealth = config.health;
            CurrentHealth = config.health;
            AttackPower = config.attackPower;
            MoveSpeed = config.moveSpeed;
            AttackRange = config.attackRange;
            AttackSpeed = config.attackSpeed;
        
            Velocity = Vector3.zero;
            Acceleration = Vector3.zero;
            CurrentState = TroopState.Idle;
        
            SteeringBehavior = new CompositeSteeringBehavior();
        
            InitializeBehaviors(config.behaviors);
        }
    
        // Nhận damage
        public void TakeDamage(float damageAmount)
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - damageAmount);
        
            if (CurrentHealth <= 0)
            {
                CurrentState = TroopState.Dead;
            }
        }
    
        public void Heal(float healAmount)
        {
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + healAmount);
        }
    
        private void InitializeBehaviors(List<SteeringBehaviorSO> behaviorConfigs)
        {
            SteeringBehavior.ClearStrategies();
        
            foreach (var config in behaviorConfigs)
            {
                ISteeringBehavior behavior = config.Create();
                SteeringBehavior.AddStrategy(behavior);
            }
        }
    }

    public enum TroopState
    {
        Idle,
        Moving,
        Attacking,
        Defending,
        Fleeing,
        Dead
    }
}