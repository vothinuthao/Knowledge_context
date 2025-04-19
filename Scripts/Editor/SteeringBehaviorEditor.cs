#if UNITY_EDITOR
using System.Collections.Generic;
using SteeringBehavior;
using Troop;
using UnityEditor;
using UnityEngine;

public class SteeringBehaviorEditor : EditorWindow
{
    // References
    private TroopConfigSO selectedConfig;
    private Vector2 scrollPosition;
    private List<SteeringBehaviorSO> allBehaviorTypes;
    private SteeringBehaviorSO behaviorToAdd;
    
    // Editor các trường của behavior
    private Dictionary<SteeringBehaviorSO, bool> foldoutStates = new Dictionary<SteeringBehaviorSO, bool>();
    private SerializedObject serializedConfig;
    
    // Mở cửa sổ editor
    [MenuItem("Wiking Raven/Steering Behavior Editor")]
    public static void ShowWindow()
    {
        GetWindow<SteeringBehaviorEditor>("Steering Behavior Editor");
    }
    
    private void OnEnable()
    {
        // Tìm tất cả các loại behavior trong dự án
        allBehaviorTypes = FindAllBehaviorTypes();
    }
    
    private void OnGUI()
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
            return;
        }
        
        if (serializedConfig == null)
        {
            serializedConfig = new SerializedObject(selectedConfig);
        }
        
        EditorGUILayout.Space(10);
        
        // Editor cho behavior list
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Behaviors", EditorStyles.boldLabel);
        
        // Tìm danh sách behaviors
        SerializedProperty behaviorListProperty = serializedConfig.FindProperty("behaviors");
        
        // Phần thêm behavior mới
        EditorGUILayout.BeginHorizontal();
        behaviorToAdd = EditorGUILayout.ObjectField("Add Behavior", behaviorToAdd, typeof(SteeringBehaviorSO), false) as SteeringBehaviorSO;
        GUI.enabled = behaviorToAdd != null && !selectedConfig.behaviors.Contains(behaviorToAdd);
        if (GUILayout.Button("Add", GUILayout.Width(60)))
        {
            // Thêm behavior mới
            Undo.RecordObject(selectedConfig, "Add Behavior");
            selectedConfig.behaviors.Add(behaviorToAdd);
            behaviorToAdd = null;
            serializedConfig.Update();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Hiển thị danh sách các behavior
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < behaviorListProperty.arraySize; i++)
        {
            SerializedProperty behaviorProperty = behaviorListProperty.GetArrayElementAtIndex(i);
            SteeringBehaviorSO behavior = behaviorProperty.objectReferenceValue as SteeringBehaviorSO;
            
            if (behavior == null) continue;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header bar with name and buttons
            EditorGUILayout.BeginHorizontal();
            
            // Ensure foldout state is initialized
            if (!foldoutStates.ContainsKey(behavior))
            {
                foldoutStates[behavior] = false;
            }
            
            // Foldout with behavior name
            foldoutStates[behavior] = EditorGUILayout.Foldout(foldoutStates[behavior], behavior.name, true);
            
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
        
        // Buttons to save, load or create behaviors
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Find All Behavior Types"))
        {
            allBehaviorTypes = FindAllBehaviorTypes();
        }
        
        if (GUILayout.Button("Create New Behavior"))
        {
            ShowCreateBehaviorMenu();
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Button to create a template behavior set
        if (GUILayout.Button("Create Template Behavior Set"))
        {
            CreateTemplateBehaviorSet();
        }
        
        EditorGUILayout.EndVertical();
        
        // Apply changes
        serializedConfig.ApplyModifiedProperties();
    }
    
    // Tìm tất cả các loại behavior trong dự án
    private List<SteeringBehaviorSO> FindAllBehaviorTypes()
    {
        List<SteeringBehaviorSO> results = new List<SteeringBehaviorSO>();
        
        // Tìm tất cả các asset kiểu SteeringBehaviorSO
        string[] guids = AssetDatabase.FindAssets("t:SteeringBehaviorSO");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SteeringBehaviorSO behavior = AssetDatabase.LoadAssetAtPath<SteeringBehaviorSO>(path);
            if (behavior != null)
            {
                results.Add(behavior);
            }
        }
        
        return results;
    }
    
    // Hiển thị menu để tạo behavior mới
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
        
        menu.ShowAsContext();
    }
    
    // Tạo behavior từng loại cụ thể
    private void CreateBehavior<T>() where T : SteeringBehaviorSO
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Behavior",
            typeof(T).Name.Replace("SO", ""),
            "asset",
            "Save behavior asset to"
        );
        
        if (string.IsNullOrEmpty(path)) return;
        
        T behaviorAsset = CreateInstance<T>();
        AssetDatabase.CreateAsset(behaviorAsset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        behaviorToAdd = behaviorAsset;
    }
    
    // Tạo template behavior set cho một troop cụ thể
    private void CreateTemplateBehaviorSet()
    {
        if (selectedConfig == null) return;
        
        string path = EditorUtility.SaveFolderPanel("Save Behavior Set", "Assets", selectedConfig.name + "_Behaviors");
        if (string.IsNullOrEmpty(path)) return;
        
        // Convert to project relative path
        path = "Assets" + path.Substring(Application.dataPath.Length);
        
        // Tạo và lưu các behavior cơ bản
        SeekBehaviorSO seek = CreateInstance<SeekBehaviorSO>();
        seek.weight = 1.0f;
        seek.description = "Base movement to target";
        AssetDatabase.CreateAsset(seek, path + "/Seek.asset");
        
        ArrivalBehaviorSO arrival = CreateInstance<ArrivalBehaviorSO>();
        arrival.weight = 2.0f;
        arrival.slowingDistance = 3.0f;
        arrival.description = "Slow down when approaching target";
        AssetDatabase.CreateAsset(arrival, path + "/Arrival.asset");
        
        SeparationBehaviorSO separation = CreateInstance<SeparationBehaviorSO>();
        separation.weight = 1.5f;
        separation.separationRadius = 2.0f;
        separation.description = "Keep distance from allies";
        AssetDatabase.CreateAsset(separation, path + "/Separation.asset");
        
        // Thêm vào config
        Undo.RecordObject(selectedConfig, "Add Template Behaviors");
        selectedConfig.behaviors.Clear();
        selectedConfig.behaviors.Add(seek);
        selectedConfig.behaviors.Add(arrival);
        selectedConfig.behaviors.Add(separation);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Cập nhật serialized object
        serializedConfig.Update();
    }
}

#endif