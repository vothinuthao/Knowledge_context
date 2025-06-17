using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Team
{
    /// <summary>
    /// Team component for unit identification and team-based logic
    /// Integrates with CombatAI for enemy detection
    /// </summary>
    public class TeamComponent : BaseComponent
    {
        [Header("Team Configuration")]
        [Tooltip("Team identifier for this unit")]
        [SerializeField] private TeamType _teamType = TeamType.Player;
        
        [Tooltip("Team color for visual identification")]
        [SerializeField] private Color _teamColor = Color.blue;
        
        public TeamType Team => _teamType;
        public Color TeamColor => _teamColor;
        
        /// <summary>
        /// Check if another team is enemy to this team
        /// </summary>
        public bool IsEnemy(TeamType otherTeam)
        {
            return _teamType != otherTeam && otherTeam != TeamType.Neutral;
        }
        
        public bool IsEnemy(TeamComponent otherTeam)
        {
            if (otherTeam == null) return false;
            return IsEnemy(otherTeam.Team);
        }
        
        public void SetTeam(TeamType newTeam)
        {
            _teamType = newTeam;
            UpdateVisualTeamIndicator();
        }
        
        /// <summary>
        /// Update visual indicators based on team
        /// </summary>
        private void UpdateVisualTeamIndicator()
        {
            // Find renderer and update material color
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                // Create material instance to avoid shared material modification
                var material = renderer.material;
                material.color = _teamColor;
            }
        }
        
        public override void Initialize()
        {
            base.Initialize();
            UpdateVisualTeamIndicator();
        }
    }
    
    /// <summary>
    /// Team types for different factions
    /// </summary>
    public enum TeamType
    {
        Player = 0,
        Enemy = 1,
        Ally = 2,
        Neutral = 3
    }
}