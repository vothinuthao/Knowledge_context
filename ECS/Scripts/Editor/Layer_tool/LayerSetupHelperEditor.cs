using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
/// <summary>
/// Custom editor for LayerSetupHelper
/// </summary>
[CustomEditor(typeof(LayerSetupHelper))]
public class LayerSetupHelperEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        LayerSetupHelper layerHelper = (LayerSetupHelper)target;
        
        if (GUILayout.Button("Create Required Layers"))
        {
            layerHelper.SetupLayersAndAssign();
        }
        
        if (GUILayout.Button("Assign Layers to Objects"))
        {
            layerHelper.AssignLayersToObjects();
        }
    }
}
#endif