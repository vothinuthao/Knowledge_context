// Scripts/Core/GameDefineData.cs
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Static class containing all game constants and reusable definitions
    /// </summary>
    public static class GameDefineData
    {
        public static class Tags
        {
            public const string Troop = "Troop";
            public const string Enemy = "Enemy";
            public const string Obstacle = "Obstacle";
            public const string FormationPosition = "FormationPosition";
            public const string Tile = "Tile";
            public const string Squad = "Squad";
        }
        
        public static class Layers
        {
            public const int Troops = 8;
            public const int Enemies = 9;
            public const int Obstacles = 10;
            public const int Ground = 11;
            public const int Tiles = 12;
            
            public static readonly int TroopsMask = 1 << Troops;
            public static readonly int EnemiesMask = 1 << Enemies;
            public static readonly int ObstaclesMask = 1 << Obstacles;
            public static readonly int GroundMask = 1 << Ground;
            public static readonly int TilesMask = 1 << Tiles;
            
            public static readonly int PerceptionMask = TroopsMask | EnemiesMask | ObstaclesMask;
            public static readonly int MovementMask = ObstaclesMask | GroundMask;
        }
        
        // Formation settings
        public static class Formation
        {
            public enum FormationType
            {
                Square,
                Line,
                Column,
                Vshape,
                Circle
            }
            
            // Default spacing for each troop type
            public static readonly float DefaultWarriorSpacing = 1.0f;
            public static readonly float DefaultArcherSpacing = 1.5f;
            public static readonly float DefaultPikerSpacing = 1.2f;
            
            // Maximum troops in a squad
            public const int MaxTroopsInSquad = 9;
            public static readonly Vector3[] SquareFormationPositions = new Vector3[]
            {
                new Vector3(-1, 0, -1),  // Back Left
                new Vector3(0, 0, -1),   // Back Center
                new Vector3(1, 0, -1),   // Back Right
                new Vector3(-1, 0, 0),   // Middle Left
                new Vector3(0, 0, 0),    // Middle Center
                new Vector3(1, 0, 0),    // Middle Right
                new Vector3(-1, 0, 1),   // Front Left
                new Vector3(0, 0, 1),    // Front Center
                new Vector3(1, 0, 1)     // Front Right
            };
            
        }
        
        // Tile settings
        public static class Tiles
        {
            public const float TileSize = 2.0f;
            public const float TileHeight = 0.2f;
            public static readonly Color DefaultTileColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            public static readonly Color HighlightedTileColor = new Color(0.4f, 0.8f, 0.4f, 0.7f);
            public static readonly Color SelectedTileColor = new Color(0.2f, 0.6f, 1.0f, 0.7f);
        }
        
        // Squad types
        public enum SquadType
        {
            Warrior,
            Archer,
            Piker
        }
    }
}