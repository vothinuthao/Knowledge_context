using System;
using System.Collections.Generic;
using SteeringBehavior;
using Unity.VisualScripting;
using UnityEngine;

namespace Troop
{
    [Serializable]
    public partial class TroopModel
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
        public TroopState PreviousState { get; set; } = TroopState.Idle;
        public float TemporarySpeedMultiplier { get; set; } = 1.0f;
        public CompositeSteeringBehavior SteeringBehavior { get; private set; }
        public float KnockbackResistance { get; set; } = 0.0f;
        public float StunResistance { get; set; } = 0.0f;
        
        private GameObject _gameObject;
        public GameObject GameObject => _gameObject; 
        public TroopModel(TroopConfigSO config, GameObject gameObject = null)
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
            PreviousState = TroopState.Idle;
            SteeringBehavior = new CompositeSteeringBehavior();
            InitializeBehaviors(config.behaviors);
            _gameObject = gameObject;
        }
    
        public void TakeDamage(float damageAmount)
        {
            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0, CurrentHealth - damageAmount);
            if (previousHealth > 0 && CurrentHealth <= 0)
            {
                PreviousState = CurrentState;
                CurrentState = TroopState.Dead;
            }
        }
    
        public void Heal(float healAmount)
        {
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + healAmount);
        }
        
        public float GetModifiedMoveSpeed()
        {
            return MoveSpeed * TemporarySpeedMultiplier;
        }
        
        private void InitializeBehaviors(List<SteeringBehaviorSO> behaviorConfigs)
        {
            SteeringBehavior.ClearStrategies();
        
            if (behaviorConfigs != null)
            {
                foreach (var config in behaviorConfigs)
                {
                    if (config)
                    {
                        ISteeringBehavior behavior = config.Create();
                        SteeringBehavior.AddStrategy(behavior);
                    }
                }
            }
        }
        public void AddBehavior(ISteeringBehavior behavior)
        {
            if (behavior != null)
            {
                SteeringBehavior.AddStrategy(behavior);
            }
        }
        
        public void RemoveBehavior(string behaviorName)
        {
            List<ISteeringBehavior> behaviors = SteeringBehavior.GetSteeringBehaviors();
            
            foreach (var behavior in behaviors)
            {
                if (behavior.GetName() == behaviorName)
                {
                    SteeringBehavior.RemoveStrategy(behavior);
                    break;
                }
            }
        }
        public T GetComponent<T>() where T : Component
        {
            if (_gameObject != null)
            {
                return _gameObject.GetComponent<T>();
            }
            return null;
        }
        
        public void SetState(TroopState newState)
        {
            if (newState != CurrentState)
            {
                PreviousState = CurrentState;
                CurrentState = newState;
            }
        }
    }
}