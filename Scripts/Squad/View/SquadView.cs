using System.Collections.Generic;
using Squad;
using Troops.Base;
using UnityEngine;

/// <summary>
/// View component for Squad - handles visualization and effects
/// </summary>
public class SquadView : MonoBehaviour
{
    [SerializeField] private Transform bannerAttachPoint;
        
    private SquadBase _squadModel;
    private GameObject _bannerInstance;
    private List<ParticleSystem> _movementEffects = new List<ParticleSystem>();
        
    /// <summary>
    /// Initialize the view with the squad model
    /// </summary>
    public void Initialize(SquadBase squadModel)
    {
        _squadModel = squadModel;
            
        // Create and setup visual elements
        CreateBanner();
        UpdateSquadVisuals();
    }
        
    /// <summary>
    /// Create the squad banner if specified in the model
    /// </summary>
    private void CreateBanner()
    {
        if (_squadModel.SquadBannerPrefab != null && _bannerInstance == null)
        {
            // Determine attachment position
            Transform attachTo = bannerAttachPoint != null ? bannerAttachPoint : transform;
            Vector3 position = bannerAttachPoint != null ? Vector3.zero : new Vector3(0, 2, 0);
                
            // Instantiate banner
            _bannerInstance = Instantiate(_squadModel.SquadBannerPrefab, attachTo);
            _bannerInstance.transform.localPosition = position;
            _bannerInstance.name = $"{_squadModel.SquadName}_Banner";
                
            // Apply squad color if the banner has a renderer
            ApplyColorToBanner();
        }
    }
        
    /// <summary>
    /// Apply squad color to banner materials if applicable
    /// </summary>
    private void ApplyColorToBanner()
    {
        if (_bannerInstance != null)
        {
            // Get all renderers in the banner
            Renderer[] renderers = _bannerInstance.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                // Find materials with "_Color" property
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_Color"))
                    {
                        material.SetColor("_Color", _squadModel.SquadColor);
                    }
                }
            }
        }
    }
        
    /// <summary>
    /// Update squad visuals based on the model
    /// </summary>
    private void UpdateSquadVisuals()
    {
        // Could update formation visualization
        // Could update troop indicators
    }
        
    /// <summary>
    /// Called when the squad starts moving
    /// </summary>
    public void OnSquadMoving()
    {
        // Activate movement particle effects if any
        foreach (var effect in _movementEffects)
        {
            if (effect != null && !effect.isPlaying)
            {
                effect.Play();
            }
        }
            
        // Could animate banner or other visual elements
        if (_bannerInstance != null)
        {
            // Example: slight movement animation for banner
            Animator bannerAnimator = _bannerInstance.GetComponent<Animator>();
            if (bannerAnimator != null)
            {
                bannerAnimator.SetBool("IsMoving", true);
            }
        }
    }
        
    /// <summary>
    /// Called when the squad stops moving
    /// </summary>
    public void OnSquadStopped()
    {
        // Stop movement particle effects
        foreach (var effect in _movementEffects)
        {
            if (effect != null && effect.isPlaying)
            {
                effect.Stop();
            }
        }
            
        // Reset banner animations
        if (_bannerInstance != null)
        {
            Animator bannerAnimator = _bannerInstance.GetComponent<Animator>();
            if (bannerAnimator != null)
            {
                bannerAnimator.SetBool("IsMoving", false);
            }
        }
    }
        
    /// <summary>
    /// Called when a troop is added to the squad
    /// </summary>
    public void OnTroopAdded(TroopBase troop)
    {
        // Could show visual feedback
        // Could update squad formation visualization
    }
        
    /// <summary>
    /// Called when a troop is removed from the squad
    /// </summary>
    public void OnTroopRemoved(TroopBase troop)
    {
        // Could show visual feedback
        // Could update squad formation visualization
    }
        
    /// <summary>
    /// Called when the squad takes damage
    /// </summary>
    public void OnSquadDamaged()
    {
        // Visual effect for squad taking damage
        // Could temporarily change banner color or show impact effects
    }
        
    /// <summary>
    /// Highlight the squad (for selection)
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        // Implementation to visually highlight the squad when selected
        // Could use outline shader, color change, or visual effects
    }
}