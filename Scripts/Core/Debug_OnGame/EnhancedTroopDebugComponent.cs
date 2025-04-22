using UnityEngine;
using Troop;
using System.Collections.Generic;
using SteeringBehavior;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnhancedTroopDebugComponent : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Enable/disable visualization of steering forces")]
    public bool showForces = true;
    
    [Tooltip("Scale for visualizing forces")]
    [Range(0.1f, 5f)]
    public float forceVisualizationScale = 1f;
    
    [Tooltip("Only show behavior visualization when troop is selected")]
    public bool onlyShowWhenSelected = true;
    
    [Header("State")]
    [SerializeField, ReadOnly] private string currentState = "Idle";
    [SerializeField, ReadOnly] private string previousState = "None";
    
    [Header("Squad")]
    [SerializeField, ReadOnly] private bool isInSquad = false;
    [SerializeField, ReadOnly] private Vector2Int squadPosition = new Vector2Int(-1, -1);
    [SerializeField, ReadOnly] private float distanceToSquadPosition = 0f;
    
    [Header("Movement")]
    [SerializeField, ReadOnly] private Vector3 currentPosition = Vector3.zero;
    [SerializeField, ReadOnly] private Vector3 targetPosition = Vector3.zero;
    [SerializeField, ReadOnly] private float currentVelocity = 0f;
    [SerializeField, ReadOnly] private float maxVelocity = 0f;
    
    [Header("Combat")]
    [SerializeField, ReadOnly] private bool hasTarget = false;
    [SerializeField, ReadOnly] private string targetName = "None";
    [SerializeField, ReadOnly] private float distanceToTarget = 0f;
    [SerializeField, ReadOnly] private float attackRange = 0f;
    [SerializeField, ReadOnly] private float attackTimer = 0f;
    
    // Internal data
    private TroopController _troopController;
    private Dictionary<string, bool> _behaviorStates = new Dictionary<string, bool>();
    private Dictionary<string, float> _behaviorWeights = new Dictionary<string, float>();
    private Dictionary<string, Vector3> _behaviorForces = new Dictionary<string, Vector3>();
    private Vector3 _combinedForce = Vector3.zero;
    
    // For serializing behavior data in inspector
    [System.Serializable]
    public class BehaviorInfo
    {
        public string name;
        public bool enabled;
        public float weight;
        public int priority;
        public float forceMagnitude;
    }
    
    [SerializeField]
    private List<BehaviorInfo> _behaviorInfos = new List<BehaviorInfo>();
    
    private void Awake()
    {
        _troopController = GetComponent<TroopController>();
        if (_troopController == null)
        {
            Debug.LogError("EnhancedTroopDebugComponent requires a TroopController component", this);
            enabled = false;
        }
    }
    
    private void Update()
    {
        if (_troopController == null) return;
        
        UpdateDisplayData();
        
        if (showForces && (onlyShowWhenSelected == false || IsSelected()))
        {
            UpdateForceVisualization();
        }
    }
    
    private void UpdateDisplayData()
    {
        // Update state info
        TroopState state = _troopController.GetState();
        currentState = state.ToString();
        previousState = _troopController.GetModel().PreviousState.ToString();
        
        // Update position and movement info
        currentPosition = _troopController.GetPosition();
        targetPosition = _troopController.GetTargetPosition();
        currentVelocity = _troopController.GetModel().Velocity.magnitude;
        maxVelocity = _troopController.GetModel().GetModifiedMoveSpeed();
        
        // Update squad info
        var squadExtensions = TroopControllerSquadExtensions.Instance;
        if (squadExtensions != null)
        {
            var squad = squadExtensions.GetSquad(_troopController);
            isInSquad = squad != null;
            
            if (isInSquad)
            {
                squadPosition = squadExtensions.GetSquadPosition(_troopController);
                Vector3 squadWorldPos = squad.GetPositionForTroop(squad, squadPosition.x, squadPosition.y);
                distanceToSquadPosition = Vector3.Distance(currentPosition, squadWorldPos);
            }
            else
            {
                squadPosition = new Vector2Int(-1, -1);
                distanceToSquadPosition = 0f;
            }
        }
        
        // Update combat info
        attackRange = _troopController.GetModel().AttackRange;
        attackTimer = _troopController.AttackTimer;
        
        // Update behavior info for Inspector
        _behaviorInfos.Clear();
        
        if (_troopController.GetModel() != null)
        {
            foreach (var behavior in _troopController.GetModel().SteeringBehavior.GetSteeringBehaviors())
            {
                if (behavior is ISteeringBehavior steeringBehavior)
                {
                    string name = steeringBehavior.GetName();
                    bool enabled = _troopController.IsBehaviorEnabled(name);
                    float weight = steeringBehavior.GetWeight();
                    int priority = steeringBehavior.GetPriority();
                    
                    float forceMagnitude = 0f;
                    if (_behaviorForces.TryGetValue(name, out Vector3 force))
                    {
                        forceMagnitude = force.magnitude;
                    }
                    
                    _behaviorInfos.Add(new BehaviorInfo 
                    { 
                        name = name, 
                        enabled = enabled, 
                        weight = weight,
                        priority = priority,
                        forceMagnitude = forceMagnitude
                    });
                }
            }
        }
    }
    
    private bool IsSelected()
    {
        #if UNITY_EDITOR
        return Selection.Contains(gameObject);
        #else
        return false;
        #endif
    }
    
    private void UpdateForceVisualization()
    {
        _behaviorForces.Clear();
        _combinedForce = Vector3.zero;
        
        if (_troopController.GetModel() == null) return;
        
        // Get steering context
        SteeringContext context = _troopController.SteeringContext;
        
        // Calculate forces for each behavior individually
        foreach (var behavior in _troopController.GetModel().SteeringBehavior.GetSteeringBehaviors())
        {
            if (behavior is ISteeringBehavior steeringBehavior)
            {
                string name = steeringBehavior.GetName();
                bool enabled = _troopController.IsBehaviorEnabled(name);
                
                if (enabled)
                {
                    Vector3 force = steeringBehavior.Execute(context);
                    _behaviorForces[name] = force;
                    
                    // Apply weight to force for combined visual
                    _combinedForce += force * steeringBehavior.GetWeight();
                }
                else
                {
                    _behaviorForces[name] = Vector3.zero;
                }
            }
        }
        
        // Limit combined force for visualization
        if (_combinedForce.magnitude > 10f)
        {
            _combinedForce = _combinedForce.normalized * 10f;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || _troopController == null || !showForces) return;
        
        // Only show gizmos when selected if that option is enabled
        if (onlyShowWhenSelected && !IsSelected()) return;
        
        // Draw individual behavior forces
        foreach (var kvp in _behaviorForces)
        {
            if (kvp.Value.magnitude > 0.01f)
            {
                Gizmos.color = GetColorForBehavior(kvp.Key);
                Gizmos.DrawLine(transform.position, transform.position + kvp.Value * forceVisualizationScale);
                Gizmos.DrawSphere(transform.position + kvp.Value * forceVisualizationScale, 0.1f);
            }
        }
        
        // Draw combined force
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + _combinedForce * forceVisualizationScale);
        Gizmos.DrawSphere(transform.position + _combinedForce * forceVisualizationScale, 0.15f);
        
        // Draw target position
        Gizmos.color = Color.yellow;
        if (targetPosition != Vector3.zero)
        {
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawSphere(targetPosition, 0.2f);
        }
        
        // Draw squad position if in a squad
        if (isInSquad)
        {
            var squadExtensions = TroopControllerSquadExtensions.Instance;
            if (squadExtensions != null)
            {
                var squad = squadExtensions.GetSquad(_troopController);
                if (squad != null)
                {
                    Vector3 squadWorldPos = squad.GetPositionForTroop(squad, squadPosition.x, squadPosition.y);
                    
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(transform.position, squadWorldPos);
                    Gizmos.DrawCube(squadWorldPos, Vector3.one * 0.3f);
                }
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
    
    // Helper methods for the inspector
    public void ToggleBehavior(string behaviorName)
    {
        if (_troopController != null)
        {
            bool currentState = _troopController.IsBehaviorEnabled(behaviorName);
            _troopController.EnableBehavior(behaviorName, !currentState);
        }
    }
    
    public void EnableOnlyBehavior(string behaviorName)
    {
        if (_troopController != null && _troopController.GetModel() != null)
        {
            // Disable all behaviors first
            foreach (var behavior in _troopController.GetModel().SteeringBehavior.GetSteeringBehaviors())
            {
                _troopController.EnableBehavior(behavior.GetName(), false);
            }
            
            // Then enable just the one we want
            _troopController.EnableBehavior(behaviorName, true);
        }
    }
}

// Custom ReadOnly attribute for inspector
public class ReadOnlyAttribute : PropertyAttribute {}

#if UNITY_EDITOR
// Custom property drawer for ReadOnly attribute
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}

