using System.Collections.Generic;
using Components.Squad;
using Data;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFormationConfig", menuName = "Viking Raven/Formation Config")]
public class FormationConfig : ScriptableObject
{
    [Header("Formation Type")]
    public FormationType Type;
    public string FormationName;
    public string Description;
        
    [Header("Layout")]
    public int MaxRows = 3;
    public int MaxColumns = 3;
    public float Spacing = 1.5f;
        
    [Header("Bonuses")]
    public float AttackBonus = 1.0f;
    public float DefenseBonus = 1.0f;
    public float SpeedMultiplier = 1.0f;
    public float MoraleBonus = 1.0f;
        
    [Header("Requirements")]
    public int MinTroops = 1;
    public int MaxTroops = 9;
    public TroopType[] AllowedTroopTypes;
        
    [Header("Visual")]
    public Sprite FormationIcon;
    public Color FormationColor = Color.white;
        
    [Header("Custom Formation Pattern")]
    [TextArea(3, 10)]
    public string FormationPattern = @"
X X X
X X X
X X X";
        
    /// <summary>
    /// Parse formation pattern to get positions
    /// </summary>
    public Vector2Int[] GetFormationPositions()
    {
        var positions = new List<Vector2Int>();
        string[] rows = FormationPattern.Trim().Split('\n');
            
        for (int y = 0; y < rows.Length; y++)
        {
            string[] cells = rows[y].Trim().Split(' ');
            for (int x = 0; x < cells.Length; x++)
            {
                if (cells[x].Trim().ToUpper() == "X")
                {
                    positions.Add(new Vector2Int(x, y));
                }
            }
        }
            
        return positions.ToArray();
    }
}