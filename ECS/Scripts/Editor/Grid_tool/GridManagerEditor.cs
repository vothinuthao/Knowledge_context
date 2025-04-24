using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
/// <summary>
/// Editor script for GridManager customization in Inspector
/// </summary>
[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GridManager gridManager = (GridManager)target;
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Preview Grid"))
        {
            // Force OnDrawGizmos to update
            SceneView.RepaintAll();
        }
        
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Clear All Occupied Cells"))
            {
                gridManager.ClearAllOccupiedCells();
            }
        }
    }
}
#endif