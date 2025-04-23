#if UNITY_EDITOR
using System.Collections.Generic;
using SteeringBehavior;
using Troop;
using UnityEditor;
using UnityEngine;
using System.IO;

public class SteeringBehaviorEditor : EditorWindow
{
    // References
    private TroopConfigSO selectedConfig;
    private TroopTemplate selectedTemplate;
    private BehaviorTemplateSet templateSet;
    
    private Vector2 behaviorScrollPosition;
    private Vector2 templateScrollPosition;
    
    // Editor states
    private Dictionary<SteeringBehaviorSO, bool> foldoutStates = new Dictionary<SteeringBehaviorSO, bool>();
    private SerializedObject serializedConfig;
    private SerializedObject serializedTemplate;
    
    // UI tabs
    private enum EditorTab { Behaviors, Templates, TroopTemplates }
    private EditorTab currentTab = EditorTab.Behaviors;
    
    // Filter settings
    private bool showMovementBehaviors = true;
    private bool showFormationBehaviors = true;
    private bool showCombatBehaviors = true;
    private bool showSpecialBehaviors = true;
    private bool showEssentialBehaviors = true;
    
    // Mở cửa sổ editor
    [MenuItem("Wiking Raven/Steering Behavior Editor")]
    public static void ShowWindow()
    {
        GetWindow<SteeringBehaviorEditor>("Steering Behavior Editor");
    }
    
    private void OnEnable()
    {
        // Find all available template sets
        string[] templateSetGuids = AssetDatabase.FindAssets("t:BehaviorTemplateSet");
        if (templateSetGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(templateSetGuids[0]);
            templateSet = AssetDatabase.LoadAssetAtPath<BehaviorTemplateSet>(path);
        }
    }
    
