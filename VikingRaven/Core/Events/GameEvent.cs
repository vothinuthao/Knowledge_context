using System;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.Events
{
    public abstract class GameEvent
    {
        public DateTime Timestamp { get; private set; }
        
        protected GameEvent()
        {
            Timestamp = DateTime.Now;
        }
    }
     public class UnitMovedEvent : GameEvent
    {
        public IEntity Unit { get; private set; }
        public UnityEngine.Vector3 OldPosition { get; private set; }
        public UnityEngine.Vector3 NewPosition { get; private set; }
        
        public UnitMovedEvent(IEntity unit, UnityEngine.Vector3 oldPosition, UnityEngine.Vector3 newPosition)
        {
            Unit = unit;
            OldPosition = oldPosition;
            NewPosition = newPosition;
        }
    }
    
    // Combat events
    public class DamageEvent : GameEvent
    {
        public IEntity Target { get; private set; }
        public IEntity Source { get; private set; }
        public float Amount { get; private set; }
        public DamageType DamageType { get; private set; }
        
        public DamageEvent(IEntity target, IEntity source, float amount, DamageType damageType)
        {
            Target = target;
            Source = source;
            Amount = amount;
            DamageType = damageType;
        }
    }
    
    public class DeathEvent : GameEvent
    {
        public IEntity Unit { get; private set; }
        public IEntity Killer { get; private set; }
        
        public DeathEvent(IEntity unit, IEntity killer)
        {
            Unit = unit;
            Killer = killer;
        }
    }
    
    // State events
    public class UnitStateChangedEvent : GameEvent
    {
        public IEntity Unit { get; private set; }
        public Type OldState { get; private set; }
        public Type NewState { get; private set; }
        
        public UnitStateChangedEvent(IEntity unit, Type oldState, Type newState)
        {
            Unit = unit;
            OldState = oldState;
            NewState = newState;
        }
    }
    
    // Formation events
    public class FormationChangedEvent : GameEvent
    {
        public int SquadId { get; private set; }
        public FormationType OldFormation { get; private set; }
        public FormationType NewFormation { get; private set; }
        
        public FormationChangedEvent(int squadId, FormationType oldFormation, FormationType newFormation)
        {
            SquadId = squadId;
            OldFormation = oldFormation;
            NewFormation = newFormation;
        }
    }
    
    // Detection events
    public class EnemyDetectedEvent : GameEvent
    {
        public IEntity Observer { get; private set; }
        public IEntity Enemy { get; private set; }
        
        public EnemyDetectedEvent(IEntity observer, IEntity enemy)
        {
            Observer = observer;
            Enemy = enemy;
        }
    }
    
    public class EnemyLostEvent : GameEvent
    {
        public IEntity Observer { get; private set; }
        public IEntity Enemy { get; private set; }
        
        public EnemyLostEvent(IEntity observer, IEntity enemy)
        {
            Observer = observer;
            Enemy = enemy;
        }
    }
    
    // Squad events
    public class SquadCreatedEvent : GameEvent
    {
        public int SquadId { get; private set; }
        public System.Collections.Generic.List<IEntity> Units { get; private set; }
        
        public SquadCreatedEvent(int squadId, System.Collections.Generic.List<IEntity> units)
        {
            SquadId = squadId;
            Units = units;
        }
    }
    
    // Command events
    public class MoveCommandEvent : GameEvent
    {
        public int SquadId { get; private set; }
        public UnityEngine.Vector3 Destination { get; private set; }
        
        public MoveCommandEvent(int squadId, UnityEngine.Vector3 destination)
        {
            SquadId = squadId;
            Destination = destination;
        }
    }
    
    public class AttackCommandEvent : GameEvent
    {
        public int SquadId { get; private set; }
        public IEntity Target { get; private set; }
        
        public AttackCommandEvent(int squadId, IEntity target)
        {
            SquadId = squadId;
            Target = target;
        }
    }
    
    public enum DamageType
    {
        Physical,
        Knockback,
        Special
    }
}