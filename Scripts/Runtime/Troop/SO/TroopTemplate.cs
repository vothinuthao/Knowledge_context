using System.Collections.Generic;
using SteeringBehavior;
using UnityEngine;

namespace Troop
{
    [CreateAssetMenu(fileName = "Troop Template", menuName = "Wiking Raven/Behavior Templates/Troop Template")]
    public class TroopTemplate : ScriptableObject
    {
        public string templateName;
        [TextArea(1, 3)]
        public string description;
        
        [Header("Base Stats")]
        public float baseHealth = 100;
        public float baseAttackPower = 10;
        public float baseMoveSpeed = 3;
        public float baseAttackRange = 1.5f;
        public float baseAttackSpeed = 1;
        
        [Header("Behavior Categories")]
        public bool includeMovementBehaviors = true;
        public bool includeFormationBehaviors = true;
        public bool includeCombatBehaviors = true;
        public bool includeSpecialBehaviors = false;
        
        [Header("Additional Behaviors")]
        public List<string> additionalBehaviorNames = new List<string>();
        
        [Header("References")]
        public BehaviorTemplateSet templateSet;
        
        // Tạo một TroopConfigSO dựa trên template này
        public TroopConfigSO CreateTroopConfig(string troopName)
        {
            TroopConfigSO config = ScriptableObject.CreateInstance<TroopConfigSO>();
            config.troopName = troopName;
            
            // Set base stats
            config.health = baseHealth;
            config.attackPower = baseAttackPower;
            config.moveSpeed = baseMoveSpeed;
            config.attackRange = baseAttackRange;
            config.attackSpeed = baseAttackSpeed;
            
            // Khởi tạo danh sách behavior
            config.behaviors = new List<SteeringBehaviorSO>();
            
            // Luôn thêm các behavior cần thiết
            if (templateSet != null)
            {
                // Thêm các behavior cần thiết
                config.behaviors.AddRange(templateSet.GetEssentialBehaviors());
                
                // Thêm các behavior theo category
                if (includeMovementBehaviors)
                {
                    config.behaviors.AddRange(templateSet.GetBehaviorsByCategory(BehaviorCategory.Movement));
                }
                
                if (includeFormationBehaviors)
                {
                    config.behaviors.AddRange(templateSet.GetBehaviorsByCategory(BehaviorCategory.Formation));
                }
                
                if (includeCombatBehaviors)
                {
                    config.behaviors.AddRange(templateSet.GetBehaviorsByCategory(BehaviorCategory.Combat));
                }
                
                if (includeSpecialBehaviors)
                {
                    config.behaviors.AddRange(templateSet.GetBehaviorsByCategory(BehaviorCategory.Special));
                }
                
                // Thêm các behavior bổ sung
                foreach (string behaviorName in additionalBehaviorNames)
                {
                    BehaviorTemplate template = templateSet.GetTemplateByName(behaviorName);
                    if (template != null && !config.behaviors.Contains(template.behaviorSO))
                    {
                        config.behaviors.Add(template.behaviorSO);
                    }
                }
            }
            
            return config;
        }
    }
}