    private void OnGUI()
    {
        // Tabs
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Toggle(currentTab == EditorTab.Behaviors, "Behaviors", EditorStyles.toolbarButton))
            currentTab = EditorTab.Behaviors;
        if (GUILayout.Toggle(currentTab == EditorTab.Templates, "Behavior Templates", EditorStyles.toolbarButton))
            currentTab = EditorTab.Templates;
        if (GUILayout.Toggle(currentTab == EditorTab.TroopTemplates, "Troop Templates", EditorStyles.toolbarButton))
            currentTab = EditorTab.TroopTemplates;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Draw the appropriate tab
        switch (currentTab)
        {
            case EditorTab.Behaviors:
                DrawBehaviorsTab();
                break;
            case EditorTab.Templates:
                DrawTemplatesTab();
                break;
            case EditorTab.TroopTemplates:
                DrawTroopTemplatesTab();
                break;
        }
    }
    
    #region Behaviors Tab
    
    private void DrawBehaviorsTab()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Troop Config", EditorStyles.boldLabel);
        
        // Chọn config
        EditorGUI.BeginChangeCheck();
        selectedConfig = EditorGUILayout.ObjectField("Troop Config", selectedConfig, typeof(TroopConfigSO), false) as TroopConfigSO;
        if (EditorGUI.EndChangeCheck())
        {
            // Reset serialized object khi config thay đổi
            if (selectedConfig != null)
            {
                serializedConfig = new SerializedObject(selectedConfig);
            }
        }
        EditorGUILayout.EndVertical();
        
        if (selectedConfig == null)
        {
            EditorGUILayout.HelpBox("Hãy chọn một Troop Config để chỉnh sửa behaviors.", MessageType.Info);
            
            // Thêm nút tạo config mới
            if (GUILayout.Button("Create New Troop Config"))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Create Troop Config",
                    "New Troop Config",
                    "asset",
                    "Save new troop config to"
                );
                
                if (!string.IsNullOrEmpty(path))
                {
                    TroopConfigSO newConfig = ScriptableObject.CreateInstance<TroopConfigSO>();
                    newConfig.troopName = "New Troop";
                    newConfig.health = 100f;
                    newConfig.attackPower = 10f;
                    newConfig.moveSpeed = 3f;
                    newConfig.attackRange = 1.5f;
                    newConfig.attackSpeed = 1f;
                    newConfig.behaviors = new List<SteeringBehaviorSO>();
                    
                    AssetDatabase.CreateAsset(newConfig, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    selectedConfig = newConfig;
                    serializedConfig = new SerializedObject(selectedConfig);
                }
            }
            
            return;
        }
        
        if (serializedConfig == null)
        {
            serializedConfig = new SerializedObject(selectedConfig);
        }
        
        EditorGUILayout.Space(10);
        
        // Filters
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Filter Behaviors", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        showMovementBehaviors = EditorGUILayout.ToggleLeft("Movement", showMovementBehaviors, GUILayout.Width(100));
        showFormationBehaviors = EditorGUILayout.ToggleLeft("Formation", showFormationBehaviors, GUILayout.Width(100));
        showCombatBehaviors = EditorGUILayout.ToggleLeft("Combat", showCombatBehaviors, GUILayout.Width(100));
        showSpecialBehaviors = EditorGUILayout.ToggleLeft("Special", showSpecialBehaviors, GUILayout.Width(100));
        showEssentialBehaviors = EditorGUILayout.ToggleLeft("Essential", showEssentialBehaviors, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        // Editor cho behavior list
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Behaviors", EditorStyles.boldLabel);
        
        // Tìm danh sách behaviors
        SerializedProperty behaviorListProperty = serializedConfig.FindProperty("behaviors");
        
        // Phần thêm behavior từ template
        if (templateSet != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Add From Template:", GUILayout.Width(120));
            
            if (GUILayout.Button("Movement", GUILayout.Width(80)))
            {
                AddBehaviorsByCategory(BehaviorCategory.Movement);
            }
            
            if (GUILayout.Button("Formation", GUILayout.Width(80)))
            {
                AddBehaviorsByCategory(BehaviorCategory.Formation);
            }
            
            if (GUILayout.Button("Combat", GUILayout.Width(80)))
            {
                AddBehaviorsByCategory(BehaviorCategory.Combat);
            }
            
            if (GUILayout.Button("Special", GUILayout.Width(80)))
            {
                AddBehaviorsByCategory(BehaviorCategory.Special);
            }
            
            if (GUILayout.Button("Essential", GUILayout.Width(80)))
            {
                AddBehaviorsByCategory(BehaviorCategory.Essential);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space(5);
        
        // Manual behavior add
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Add Individual Behavior:", GUILayout.Width(150));
        
        if (GUILayout.Button("Create New Behavior", GUILayout.Width(150)))
        {
            ShowCreateBehaviorMenu();
        }
        
        if (GUILayout.Button("Find Existing", GUILayout.Width(150)))
        {
            OpenAddBehaviorWindow();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Hiển thị danh sách các behavior
        behaviorScrollPosition = EditorGUILayout.BeginScrollView(behaviorScrollPosition);
        
        for (int i = 0; i < behaviorListProperty.arraySize; i++)
        {
            SerializedProperty behaviorProperty = behaviorListProperty.GetArrayElementAtIndex(i);
            SteeringBehaviorSO behavior = behaviorProperty.objectReferenceValue as SteeringBehaviorSO;
            
            if (behavior == null) continue;
            
            // Filtering
            BehaviorCategory category = GetBehaviorCategory(behavior);
            if (!ShouldShowBehavior(category))
                continue;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header bar with name and buttons
            EditorGUILayout.BeginHorizontal();
            
            // Ensure foldout state is initialized
            if (!foldoutStates.ContainsKey(behavior))
            {
                foldoutStates[behavior] = false;
            }
            
            // Show behavior category as an icon/color
            GUI.color = GetCategoryColor(category);
            EditorGUILayout.LabelField(GetCategoryIcon(category), GUILayout.Width(20));
            GUI.color = Color.white;
            
            // Foldout with behavior name
            foldoutStates[behavior] = EditorGUILayout.Foldout(foldoutStates[behavior], behavior.name, true);
            
            // Priority field
            EditorGUILayout.LabelField("Priority:", GUILayout.Width(50));
            int priority = EditorGUILayout.IntField(GetBehaviorPriority(behavior), GUILayout.Width(40));
            SetBehaviorPriority(behavior, priority);
            
            // Move up button
            GUI.enabled = i > 0;
            if (GUILayout.Button("▲", GUILayout.Width(25)))
            {
                Undo.RecordObject(selectedConfig, "Move Behavior Up");
                selectedConfig.behaviors.RemoveAt(i);
                selectedConfig.behaviors.Insert(i - 1, behavior);
                serializedConfig.Update();
            }
            
            // Move down button
            GUI.enabled = i < behaviorListProperty.arraySize - 1;
            if (GUILayout.Button("▼", GUILayout.Width(25)))
            {
                Undo.RecordObject(selectedConfig, "Move Behavior Down");
                selectedConfig.behaviors.RemoveAt(i);
                selectedConfig.behaviors.Insert(i + 1, behavior);
                serializedConfig.Update();
            }
            
            // Remove button
            GUI.enabled = true;
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                Undo.RecordObject(selectedConfig, "Remove Behavior");
                selectedConfig.behaviors.RemoveAt(i);
                serializedConfig.Update();
                i--; // Adjust index after removal
                continue;
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Display behavior properties if expanded
            if (foldoutStates[behavior])
            {
                SerializedObject behaviorObject = new SerializedObject(behavior);
                
                // Display all serialized properties except script field
                SerializedProperty behaviorProperty2 = behaviorObject.GetIterator();
                bool enterChildren = true;
                while (behaviorProperty2.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    
                    // Skip script field
                    if (behaviorProperty2.name.Equals("m_Script")) continue;
                    
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(behaviorProperty2, true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(behavior, "Change Behavior Property");
                        behaviorObject.ApplyModifiedProperties();
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(10);
        
        // Buttons for template operations
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Save As Template"))
        {
            SaveBehaviorsAsTemplate();
        }
        
        if (GUILayout.Button("Apply Default Template"))
        {
            ApplyDefaultTemplate();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        // Apply changes
        serializedConfig.ApplyModifiedProperties();
    }
    
    private void AddBehaviorsByCategory(BehaviorCategory category)
    {
        if (templateSet == null || selectedConfig == null) return;
        
        Undo.RecordObject(selectedConfig, "Add Behaviors By Category");
        
        List<SteeringBehaviorSO> behaviorsToAdd = templateSet.GetBehaviorsByCategory(category);
        
        foreach (var behavior in behaviorsToAdd)
        {
            if (!selectedConfig.behaviors.Contains(behavior))
            {
                selectedConfig.behaviors.Add(behavior);
            }
        }
        
        serializedConfig.Update();
    }
    
    private void OpenAddBehaviorWindow()
    {
        // Find all behavior SOs
        string[] guids = AssetDatabase.FindAssets("t:SteeringBehaviorSO");
        List<SteeringBehaviorSO> availableBehaviors = new List<SteeringBehaviorSO>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SteeringBehaviorSO behavior = AssetDatabase.LoadAssetAtPath<SteeringBehaviorSO>(path);
            if (behavior != null && !selectedConfig.behaviors.Contains(behavior))
            {
                availableBehaviors.Add(behavior);
            }
        }
        
        if (availableBehaviors.Count == 0)
        {
            EditorUtility.DisplayDialog("No Behaviors Found", "No additional behaviors found. You can create new ones using the 'Create New Behavior' button.", "OK");
            return;
        }
        
        // Create popup window
        GenericMenu menu = new GenericMenu();
        
        foreach (var behavior in availableBehaviors)
        {
            menu.AddItem(new GUIContent(behavior.name), false, () => {
                Undo.RecordObject(selectedConfig, "Add Behavior");
                selectedConfig.behaviors.Add(behavior);
                serializedConfig.Update();
            });
        }
        
        menu.ShowAsContext();
    }
    
    private void ShowCreateBehaviorMenu()
    {
        GenericMenu menu = new GenericMenu();
        
        // Add menu items for each behavior type
        menu.AddItem(new GUIContent("Seek"), false, CreateBehavior<SeekBehaviorSO>);
        menu.AddItem(new GUIContent("Flee"), false, CreateBehavior<FleeBehaviorSO>);
        menu.AddItem(new GUIContent("Arrival"), false, CreateBehavior<ArrivalBehaviorSO>);
        menu.AddItem(new GUIContent("Separation"), false, CreateBehavior<SeparationBehaviorSO>);
        menu.AddItem(new GUIContent("Cohesion"), false, CreateBehavior<CohesionBehaviorSO>);
        menu.AddItem(new GUIContent("Alignment"), false, CreateBehavior<AlignmentBehaviorSO>);
        menu.AddItem(new GUIContent("Obstacle Avoidance"), false, CreateBehavior<ObstacleAvoidanceBehaviorSO>);
        menu.AddItem(new GUIContent("Path Following"), false, CreateBehavior<PathFollowingBehaviorSO>);
        
        // Add more behaviors based on the Troop Behavior document
        // Example: menu.AddItem(new GUIContent("Jump Attack"), false, CreateCustomBehavior<JumpAttackBehaviorSO>);
        
        menu.ShowAsContext();
    }
    
    private void CreateBehavior<T>() where T : SteeringBehaviorSO
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Behavior",
            typeof(T).Name.Replace("SO", ""),
            "asset",
            "Save behavior asset to"
        );
        
        if (string.IsNullOrEmpty(path)) return;
        
        T behaviorAsset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(behaviorAsset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        if (selectedConfig != null)
        {
            Undo.RecordObject(selectedConfig, "Add New Behavior");
            selectedConfig.behaviors.Add(behaviorAsset);
            serializedConfig.Update();
        }
    }
    
    private void SaveBehaviorsAsTemplate()
    {
        if (selectedConfig == null || selectedConfig.behaviors.Count == 0) return;
        
        string templateName = EditorUtility.SaveFilePanelInProject(
            "Save As Template",
            selectedConfig.troopName + "_Template",
            "asset",
            "Save behavior template as"
        );
        
        if (string.IsNullOrEmpty(templateName)) return;
        
        // Create template
        TroopTemplate template = ScriptableObject.CreateInstance<TroopTemplate>();
        template.templateName = selectedConfig.troopName + " Template";
        template.description = "Template based on " + selectedConfig.troopName;
        
        // Copy base stats
        template.baseHealth = selectedConfig.health;
        template.baseAttackPower = selectedConfig.attackPower;
        template.baseMoveSpeed = selectedConfig.moveSpeed;
        template.baseAttackRange = selectedConfig.attackRange;
        template.baseAttackSpeed = selectedConfig.attackSpeed;
        
        // Reference template set
        template.templateSet = templateSet;
        
        // Save the template
        AssetDatabase.CreateAsset(template, templateName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Template Saved", "Behavior template saved successfully!", "OK");
    }
    
    private void ApplyDefaultTemplate()
    {
        if (selectedConfig == null) return;
        
        if (EditorUtility.DisplayDialog("Apply Default Template",
            "This will clear all existing behaviors and apply default ones. Continue?",
            "Yes", "Cancel"))
        {
            if (templateSet != null)
            {
                Undo.RecordObject(selectedConfig, "Apply Default Template");
                
                selectedConfig.behaviors.Clear();
                selectedConfig.behaviors.AddRange(templateSet.GetDefaultBehaviors());
                
                serializedConfig.Update();
            }
            else
            {
                EditorUtility.DisplayDialog("No Template Set", "No behavior template set found. Please create one first.", "OK");
            }
        }
    }
    
    // Helper functions for categories
    private BehaviorCategory GetBehaviorCategory(SteeringBehaviorSO behavior)
    {
        if (templateSet == null) return BehaviorCategory.Movement;
        
        foreach (var template in GetAllTemplates())
        {
            if (template.behaviorSO == behavior)
            {
                return template.category;
            }
        }
        
        // Default to Movement if not found
        return BehaviorCategory.Movement;
    }
    
    private int GetBehaviorPriority(SteeringBehaviorSO behavior)
    {
        // Simple reflection to get priority field if it exists
        System.Type type = behavior.GetType();
        var field = type.GetField("priority");
        if (field != null)
        {
            return (int)field.GetValue(behavior);
        }
        
        // Default priority
        return 0;
    }
    
    private void SetBehaviorPriority(SteeringBehaviorSO behavior, int priority)
    {
        // Simple reflection to set priority field if it exists
        System.Type type = behavior.GetType();
        var field = type.GetField("priority");
        if (field != null)
        {
            Undo.RecordObject(behavior, "Change Behavior Priority");
            field.SetValue(behavior, priority);
            EditorUtility.SetDirty(behavior);
        }
    }
    
    private bool ShouldShowBehavior(BehaviorCategory category)
    {
        switch (category)
        {
            case BehaviorCategory.Movement: return showMovementBehaviors;
            case BehaviorCategory.Formation: return showFormationBehaviors;
            case BehaviorCategory.Combat: return showCombatBehaviors;
            case BehaviorCategory.Special: return showSpecialBehaviors;
            case BehaviorCategory.Essential: return showEssentialBehaviors;
            default: return true;
        }
    }
    
    private Color GetCategoryColor(BehaviorCategory category)
    {
        switch (category)
        {
            case BehaviorCategory.Movement: return new Color(0.2f, 0.7f, 0.2f);
            case BehaviorCategory.Formation: return new Color(0.2f, 0.2f, 0.7f);
            case BehaviorCategory.Combat: return new Color(0.7f, 0.2f, 0.2f);
            case BehaviorCategory.Special: return new Color(0.7f, 0.7f, 0.2f);
            case BehaviorCategory.Essential: return new Color(0.7f, 0.2f, 0.7f);
            default: return Color.white;
        }
    }
    
    private string GetCategoryIcon(BehaviorCategory category)
    {
        switch (category)
        {
            case BehaviorCategory.Movement: return "⟳";
            case BehaviorCategory.Formation: return "⧉";
            case BehaviorCategory.Combat: return "⚔";
            case BehaviorCategory.Special: return "★";
            case BehaviorCategory.Essential: return "⚙";
            default: return "⋄";
        }
    }
    
    #endregion
    
    #region Templates Tab
    
    private void DrawTemplatesTab()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Behavior Template Set", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        templateSet = EditorGUILayout.ObjectField("Template Set", templateSet, typeof(BehaviorTemplateSet), false) as BehaviorTemplateSet;
        EditorGUILayout.EndVertical();
        
        if (templateSet == null)
        {
            EditorGUILayout.HelpBox("No template set selected. Create or select a BehaviorTemplateSet asset.", MessageType.Info);
            
            if (GUILayout.Button("Create New Template Set"))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Create Template Set",
                    "BehaviorTemplateSet",
                    "asset",
                    "Save template set asset to"
                );
                
                if (!string.IsNullOrEmpty(path))
                {
                    BehaviorTemplateSet newSet = ScriptableObject.CreateInstance<BehaviorTemplateSet>();
                    AssetDatabase.CreateAsset(newSet, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    templateSet = newSet;
                }
            }
            
            return;
        }
        
        EditorGUILayout.Space(10);
        
        // Template management section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Behavior Templates", EditorStyles.boldLabel);
        
        // List all templates
        templateScrollPosition = EditorGUILayout.BeginScrollView(templateScrollPosition);
        
        List<BehaviorTemplate> templates = GetAllTemplates();
        
        for (int i = 0; i < templates.Count; i++)
        {
            BehaviorTemplate template = templates[i];
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            GUI.color = GetCategoryColor(template.category);
            EditorGUILayout.LabelField(GetCategoryIcon(template.category), GUILayout.Width(20));
            GUI.color = Color.white;
            
            EditorGUILayout.LabelField(template.name, EditorStyles.boldLabel);
            
            // Category dropdown
            template.category = (BehaviorCategory)EditorGUILayout.EnumPopup(template.category, GUILayout.Width(100));
            
            // Default toggle
            template.isDefault = EditorGUILayout.Toggle("Default", template.isDefault, GUILayout.Width(80));
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Behavior:", GUILayout.Width(60));
            template.behaviorSO = EditorGUILayout.ObjectField(template.behaviorSO, typeof(SteeringBehaviorSO), false) as SteeringBehaviorSO;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Description:", GUILayout.Width(80));
            template.description = EditorGUILayout.TextField(template.description);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndScrollView();
        
        // Add template button
        if (GUILayout.Button("Add New Template"))
        {
            AddNewTemplate();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private List<BehaviorTemplate> GetAllTemplates()
    {
        // This is a placeholder - in a real implementation you would access the templates from the templateSet
        // For now we'll return an empty list
        return new List<BehaviorTemplate>();
    }
    
    private void AddNewTemplate()
    {
        // Create a new template dialog
        string[] behaviorGuids = AssetDatabase.FindAssets("t:SteeringBehaviorSO");
        
        if (behaviorGuids.Length == 0)
        {
            EditorUtility.DisplayDialog("No Behaviors", "No steering behaviors found in the project.", "OK");
            return;
        }
        
        GenericMenu menu = new GenericMenu();
        
        foreach (string guid in behaviorGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SteeringBehaviorSO behavior = AssetDatabase.LoadAssetAtPath<SteeringBehaviorSO>(path);
            
            menu.AddItem(new GUIContent(behavior.name), false, () => {
                BehaviorTemplate newTemplate = new BehaviorTemplate
                {
                    name = behavior.name,
                    behaviorSO = behavior,
                    category = BehaviorCategory.Movement,
                    isDefault = false,
                    description = "Template for " + behavior.name
                };
                
                // Add the template to the set
                // This would need proper implementation in the BehaviorTemplateSet class
                EditorUtility.SetDirty(templateSet);
            });
        }
        
        menu.ShowAsContext();
    }
    
    #endregion
    
    #region Troop Templates Tab
    
    private void DrawTroopTemplatesTab()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Troop Template Management", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        selectedTemplate = EditorGUILayout.ObjectField("Troop Template", selectedTemplate, typeof(TroopTemplate), false) as TroopTemplate;
        
        if (EditorGUI.EndChangeCheck())
        {
            if (selectedTemplate != null)
            {
                serializedTemplate = new SerializedObject(selectedTemplate);
            }
        }
        
        EditorGUILayout.EndVertical();
        
        if (selectedTemplate == null)
        {
            EditorGUILayout.HelpBox("No troop template selected. Create or select a TroopTemplate asset.", MessageType.Info);
            
            if (GUILayout.Button("Create New Troop Template"))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Create Troop Template",
                    "TroopTemplate",
                    "asset",
                    "Save troop template asset to"
                );
                
                if (!string.IsNullOrEmpty(path))
                {
                    TroopTemplate newTemplate = ScriptableObject.CreateInstance<TroopTemplate>();
                    newTemplate.templateName = "New Troop Template";
                    newTemplate.templateSet = templateSet;
                    
                    AssetDatabase.CreateAsset(newTemplate, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    selectedTemplate = newTemplate;
                    serializedTemplate = new SerializedObject(selectedTemplate);
                }
            }
            
            return;
        }
        
        if (serializedTemplate == null)
        {
            serializedTemplate = new SerializedObject(selectedTemplate);
        }
        
        EditorGUILayout.Space(10);
        
        // Troop template editor
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        SerializedProperty templateNameProp = serializedTemplate.FindProperty("templateName");
        SerializedProperty descriptionProp = serializedTemplate.FindProperty("description");
        
        // Basic properties
        EditorGUILayout.PropertyField(templateNameProp);
        EditorGUILayout.PropertyField(descriptionProp);
        
        EditorGUILayout.Space(5);
        
        // Base stats
        EditorGUILayout.LabelField("Base Stats", EditorStyles.boldLabel);
        
        SerializedProperty healthProp = serializedTemplate.FindProperty("baseHealth");
        SerializedProperty attackPowerProp = serializedTemplate.FindProperty("baseAttackPower");
        SerializedProperty moveSpeedProp = serializedTemplate.FindProperty("baseMoveSpeed");
        SerializedProperty attackRangeProp = serializedTemplate.FindProperty("baseAttackRange");
        SerializedProperty attackSpeedProp = serializedTemplate.FindProperty("baseAttackSpeed");
        
        EditorGUILayout.PropertyField(healthProp);
        EditorGUILayout.PropertyField(attackPowerProp);
        EditorGUILayout.PropertyField(moveSpeedProp);
        EditorGUILayout.PropertyField(attackRangeProp);
        EditorGUILayout.PropertyField(attackSpeedProp);
        
        EditorGUILayout.Space(5);
        
        // Behavior categories
        EditorGUILayout.LabelField("Behavior Categories", EditorStyles.boldLabel);
        
        SerializedProperty movementProp = serializedTemplate.FindProperty("includeMovementBehaviors");
        SerializedProperty formationProp = serializedTemplate.FindProperty("includeFormationBehaviors");
        SerializedProperty combatProp = serializedTemplate.FindProperty("includeCombatBehaviors");
        SerializedProperty specialProp = serializedTemplate.FindProperty("includeSpecialBehaviors");
        
        EditorGUILayout.PropertyField(movementProp);
        EditorGUILayout.PropertyField(formationProp);
        EditorGUILayout.PropertyField(combatProp);
        EditorGUILayout.PropertyField(specialProp);
        
        EditorGUILayout.Space(5);
        
        // Template set reference
        SerializedProperty templateSetProp = serializedTemplate.FindProperty("templateSet");
        EditorGUILayout.PropertyField(templateSetProp);
        
        EditorGUILayout.Space(5);
        
        // Create troop from template button
        if (GUILayout.Button("Create Troop From Template"))
        {
            CreateTroopFromTemplate();
        }
        
        EditorGUILayout.EndVertical();
        
        // Apply changes
        serializedTemplate.ApplyModifiedProperties();
    }
    
    private void CreateTroopFromTemplate()
    {
        if (selectedTemplate == null) return;
        
        string troopName = EditorUtility.SaveFilePanelInProject(
            "Create Troop Config",
            selectedTemplate.templateName + "_Troop",
            "asset",
            "Save new troop config to"
        );
        
        if (string.IsNullOrEmpty(troopName)) return;
        
        // Create the troop config
        TroopConfigSO newTroop = selectedTemplate.CreateTroopConfig(Path.GetFileNameWithoutExtension(troopName));
        
        // Save it
        AssetDatabase.CreateAsset(newTroop, troopName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Select it
        selectedConfig = newTroop;
        serializedConfig = new SerializedObject(selectedConfig);
        currentTab = EditorTab.Behaviors;
        
        EditorUtility.DisplayDialog("Troop Created", "New troop config created successfully!", "OK");
    }
    
    #endregion
}
#endif