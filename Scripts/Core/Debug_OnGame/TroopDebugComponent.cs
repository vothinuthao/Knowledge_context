using System.Collections.Generic;
using Troop;
using UnityEngine;

namespace Core.Debug_OnGame
{
    public class TroopDebugComponent : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool showBehaviorInfo = true;
        [SerializeField] private bool showStateInfo = true;
        [SerializeField] private bool showMovementInfo = true;
        [SerializeField] private bool showForces = true;
        [SerializeField] private float forceLineScale = 1f;
    
        private TroopController _troopController;
        private Dictionary<string, Vector3> _behaviorForces = new Dictionary<string, Vector3>();
        private Vector3 _combinedForce = Vector3.zero;
    
        private void Awake()
        {
            _troopController = GetComponent<TroopController>();
            if (_troopController == null)
            {
                Debug.LogError("TroopDebugComponent requires a TroopController component", this);
                enabled = false;
            }
        }
    
        private void Update()
        {
            if (showForces)
            {
                UpdateBehaviorForces();
            }
        }
    
        private void OnGUI()
        {
            if (_troopController == null || Camera.main == null) return;
        
            Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            // Convert to GUI space (Y is inverted)
            screenPos.y = Screen.height - screenPos.y;
        
            // Check if troop is on screen
            if (screenPos.x < 0 || screenPos.x > Screen.width || 
                screenPos.y < 0 || screenPos.y > Screen.height)
                return;
        
            // Draw debug info near the troop
            float yOffset = 0;
            if (showStateInfo)
            {
                DisplayStateInfo(screenPos.x, screenPos.y + yOffset);
                yOffset += 70; // Adjust based on content height
            }
        
            if (showBehaviorInfo)
            {
                DisplayBehaviorInfo(screenPos.x, screenPos.y + yOffset);
                yOffset += 150; // Will grow based on number of behaviors
            }
        
            if (showMovementInfo)
            {
                DisplayMovementInfo(screenPos.x, screenPos.y + yOffset);
            }
        }
    
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || _troopController == null || !showForces) return;
        
            // Draw individual behavior forces
            foreach (var kvp in _behaviorForces)
            {
                if (kvp.Value.magnitude > 0.01f)
                {
                    Gizmos.color = GetColorForBehavior(kvp.Key);
                    Gizmos.DrawLine(transform.position, transform.position + kvp.Value * forceLineScale);
                    Gizmos.DrawSphere(transform.position + kvp.Value * forceLineScale, 0.1f);
                }
            }
        
            // Draw combined force
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, transform.position + _combinedForce * forceLineScale);
            Gizmos.DrawSphere(transform.position + _combinedForce * forceLineScale, 0.15f);
        
            // Draw velocity and target
            if (_troopController.GetModel() != null)
            {
                // Draw velocity
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, 
                    transform.position + _troopController.GetModel().Velocity * forceLineScale);
            
                // Draw target position
                Gizmos.color = Color.yellow;
                Vector3 targetPos = _troopController.GetTargetPosition();
                if (targetPos != Vector3.zero)
                {
                    Gizmos.DrawLine(transform.position, targetPos);
                    Gizmos.DrawSphere(targetPos, 0.2f);
                }
            
                // Draw squad position if in a squad
                var squadExtensions = TroopControllerSquadExtensions.Instance;
                if (squadExtensions != null)
                {
                    var squad = squadExtensions.GetSquad(_troopController);
                    if (squad != null)
                    {
                        var squadPos = squadExtensions.GetSquadPosition(_troopController);
                        Vector3 squadWorldPos = squad.GetPositionForTroop(squad, squadPos.x, squadPos.y);
                    
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(transform.position, squadWorldPos);
                        Gizmos.DrawCube(squadWorldPos, Vector3.one * 0.3f);
                    }
                }
            }
        }
    
        private void UpdateBehaviorForces()
        {
            _behaviorForces.Clear();
            _combinedForce = Vector3.zero;
        
            if (_troopController.GetModel() == null) return;
        
            foreach (var behavior in _troopController.GetModel().SteeringBehavior.GetSteeringBehaviors())
            {
                if (_troopController.IsBehaviorEnabled(behavior.GetName()))
                {
                    Vector3 force = behavior.Execute(_troopController.SteeringContext);
                    _behaviorForces[behavior.GetName()] = force;
                
                    // Apply weight to force for combined visual
                    _combinedForce += force * behavior.GetWeight();
                }
                else
                {
                    _behaviorForces[behavior.GetName()] = Vector3.zero;
                }
            }
        
            // Limit combined force for visualization
            if (_combinedForce.magnitude > 10f)
            {
                _combinedForce = _combinedForce.normalized * 10f;
            }
        }
    
        private void DisplayStateInfo(float x, float y)
        {
            GUI.Box(new Rect(x, y, 150, 70), "State Info");
            GUI.Label(new Rect(x + 5, y + 20, 140, 20), $"State: {_troopController.GetState()}");
        
            if (_troopController.GetModel() != null)
            {
                float health = _troopController.GetModel().CurrentHealth;
                float maxHealth = _troopController.GetModel().MaxHealth;
                GUI.Label(new Rect(x + 5, y + 40, 140, 20), $"HP: {health:F0}/{maxHealth:F0}");
            }
        }
    
        private void DisplayBehaviorInfo(float x, float y)
        {
            if (_troopController.GetModel() == null) return;
        
            var behaviors = _troopController.GetModel().SteeringBehavior.GetSteeringBehaviors();
        
            GUI.Box(new Rect(x, y, 200, 30 + behaviors.Count * 20), "Behaviors");
        
            int enabledCount = 0;
            int yOffset = 20;
        
            foreach (var behavior in behaviors)
            {
                bool isEnabled = _troopController.IsBehaviorEnabled(behavior.GetName());
                string status = isEnabled ? "ON" : "OFF";
                string force = "";
            
                if (_behaviorForces.TryGetValue(behavior.GetName(), out Vector3 forceVector))
                {
                    force = $"F:{forceVector.magnitude:F1}";
                }
            
                GUI.Label(new Rect(x + 5, y + yOffset, 190, 20), 
                    $"{behavior.GetName()}: {status} (P:{behavior.GetPriority()}) {force}");
            
                yOffset += 20;
            
                if (isEnabled)
                {
                    enabledCount++;
                }
            }
        
            GUI.Label(new Rect(x + 5, y + yOffset, 190, 20), $"Enabled: {enabledCount}/{behaviors.Count}");
        }
    
        private void DisplayMovementInfo(float x, float y)
        {
            if (_troopController.GetModel() == null) return;
        
            GUI.Box(new Rect(x, y, 200, 100), "Movement");
        
            Vector3 pos = _troopController.GetPosition();
            Vector3 target = _troopController.GetTargetPosition();
            float velocity = _troopController.GetModel().Velocity.magnitude;
            float speedMult = _troopController.GetModel().TemporarySpeedMultiplier;
        
            GUI.Label(new Rect(x + 5, y + 20, 190, 20), 
                $"Pos: ({pos.x:F1}, {pos.z:F1})");
            GUI.Label(new Rect(x + 5, y + 40, 190, 20), 
                $"Target: ({target.x:F1}, {target.z:F1})");
            GUI.Label(new Rect(x + 5, y + 60, 190, 20), 
                $"Vel: {velocity:F1} (Mult: {speedMult:F1})");
        
            // Check squad info
            var squadExtensions = TroopControllerSquadExtensions.Instance;
            if (squadExtensions != null)
            {
                var squad = squadExtensions.GetSquad(_troopController);
                if (squad != null)
                {
                    var squadPos = squadExtensions.GetSquadPosition(_troopController);
                    Vector3 squadWorldPos = squad.GetPositionForTroop(squad, squadPos.x, squadPos.y);
                    float distToSquad = Vector3.Distance(_troopController.GetPosition(), squadWorldPos);
                
                    GUI.Label(new Rect(x + 5, y + 80, 190, 20), 
                        $"Squad: [{squadPos.x},{squadPos.y}] Dist:{distToSquad:F1}");
                }
            }
        }
    
        private Color GetColorForBehavior(string behaviorName)
        {
            switch (behaviorName)
            {
                case "Seek": return Color.green;
                case "Flee": return Color.red;
                case "Arrival": return Color.cyan;
                case "Separation": return Color.magenta;
                case "Cohesion": return Color.yellow;
                case "Alignment": return new Color(1f, 0.5f, 0);
                case "Obstacle Avoidance": return new Color(0.5f, 0, 0.5f);
                case "Path Following": return new Color(0, 0.5f, 0.5f);
                case "Surround": return new Color(0.5f, 0.5f, 0);
                case "Charge": return new Color(1f, 0, 0);
                case "Jump Attack": return new Color(0.7f, 0, 0.7f);
                case "Protect": return new Color(0, 0, 1f);
                case "Cover": return new Color(0, 0, 0.7f);
                case "Phalanx": return new Color(0.7f, 0.7f, 0);
                case "Testudo": return new Color(0.5f, 0.5f, 0.5f);
                case "Ambush Move": return new Color(0.3f, 0.3f, 0.3f);
                default: return Color.white;
            }
        }
    }
}