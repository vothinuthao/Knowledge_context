using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Data
{
    /// <summary>
    /// ScriptableObject configuration for formation spacing
    /// Separated from system logic for better data management
    /// Used by SquadDataSO to define formation behavior
    /// </summary>
    [CreateAssetMenu(fileName = "Formation_Spacing_Config", menuName = "VikingRaven/Formation/Spacing Config")]
    public class FormationSpacingConfig : SerializedScriptableObject
    {
        #region Header and Info
        
        [Title("Formation Spacing Configuration")]
        [InfoBox("Configure spacing between units for the 3 core formation types. " +
                 "Higher values = more space between units. Lower values = tighter formation.", 
                 InfoMessageType.Info)]

        #endregion

        #region Core Formation Settings

        [FoldoutGroup("Formation Spacing")]
        [Tooltip("Spacing for Normal formation (3x3 grid)")]
        [Range(1.5f, 4f), SuffixLabel("units")]
        [SerializeField] private float _normalSpacing = 2.5f;
        
        [FoldoutGroup("Formation Spacing")]
        [Tooltip("Spacing for Phalanx formation (tight combat grid)")]
        [Range(1f, 3f), SuffixLabel("units")]
        [SerializeField] private float _phalanxSpacing = 1.8f;
        
        [FoldoutGroup("Formation Spacing")]
        [Tooltip("Spacing for Testudo formation (very tight defensive grid)")]
        [Range(0.8f, 2f), SuffixLabel("units")]
        [SerializeField] private float _testudoSpacing = 1.2f;

        #endregion

        #region Advanced Settings

        [FoldoutGroup("Advanced Settings")]
        [Tooltip("Minimum spacing to prevent unit overlap")]
        [Range(0.5f, 2f), SuffixLabel("units")]
        [SerializeField] private float _minimumSpacing = 1.0f;
        
        [FoldoutGroup("Advanced Settings")]
        [Tooltip("Position tolerance for units to be considered 'in formation'")]
        [Range(0.1f, 1f), SuffixLabel("units")]
        [SerializeField] private float _positionTolerance = 0.3f;

        #endregion

        #region Visual Settings

        [FoldoutGroup("Visual Settings")]
        [Tooltip("Enable debug visualization for formations")]
        [SerializeField] private bool _enableDebugVisualization = true;
        
        [FoldoutGroup("Visual Settings")]
        [Tooltip("Color for Normal formation debug")]
        [ShowIf("_enableDebugVisualization")]
        [SerializeField] private Color _normalFormationColor = Color.green;
        
        [FoldoutGroup("Visual Settings")]
        [Tooltip("Color for Phalanx formation debug")]
        [ShowIf("_enableDebugVisualization")]
        [SerializeField] private Color _phalanxFormationColor = Color.red;
        
        [FoldoutGroup("Visual Settings")]
        [Tooltip("Color for Testudo formation debug")]
        [ShowIf("_enableDebugVisualization")]
        [SerializeField] private Color _testudoFormationColor = Color.blue;

        #endregion

        #region Properties

        public float NormalSpacing => Mathf.Max(_normalSpacing, _minimumSpacing);
        public float PhalanxSpacing => Mathf.Max(_phalanxSpacing, _minimumSpacing);
        public float TestudoSpacing => Mathf.Max(_testudoSpacing, _minimumSpacing);
        public float MinimumSpacing => _minimumSpacing;
        public float PositionTolerance => _positionTolerance;
        public bool EnableDebugVisualization => _enableDebugVisualization;

        #endregion

        #region Public Methods

        /// <summary>
        /// Get spacing for specific formation type with validation
        /// </summary>
        public float GetSpacing(FormationType formationType)
        {
            float spacing = formationType switch
            {
                FormationType.Normal => NormalSpacing,
                FormationType.Phalanx => PhalanxSpacing,
                FormationType.Testudo => TestudoSpacing,
                _ => NormalSpacing // Default fallback
            };
            
            return Mathf.Max(spacing, _minimumSpacing);
        }
        
        /// <summary>
        /// Get debug color for formation type
        /// </summary>
        public Color GetFormationDebugColor(FormationType formationType)
        {
            return formationType switch
            {
                FormationType.Normal => _normalFormationColor,
                FormationType.Phalanx => _phalanxFormationColor,
                FormationType.Testudo => _testudoFormationColor,
                _ => Color.white
            };
        }

        #endregion

        #region Debug Tools

        [Button("Reset to Optimal Values"), PropertySpace(10)]
        [FoldoutGroup("Debug Tools")]
        private void ResetToOptimalValues()
        {
            _normalSpacing = 2.5f;    // Good for 3x3 grid visibility
            _phalanxSpacing = 1.8f;   // Tight for combat effectiveness
            _testudoSpacing = 1.2f;   // Very tight for maximum defense
            _minimumSpacing = 1.0f;   // Prevent overlap
            _positionTolerance = 0.3f; // Reasonable tolerance
            
            Debug.Log("FormationSpacingConfig: Reset to optimal values for 3x3 formations");
        }
        
        [Button("Validate Configuration")]
        [FoldoutGroup("Debug Tools")]
        private void ValidateConfiguration()
        {
            bool hasIssues = false;
            
            if (_testudoSpacing >= _phalanxSpacing)
            {
                Debug.LogWarning("FormationSpacingConfig: Testudo should be tighter than Phalanx");
                hasIssues = true;
            }
            
            if (_phalanxSpacing >= _normalSpacing)
            {
                Debug.LogWarning("FormationSpacingConfig: Phalanx should be tighter than Normal");
                hasIssues = true;
            }
            
            if (_positionTolerance > _minimumSpacing)
            {
                Debug.LogWarning("FormationSpacingConfig: Position tolerance should be smaller than minimum spacing");
                hasIssues = true;
            }
            
            if (!hasIssues)
            {
                Debug.Log("FormationSpacingConfig: Configuration is valid");
            }
        }
        
        [Button("Test Formation Spacing")]
        [FoldoutGroup("Debug Tools")]
        private void TestFormationSpacing()
        {
            Debug.Log("=== Formation Spacing Test ===");
            Debug.Log($"Normal Formation (3x3): {GetSpacing(FormationType.Normal)} units spacing");
            Debug.Log($"Phalanx Formation: {GetSpacing(FormationType.Phalanx)} units spacing");
            Debug.Log($"Testudo Formation: {GetSpacing(FormationType.Testudo)} units spacing");
            Debug.Log($"Position Tolerance: {PositionTolerance} units");
        }

        #endregion
    }
}