using Components.Squad;
using Core.ECS;
using UnityEngine;

namespace Managers
{
    public enum GameState
    {
        INITIALIZING,
        READY,
        PLAYING,
        PAUSED,
        COMBAT,
        VICTORY,
        DEFEAT,
        GAME_OVER,
        ERROR
    }
    
    public enum Faction
    {
        PLAYER,
        ENEMY,
        NEUTRAL,
        ALLY
    }
    
    public enum CommandType
    {
        MOVE,
        ATTACK,
        DEFEND,
        FORMATION_CHANGE,
        RETREAT,
        SPECIAL_ABILITY
    }
    
    public class Command
    {
        public CommandType Type;
        public Entity TargetSquad;
        public Entity TargetEntity;
        public Vector2Int TargetPosition;
        public FormationType Formation;
        public object CustomData;
    }
    
    public class SquadData
    {
        public Entity Entity;
        public SquadConfig Config;
        public Faction Faction;
        public float CreationTime;
        public int Kills;
        public int Deaths;
        public float TotalDamageDealt;
        public float TotalDamageTaken;
    }
    
    public class FactionComponent : IComponent
    {
        public Faction Faction;
    }
}