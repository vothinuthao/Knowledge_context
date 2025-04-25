using Components.Squad;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSquadConfig", menuName = "Viking Raven/Squad Config")]
public class SquadConfig : ScriptableObject
{
    [Header("Squad Properties")]
    public int MaxTroops = 9;
    public float FormationSpacing = 1.5f;
    public FormationType DefaultFormation = FormationType.BASIC;
        
    [Header("Movement")]
    public float MovementSpeed = 3.0f;
    public float RotationSpeed = 5.0f;
        
    [Header("Combat")]
    public float CombatRange = 1.5f;
    public float AttackRate = 1.0f;
    public float BaseDamage = 10.0f;
    public float BaseHealth = 100.0f;
        
    [Header("Morale")]
    public float MoraleBase = 1.0f;
    public float MoraleGainRate = 0.1f;
    public float MoraleLossRate = 0.2f;
        
    [Header("Formation Bonuses")]
    public float PhalanxDefenseBonus = 1.5f;
    public float TestudoRangedDefenseBonus = 2.0f;
    public float WedgeAttackBonus = 1.3f;
}