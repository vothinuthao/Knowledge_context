#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Steering.Config;

namespace Editor
{
    [CustomEditor(typeof(SteeringBehaviorConfig))]
    public class SteeringBehaviorConfigEditor : UnityEditor.Editor
    {
        private bool showBasicBehaviors = true;
        private bool showFormationBehaviors = false;
        private bool showSpecialBehaviors = false;
        
        public override void OnInspectorGUI()
        {
            SteeringBehaviorConfig config = (SteeringBehaviorConfig)target;
            
            EditorGUILayout.LabelField("Steering Behavior Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Basic behaviors section
            showBasicBehaviors = EditorGUILayout.Foldout(showBasicBehaviors, "Basic Behaviors", true);
            if (showBasicBehaviors)
            {
                EditorGUI.indentLevel++;
                
                // Seek
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                config.SeekSettings.Enabled = EditorGUILayout.Toggle("Seek Enabled", config.SeekSettings.Enabled);
                if (config.SeekSettings.Enabled)
                {
                    config.SeekSettings.Weight = EditorGUILayout.Slider("Seek Weight", config.SeekSettings.Weight, 0.1f, 5.0f);
                }
                EditorGUILayout.EndVertical();
                
                // Flee
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                config.FleeSettings.Enabled = EditorGUILayout.Toggle("Flee Enabled", config.FleeSettings.Enabled);
                if (config.FleeSettings.Enabled)
                {
                    config.FleeSettings.Weight = EditorGUILayout.Slider("Flee Weight", config.FleeSettings.Weight, 0.1f, 5.0f);
                    config.FleeSettings.Parameter1 = EditorGUILayout.FloatField("Panic Distance", config.FleeSettings.Parameter1);
                }
                EditorGUILayout.EndVertical();
                
                // Arrival
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                config.ArrivalSettings.Enabled = EditorGUILayout.Toggle("Arrival Enabled", config.ArrivalSettings.Enabled);
                if (config.ArrivalSettings.Enabled)
                {
                    config.ArrivalSettings.Weight = EditorGUILayout.Slider("Arrival Weight", config.ArrivalSettings.Weight, 0.1f, 5.0f);
                    config.ArrivalSettings.Parameter1 = EditorGUILayout.FloatField("Slowing Distance", config.ArrivalSettings.Parameter1);
                }
                EditorGUILayout.EndVertical();
                
                // Separation
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                config.SeparationSettings.Enabled = EditorGUILayout.Toggle("Separation Enabled", config.SeparationSettings.Enabled);
                if (config.SeparationSettings.Enabled)
                {
                    config.SeparationSettings.Weight = EditorGUILayout.Slider("Separation Weight", config.SeparationSettings.Weight, 0.1f, 5.0f);
                    config.SeparationSettings.Parameter1 = EditorGUILayout.FloatField("Separation Radius", config.SeparationSettings.Parameter1);
                }
                EditorGUILayout.EndVertical();
                
                // Cohesion
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                config.CohesionSettings.Enabled = EditorGUILayout.Toggle("Cohesion Enabled", config.CohesionSettings.Enabled);
                if (config.CohesionSettings.Enabled)
                {
                    config.CohesionSettings.Weight = EditorGUILayout.Slider("Cohesion Weight", config.CohesionSettings.Weight, 0.1f, 5.0f);
                    config.CohesionSettings.Parameter1 = EditorGUILayout.FloatField("Cohesion Radius", config.CohesionSettings.Parameter1);
                }
                EditorGUILayout.EndVertical();
                
                // Alignment
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                config.AlignmentSettings.Enabled = EditorGUILayout.Toggle("Alignment Enabled", config.AlignmentSettings.Enabled);
                if (config.AlignmentSettings.Enabled)
                {
                    config.AlignmentSettings.Weight = EditorGUILayout.Slider("Alignment Weight", config.AlignmentSettings.Weight, 0.1f, 5.0f);
                    config.AlignmentSettings.Parameter1 = EditorGUILayout.FloatField("Alignment Radius", config.AlignmentSettings.Parameter1);
                }
                EditorGUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
            
            // Formation behaviors section
            showFormationBehaviors = EditorGUILayout.Foldout(showFormationBehaviors, "Formation Behaviors", true);
            if (showFormationBehaviors)
            {
                EditorGUI.indentLevel++;
                
                // Phalanx
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                config.PhalanxSettingsBehavior.Enabled = EditorGUILayout.Toggle("Phalanx Enabled", config.PhalanxSettingsBehavior.Enabled);
                if (config.PhalanxSettingsBehavior.Enabled)
                {
                    config.PhalanxSettingsBehavior.Weight = EditorGUILayout.Slider("Weight", config.PhalanxSettingsBehavior.Weight, 0.1f, 5.0f);
                    config.PhalanxSettingsBehavior.FormationSpacing = EditorGUILayout.FloatField("Spacing", config.PhalanxSettingsBehavior.FormationSpacing);
                    config.PhalanxSettingsBehavior.MovementSpeedMultiplier = EditorGUILayout.Slider("Speed Multiplier", config.PhalanxSettingsBehavior.MovementSpeedMultiplier, 0.1f, 1.0f);
                    config.PhalanxSettingsBehavior.MaxRowsInFormation = EditorGUILayout.IntField("Max Rows", config.PhalanxSettingsBehavior.MaxRowsInFormation);
                }
                EditorGUILayout.EndVertical();
                
                // Testudo
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                config.TestudoSettingsBehavior.Enabled = EditorGUILayout.Toggle("Testudo Enabled", config.TestudoSettingsBehavior.Enabled);
                if (config.TestudoSettingsBehavior.Enabled)
                {
                    config.TestudoSettingsBehavior.Weight = EditorGUILayout.Slider("Weight", config.TestudoSettingsBehavior.Weight, 0.1f, 5.0f);
                    config.TestudoSettingsBehavior.FormationSpacing = EditorGUILayout.FloatField("Spacing", config.TestudoSettingsBehavior.FormationSpacing);
                    config.TestudoSettingsBehavior.MovementSpeedMultiplier = EditorGUILayout.Slider("Speed Multiplier", config.TestudoSettingsBehavior.MovementSpeedMultiplier, 0.1f, 1.0f);
                    config.TestudoSettingsBehavior.KnockbackResistanceBonus = EditorGUILayout.Slider("Knockback Resistance", config.TestudoSettingsBehavior.KnockbackResistanceBonus, 0.0f, 1.0f);
                    config.TestudoSettingsBehavior.RangedDefenseBonus = EditorGUILayout.Slider("Ranged Defense", config.TestudoSettingsBehavior.RangedDefenseBonus, 0.0f, 1.0f);
                }
                EditorGUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
            
            // Advanced behaviors section
            showSpecialBehaviors = EditorGUILayout.Foldout(showSpecialBehaviors, "Special Behaviors", true);
            if (showSpecialBehaviors)
            {
                EditorGUI.indentLevel++;
                
                // Jump Attack
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                config.JumpAttackSettingsBehavior.Enabled = EditorGUILayout.Toggle("Jump Attack Enabled", config.JumpAttackSettingsBehavior.Enabled);
                if (config.JumpAttackSettingsBehavior.Enabled)
                {
                    config.JumpAttackSettingsBehavior.Weight = EditorGUILayout.Slider("Weight", config.JumpAttackSettingsBehavior.Weight, 0.1f, 5.0f);
                    config.JumpAttackSettingsBehavior.JumpRange = EditorGUILayout.FloatField("Jump Range", config.JumpAttackSettingsBehavior.JumpRange);
                    config.JumpAttackSettingsBehavior.JumpSpeed = EditorGUILayout.FloatField("Jump Speed", config.JumpAttackSettingsBehavior.JumpSpeed);
                    config.JumpAttackSettingsBehavior.JumpHeight = EditorGUILayout.FloatField("Jump Height", config.JumpAttackSettingsBehavior.JumpHeight);
                    config.JumpAttackSettingsBehavior.DamageMultiplier = EditorGUILayout.FloatField("Damage Multiplier", config.JumpAttackSettingsBehavior.DamageMultiplier);
                    config.JumpAttackSettingsBehavior.Cooldown = EditorGUILayout.FloatField("Cooldown", config.JumpAttackSettingsBehavior.Cooldown);
                }
                EditorGUILayout.EndVertical();
                
                // Ambush Move
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                config.AmbushMoveSettingsBehavior.Enabled = EditorGUILayout.Toggle("Ambush Move Enabled", config.AmbushMoveSettingsBehavior.Enabled);
                if (config.AmbushMoveSettingsBehavior.Enabled)
                {
                    config.AmbushMoveSettingsBehavior.Weight = EditorGUILayout.Slider("Weight", config.AmbushMoveSettingsBehavior.Weight, 0.1f, 5.0f);
                    config.AmbushMoveSettingsBehavior.MoveSpeedMultiplier = EditorGUILayout.Slider("Speed Multiplier", config.AmbushMoveSettingsBehavior.MoveSpeedMultiplier, 0.1f, 1.0f);
                    config.AmbushMoveSettingsBehavior.DetectionRadiusMultiplier = EditorGUILayout.Slider("Detection Radius", config.AmbushMoveSettingsBehavior.DetectionRadiusMultiplier, 0.1f, 1.0f);
                }
                EditorGUILayout.EndVertical();
                
                // Charge
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                config.ChargeSettingsBehavior.Enabled = EditorGUILayout.Toggle("Charge Enabled", config.ChargeSettingsBehavior.Enabled);
                if (config.ChargeSettingsBehavior.Enabled)
                {
                    config.ChargeSettingsBehavior.Weight = EditorGUILayout.Slider("Weight", config.ChargeSettingsBehavior.Weight, 0.1f, 5.0f);
                    config.ChargeSettingsBehavior.ChargeDistance = EditorGUILayout.FloatField("Charge Distance", config.ChargeSettingsBehavior.ChargeDistance);
                    config.ChargeSettingsBehavior.ChargeSpeedMultiplier = EditorGUILayout.FloatField("Speed Multiplier", config.ChargeSettingsBehavior.ChargeSpeedMultiplier);
                    config.ChargeSettingsBehavior.ChargeDamageMultiplier = EditorGUILayout.FloatField("Damage Multiplier", config.ChargeSettingsBehavior.ChargeDamageMultiplier);
                    config.ChargeSettingsBehavior.ChargePreparationTime = EditorGUILayout.FloatField("Preparation Time", config.ChargeSettingsBehavior.ChargePreparationTime);
                    config.ChargeSettingsBehavior.ChargeCooldown = EditorGUILayout.FloatField("Cooldown", config.ChargeSettingsBehavior.ChargeCooldown);
                }
                EditorGUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            if (GUILayout.Button("Apply"))
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif