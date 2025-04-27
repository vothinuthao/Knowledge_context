using Factories;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTroopConfig", menuName = "Viking Raven/Troop Config")]
public class TroopConfig : ScriptableObject
{
    [Header("Basic Info")]
    public string TroopName = "Infantry";
    public TroopType Type = TroopType.Warrior;
    public GameObject Prefab;
        
    [Header("Stats")]
    public float Health = 100f;
    public float AttackPower = 10f;
    public float Defense = 5f;
    public float MoveSpeed = 3f;
        
    [Header("Behavior Weights")]
    public float FormationKeepWeight = 1.0f;
    public float SeekWeight = 0.8f;
    public float SeparationWeight = 0.6f;
        
    [Header("Combat")]
    public float AttackRange = 1.5f;
    public float AttackCooldown = 1.0f;
    public bool CanBlock = true;
    public float BlockChance = 0.3f;
        
    [Header("Special Abilities")]
    public bool HasSpecialAbility = false;
    public string AbilityName = "";
    public float AbilityCooldown = 10f;
}