// Custom editor for EnhancedTroopDebugComponent
[CustomEditor(typeof(EnhancedTroopDebugComponent))]
public class EnhancedTroopDebugComponentEditor : Editor
{
    bool showStateInfo = true;
    bool showMovementInfo = true;
    bool showSquadInfo = true;
    bool showCombatInfo = true;
    bool showBehaviorInfo = true;
    
    public override void OnInspectorGUI()
    {
        EnhancedTroopDebugComponent debugComponent = (EnhancedTroopDebugComponent)target;
        
        // Draw default inspector for debug settings
        SerializedProperty showForces = serializedObject.FindProperty("showForces");
        SerializedProperty forceVisualizationScale = serializedObject.FindProperty("forceVisualizationScale");
        SerializedProperty onlyShowWhenSelected = serializedObject.FindProperty("onlyShowWhenSelected");
        
        EditorGUILayout.LabelField("Visualization Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showForces);
        EditorGUILayout.PropertyField(forceVisualizationScale);
        EditorGUILayout.PropertyField(onlyShowWhenSelected);
        
        EditorGUILayout.Space();
        
        // Custom foldout for state info
        showStateInfo = EditorGUILayout.Foldout(showStateInfo, "State Information", true);
        if (showStateInfo)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentState"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("previousState"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Custom foldout for movement info
        showMovementInfo = EditorGUILayout.Foldout(showMovementInfo, "Movement Information", true);
        if (showMovementInfo)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentPosition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetPosition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentVelocity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxVelocity"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Custom foldout for squad info
        showSquadInfo = EditorGUILayout.Foldout(showSquadInfo, "Squad Information", true);
        if (showSquadInfo)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isInSquad"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("squadPosition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("distanceToSquadPosition"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Custom foldout for combat info
        showCombatInfo = EditorGUILayout.Foldout(showCombatInfo, "Combat Information", true);
        if (showCombatInfo)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hasTarget"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("distanceToTarget"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackTimer"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Custom behavior list with toggle buttons
        showBehaviorInfo = EditorGUILayout.Foldout(showBehaviorInfo, "Behavior Controls", true);
        if (showBehaviorInfo)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            SerializedProperty behaviorInfos = serializedObject.FindProperty("_behaviorInfos");
            if (behaviorInfos.arraySize > 0)
            {
                // Header
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField("Enabled", EditorStyles.boldLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField("Weight", EditorStyles.boldLabel, GUILayout.Width(50));
                EditorGUILayout.LabelField("Priority", EditorStyles.boldLabel, GUILayout.Width(50));
                EditorGUILayout.LabelField("Force", EditorStyles.boldLabel, GUILayout.Width(50));
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(2);
                
                for (int i = 0; i < behaviorInfos.arraySize; i++)
                {
                    SerializedProperty behaviorInfo = behaviorInfos.GetArrayElementAtIndex(i);
                    SerializedProperty name = behaviorInfo.FindPropertyRelative("name");
                    SerializedProperty enabled = behaviorInfo.FindPropertyRelative("enabled");
                    SerializedProperty weight = behaviorInfo.FindPropertyRelative("weight");
                    SerializedProperty priority = behaviorInfo.FindPropertyRelative("priority");
                    SerializedProperty forceMagnitude = behaviorInfo.FindPropertyRelative("forceMagnitude");
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    // Behavior name
                    EditorGUILayout.LabelField(name.stringValue, GUILayout.Width(120));
                    
                    // Enabled status (read-only)
                    EditorGUILayout.LabelField(enabled.boolValue ? "YES" : "NO", GUILayout.Width(60));
                    
                    // Weight (read-only)
                    EditorGUILayout.LabelField(weight.floatValue.ToString("F1"), GUILayout.Width(50));
                    
                    // Priority (read-only)
                    EditorGUILayout.LabelField(priority.intValue.ToString(), GUILayout.Width(50));
                    
                    // Force magnitude (read-only)
                    EditorGUILayout.LabelField(forceMagnitude.floatValue.ToString("F1"), GUILayout.Width(50));
                    
                    // Toggle button
                    string buttonText = enabled.boolValue ? "Disable" : "Enable";
                    if (GUILayout.Button(buttonText, GUILayout.Width(60)))
                    {
                        debugComponent.ToggleBehavior(name.stringValue);
                    }
                    
                    // Solo button
                    if (GUILayout.Button("Solo", GUILayout.Width(40)))
                    {
                        debugComponent.EnableOnlyBehavior(name.stringValue);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No behaviors available");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif