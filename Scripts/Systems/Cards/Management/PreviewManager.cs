using UnityEngine;
using RavenDeckbuilding.Core.Architecture.Singleton;

namespace RavenDeckbuilding.Systems.Cards
{
    /// <summary>
    /// Manager for card preview effects
    /// </summary>
    public class PreviewManager : MonoSingleton<PreviewManager>
    {
        [Header("Preview Settings")]
        [SerializeField] private GameObject damagePreviewPrefab;
        [SerializeField] private GameObject healPreviewPrefab;
        [SerializeField] private Color damageColor = Color.red;
        [SerializeField] private Color healColor = Color.green;
        
        private GameObject _currentPreview;
        
        /// <summary>
        /// Show damage preview at position
        /// </summary>
        public void ShowDamagePreview(Vector3 position, float damage)
        {
            HideAllPreviews();
            
            if (damagePreviewPrefab != null)
            {
                _currentPreview = Instantiate(damagePreviewPrefab, position, Quaternion.identity);
                SetPreviewColor(_currentPreview, damageColor);
                
                // Add damage text if there's a TextMesh component
                var textMesh = _currentPreview.GetComponentInChildren<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = damage.ToString("F0");
                }
            }
        }
        
        /// <summary>
        /// Show heal preview at position
        /// </summary>
        public void ShowHealPreview(Vector3 position, float healAmount)
        {
            HideAllPreviews();
            
            if (healPreviewPrefab != null)
            {
                _currentPreview = Instantiate(healPreviewPrefab, position, Quaternion.identity);
                SetPreviewColor(_currentPreview, healColor);
                
                // Add heal text if there's a TextMesh component
                var textMesh = _currentPreview.GetComponentInChildren<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = $"+{healAmount:F0}";
                }
            }
        }
        
        /// <summary>
        /// Hide all preview effects
        /// </summary>
        public void HideAllPreviews()
        {
            if (_currentPreview != null)
            {
                DestroyImmediate(_currentPreview);
                _currentPreview = null;
            }
        }
        
        private void SetPreviewColor(GameObject preview, Color color)
        {
            if (preview == null) return;
            
            var renderers = preview.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.material != null)
                {
                    renderer.material.color = color;
                }
            }
        }
    }
}