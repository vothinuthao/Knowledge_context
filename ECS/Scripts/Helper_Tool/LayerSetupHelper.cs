using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper class to set up required layers and assign objects to proper layers
/// </summary>
public class LayerSetupHelper : MonoBehaviour
{
    [System.Serializable]
    public class LayerAssignment
    {
        public string objectNameContains;
        public string targetLayer;
    }
    
    [Header("Layer Setup")]
    [SerializeField] private List<string> requiredLayers = new List<string>() 
    { 
        "Ground", 
        "Squad", 
        "Troops",
        "UI"
    };
    
    [Header("Object Assignment")]
    [SerializeField] private List<LayerAssignment> layerAssignments = new List<LayerAssignment>()
    {
        new LayerAssignment { objectNameContains = "Cell", targetLayer = "Ground" },
        new LayerAssignment { objectNameContains = "Troop", targetLayer = "Troops" },
        new LayerAssignment { objectNameContains = "Squad", targetLayer = "Squad" }
    };
    
    [Header("Settings")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool showLog = true;
    
    private void Start()
    {
        if (applyOnStart)
        {
            AssignLayersToObjects();
        }
    }
    
    /// <summary>
    /// Assigns layers to objects based on naming patterns
    /// </summary>
    public void AssignLayersToObjects()
    {
        // Check if required layers exist first
        foreach (var layerName in requiredLayers)
        {
            if (LayerMask.NameToLayer(layerName) == -1)
            {
                Debug.LogWarning($"Layer '{layerName}' does not exist! Please create it in project settings.");
            }
        }
        
        // Assign layers based on name patterns
        foreach (var assignment in layerAssignments)
        {
            int layer = LayerMask.NameToLayer(assignment.targetLayer);
            if (layer == -1)
            {
                Debug.LogWarning($"Layer '{assignment.targetLayer}' does not exist. Skipping assignment.");
                continue;
            }
            
            int count = 0;
            var objects = FindObjectsContainingString(assignment.objectNameContains);
            
            foreach (var obj in objects)
            {
                if (obj.layer != layer)
                {
                    obj.layer = layer;
                    count++;
                }
            }
            
            if (showLog && count > 0)
            {
                Debug.Log($"Assigned {count} objects to layer '{assignment.targetLayer}' based on name containing '{assignment.objectNameContains}'");
            }
        }
    }
    
    private GameObject[] FindObjectsContainingString(string substring)
    {
        List<GameObject> results = new List<GameObject>();
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains(substring))
            {
                results.Add(obj);
            }
        }
        
        return results.ToArray();
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Creates required layers in the project settings
    /// </summary>
    public void CreateRequiredLayers()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        
        for (int i = 0; i < requiredLayers.Count; i++)
        {
            string layerName = requiredLayers[i];
            bool layerExists = false;
            
            // Check if layer already exists
            for (int j = 0; j < 32; j++)
            {
                SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(j);
                if (layerProp.stringValue == layerName)
                {
                    layerExists = true;
                    break;
                }
            }
            
            if (!layerExists)
            {
                // Find empty layer slot
                for (int j = 8; j < 32; j++) // Start from 8 (first user layer)
                {
                    SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(j);
                    if (string.IsNullOrEmpty(layerProp.stringValue))
                    {
                        layerProp.stringValue = layerName;
                        Debug.Log($"Created layer '{layerName}' at index {j}");
                        break;
                    }
                }
            }
        }
        
        tagManager.ApplyModifiedProperties();
    }
    #endif
    
    /// <summary>
    /// Sets up layers and assigns layers to all objects
    /// </summary>
    public void SetupLayersAndAssign()
    {
        #if UNITY_EDITOR
        CreateRequiredLayers();
        #endif
        
        AssignLayersToObjects();
    }
}

