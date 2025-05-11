using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using VikingRaven.Configuration;
using VikingRaven.Units.Components;

namespace VikingRaven.Editor.Configuration
{
    public class CSVDataManager : OdinEditorWindow
    {
        [MenuItem("VikingRaven/Configuration/CSV Data Manager")]
        private static void OpenWindow()
        {
            GetWindow<CSVDataManager>().Show();
        }

        [PropertySpace(10)]
        [Title("CSV Data Manager", "Synchronize data between ScriptableObjects and CSV files")]
        
        [TabGroup("Sync", "System Configuration")]
        [BoxGroup("Sync/System Configuration/Source")]
        [LabelText("System Config SO")]
        [AssetsOnly, AssetSelector(Paths = "Assets")]
        public SystemConfigurationSO SystemConfigurationAsset;
        
        [BoxGroup("Sync/System Configuration/Source")]
        [LabelText("CSV File Path")]
        [FolderPath(RequireExistingPath = true)]
        public string SystemConfigCsvFolder = "Assets/Data/CSV";
        
        [BoxGroup("Sync/System Configuration/Source")]
        [LabelText("CSV File Name")]
        public string SystemConfigCsvFilename = "SystemConfiguration.csv";
        
        [BoxGroup("Sync/System Configuration/Actions")]
        [HorizontalGroup("Sync/System Configuration/Actions/Buttons")]
        [Button("Export SO to CSV"), GUIColor(0.4f, 0.8f, 0.8f)]
        public void ExportSystemConfigToCSV()
        {
            if (SystemConfigurationAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a System Configuration asset first.", "OK");
                return;
            }
            
            try
            {
                // Ensure the directory exists
                Directory.CreateDirectory(SystemConfigCsvFolder);
                
                // Full path to the CSV file
                string fullPath = Path.Combine(SystemConfigCsvFolder, SystemConfigCsvFilename);
                
                using (StreamWriter writer = new StreamWriter(fullPath))
                {
                    // Write header
                    writer.WriteLine("SystemType,IsActive,Priority,UpdateInterval,CustomParameters");
                    
                    // Write data for each system
                    foreach (var config in SystemConfigurationAsset.SystemConfigurations)
                    {
                        string customParams = string.Join("|", config.CustomParameters
                            .Select(kv => $"{kv.Key}:{kv.Value}"));
                            
                        writer.WriteLine($"{config.SystemType},{config.IsActive},{config.Priority},{config.UpdateInterval},{customParams}");
                    }
                }
                
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Success", $"System Configuration exported to {fullPath}", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error exporting to CSV: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to export to CSV: {ex.Message}", "OK");
            }
        }
        
        [HorizontalGroup("Sync/System Configuration/Actions/Buttons")]
        [Button("Import CSV to SO"), GUIColor(0.8f, 0.4f, 0.8f)]
        public void ImportSystemConfigFromCSV()
        {
            if (SystemConfigurationAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a System Configuration asset first.", "OK");
                return;
            }
            
            try
            {
                // Full path to the CSV file
                string fullPath = Path.Combine(SystemConfigCsvFolder, SystemConfigCsvFilename);
                
                if (!File.Exists(fullPath))
                {
                    EditorUtility.DisplayDialog("Error", $"CSV file not found at {fullPath}", "OK");
                    return;
                }
                
                SystemConfigurationAsset.SystemConfigurations.Clear();
                
                using (StreamReader reader = new StreamReader(fullPath))
                {
                    // Skip header
                    reader.ReadLine();
                    
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(',');
                        
                        if (values.Length < 4)
                            continue;
                            
                        SystemConfigurationSO.SystemConfig config = new SystemConfigurationSO.SystemConfig
                        {
                            SystemType = values[0],
                            IsActive = bool.Parse(values[1]),
                            Priority = int.Parse(values[2]),
                            UpdateInterval = float.Parse(values[3])
                        };
                        
                        // Parse custom parameters if present
                        if (values.Length > 4 && !string.IsNullOrEmpty(values[4]))
                        {
                            string[] paramPairs = values[4].Split('|');
                            foreach (string pair in paramPairs)
                            {
                                string[] keyValue = pair.Split(':');
                                if (keyValue.Length == 2)
                                {
                                    config.CustomParameters[keyValue[0]] = keyValue[1];
                                }
                            }
                        }
                        
                        SystemConfigurationAsset.SystemConfigurations.Add(config);
                    }
                }
                
                EditorUtility.SetDirty(SystemConfigurationAsset);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Success", "CSV data imported to System Configuration asset.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error importing from CSV: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to import from CSV: {ex.Message}", "OK");
            }
        }
        
        [TabGroup("Sync", "Troop Configuration")]
        [BoxGroup("Sync/Troop Configuration/Source")]
        [LabelText("Troop Config SO")]
        [AssetsOnly, AssetSelector(Paths = "Assets")]
        public TroopConfigurationSO TroopConfigurationAsset;
        
        [BoxGroup("Sync/Troop Configuration/Source")]
        [LabelText("CSV Folder Path")]
        [FolderPath(RequireExistingPath = true)]
        public string TroopConfigCsvFolder = "Assets/Data/CSV";
        
        [BoxGroup("Sync/Troop Configuration/Source")]
        [LabelText("CSV File Name")]
        public string TroopConfigCsvFilename = "TroopConfiguration.csv";
        
        [BoxGroup("Sync/Troop Configuration/Actions")]
        [HorizontalGroup("Sync/Troop Configuration/Actions/Buttons")]
        [Button("Export SO to CSV"), GUIColor(0.4f, 0.8f, 0.8f)]
        public void ExportTroopConfigToCSV()
        {
            if (TroopConfigurationAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Troop Configuration asset first.", "OK");
                return;
            }
            
            try
            {
                // Ensure the directory exists
                Directory.CreateDirectory(TroopConfigCsvFolder);
                
                // Full path to the CSV file
                string fullPath = Path.Combine(TroopConfigCsvFolder, TroopConfigCsvFilename);
                
                using (StreamWriter writer = new StreamWriter(fullPath))
                {
                    // Write header
                    writer.WriteLine("UnitType,MaxHealth,MoveSpeed,RotationSpeed,AttackDamage,AttackRange,AttackCooldown,AggroRange,BehaviorWeights,FormationPreferences,CustomParameters");
                    
                    // Write data for each unit type
                    WriteTroopConfigToCSV(writer, TroopConfigurationAsset.InfantryConfig);
                    WriteTroopConfigToCSV(writer, TroopConfigurationAsset.ArcherConfig);
                    WriteTroopConfigToCSV(writer, TroopConfigurationAsset.PikeConfig);
                }
                
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Success", $"Troop Configuration exported to {fullPath}", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error exporting to CSV: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to export to CSV: {ex.Message}", "OK");
            }
        }
        
        private void WriteTroopConfigToCSV(StreamWriter writer, TroopConfigurationSO.TroopTypeConfig config)
        {
            string behaviorWeights = string.Join("|", config.BehaviorWeights
                .Select(kv => $"{kv.Key}:{kv.Value}"));
                
            string formationPrefs = string.Join("|", config.FormationPreferences
                .Select(kv => $"{kv.Key}:{kv.Value}"));
                
            string customParams = string.Join("|", config.CustomParameters
                .Select(kv => $"{kv.Key}:{kv.Value}"));
                
            writer.WriteLine($"{config.UnitType},{config.MaxHealth},{config.MoveSpeed},{config.RotationSpeed}," +
                            $"{config.AttackDamage},{config.AttackRange},{config.AttackCooldown},{config.AggroRange}," +
                            $"{behaviorWeights},{formationPrefs},{customParams}");
        }
        
        [HorizontalGroup("Sync/Troop Configuration/Actions/Buttons")]
        [Button("Import CSV to SO"), GUIColor(0.8f, 0.4f, 0.8f)]
        public void ImportTroopConfigFromCSV()
        {
            if (TroopConfigurationAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Troop Configuration asset first.", "OK");
                return;
            }
            
            try
            {
                // Full path to the CSV file
                string fullPath = Path.Combine(TroopConfigCsvFolder, TroopConfigCsvFilename);
                
                if (!File.Exists(fullPath))
                {
                    EditorUtility.DisplayDialog("Error", $"CSV file not found at {fullPath}", "OK");
                    return;
                }
                
                using (StreamReader reader = new StreamReader(fullPath))
                {
                    // Skip header
                    reader.ReadLine();
                    
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(',');
                        
                        if (values.Length < 8)
                            continue;
                            
                        UnitType unitType;
                        if (!Enum.TryParse(values[0], out unitType))
                            continue;
                            
                        var config = TroopConfigurationAsset.GetConfigForType(unitType);
                        
                        config.MaxHealth = float.Parse(values[1]);
                        config.MoveSpeed = float.Parse(values[2]);
                        config.RotationSpeed = float.Parse(values[3]);
                        config.AttackDamage = float.Parse(values[4]);
                        config.AttackRange = float.Parse(values[5]);
                        config.AttackCooldown = float.Parse(values[6]);
                        config.AggroRange = float.Parse(values[7]);
                        
                        // Parse behavior weights
                        if (values.Length > 8 && !string.IsNullOrEmpty(values[8]))
                        {
                            config.BehaviorWeights.Clear();
                            string[] weightPairs = values[8].Split('|');
                            foreach (string pair in weightPairs)
                            {
                                string[] keyValue = pair.Split(':');
                                if (keyValue.Length == 2)
                                {
                                    config.BehaviorWeights[keyValue[0]] = float.Parse(keyValue[1]);
                                }
                            }
                        }
                        
                        // Parse formation preferences
                        if (values.Length > 9 && !string.IsNullOrEmpty(values[9]))
                        {
                            config.FormationPreferences.Clear();
                            string[] prefPairs = values[9].Split('|');
                            foreach (string pair in prefPairs)
                            {
                                string[] keyValue = pair.Split(':');
                                if (keyValue.Length == 2)
                                {
                                    FormationType formationType;
                                    if (Enum.TryParse(keyValue[0], out formationType))
                                    {
                                        config.FormationPreferences[formationType] = float.Parse(keyValue[1]);
                                    }
                                }
                            }
                        }
                        
                        // Parse custom parameters
                        if (values.Length > 10 && !string.IsNullOrEmpty(values[10]))
                        {
                            config.CustomParameters.Clear();
                            string[] paramPairs = values[10].Split('|');
                            foreach (string pair in paramPairs)
                            {
                                string[] keyValue = pair.Split(':');
                                if (keyValue.Length == 2)
                                {
                                    config.CustomParameters[keyValue[0]] = keyValue[1];
                                }
                            }
                        }
                    }
                }
                
                EditorUtility.SetDirty(TroopConfigurationAsset);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Success", "CSV data imported to Troop Configuration asset.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error importing from CSV: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to import from CSV: {ex.Message}", "OK");
            }
        }
        
        [TabGroup("Batch", "Batch Processing")]
        [BoxGroup("Batch/Batch Processing/Settings")]
        [FolderPath(RequireExistingPath = true)]
        [LabelText("ScriptableObjects Folder")]
        public string ScriptableObjectsFolder = "Assets/ScriptableObjects/Configurations";
        
        [BoxGroup("Batch/Batch Processing/Settings")]
        [FolderPath(RequireExistingPath = true)]
        [LabelText("CSV Output Folder")]
        public string CsvOutputFolder = "Assets/Data/CSV";
        
        [BoxGroup("Batch/Batch Processing/Actions")]
        [Button("Batch Export All SOs to CSVs"), GUIColor(0.4f, 0.8f, 0.4f)]
        public void BatchExportAllToCSV()
        {
            try
            {
                // Ensure the output directory exists
                Directory.CreateDirectory(CsvOutputFolder);
                
                // Find all SystemConfigurationSO assets
                string[] systemConfigGuids = AssetDatabase.FindAssets("t:SystemConfigurationSO", new[] { ScriptableObjectsFolder });
                int systemConfigCount = 0;
                
                foreach (string guid in systemConfigGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    SystemConfigurationSO systemConfig = AssetDatabase.LoadAssetAtPath<SystemConfigurationSO>(assetPath);
                    
                    if (systemConfig != null)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(assetPath) + ".csv";
                        string outputPath = Path.Combine(CsvOutputFolder, fileName);
                        
                        SystemConfigurationAsset = systemConfig;
                        SystemConfigCsvFolder = CsvOutputFolder;
                        SystemConfigCsvFilename = fileName;
                        
                        ExportSystemConfigToCSV();
                        systemConfigCount++;
                    }
                }
                
                // Find all TroopConfigurationSO assets
                string[] troopConfigGuids = AssetDatabase.FindAssets("t:TroopConfigurationSO", new[] { ScriptableObjectsFolder });
                int troopConfigCount = 0;
                
                foreach (string guid in troopConfigGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    TroopConfigurationSO troopConfig = AssetDatabase.LoadAssetAtPath<TroopConfigurationSO>(assetPath);
                    
                    if (troopConfig != null)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(assetPath) + ".csv";
                        string outputPath = Path.Combine(CsvOutputFolder, fileName);
                        
                        TroopConfigurationAsset = troopConfig;
                        TroopConfigCsvFolder = CsvOutputFolder;
                        TroopConfigCsvFilename = fileName;
                        
                        ExportTroopConfigToCSV();
                        troopConfigCount++;
                    }
                }
                
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Batch Export Complete", 
                    $"Exported {systemConfigCount} System Configurations and {troopConfigCount} Troop Configurations to CSV.", 
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in batch export: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to complete batch export: {ex.Message}", "OK");
            }
        }
        
        [TabGroup("Batch", "Batch Processing")]
        [BoxGroup("Batch/Batch Processing/Actions")]
        [Button("Batch Import All CSVs to SOs"), GUIColor(0.8f, 0.4f, 0.8f)]
        public void BatchImportAllFromCSV()
        {
            try
            {
                // Find all CSV files in the CSV folder
                if (!Directory.Exists(CsvOutputFolder))
                {
                    EditorUtility.DisplayDialog("Error", $"CSV folder {CsvOutputFolder} does not exist.", "OK");
                    return;
                }
                
                string[] csvFiles = Directory.GetFiles(CsvOutputFolder, "*.csv");
                int importedCount = 0;
                
                foreach (string csvFile in csvFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(csvFile);
                    
                    // Check if there's a matching SO
                    string assetPath = Path.Combine(ScriptableObjectsFolder, fileName + ".asset");
                    if (!File.Exists(assetPath))
                    {
                        Debug.LogWarning($"No matching ScriptableObject found for {csvFile}");
                        continue;
                    }
                    
                    // Try to load as SystemConfigurationSO
                    SystemConfigurationSO systemConfig = AssetDatabase.LoadAssetAtPath<SystemConfigurationSO>(assetPath);
                    if (systemConfig != null)
                    {
                        SystemConfigurationAsset = systemConfig;
                        SystemConfigCsvFolder = Path.GetDirectoryName(csvFile);
                        SystemConfigCsvFilename = Path.GetFileName(csvFile);
                        
                        ImportSystemConfigFromCSV();
                        importedCount++;
                        continue;
                    }
                    
                    // Try to load as TroopConfigurationSO
                    TroopConfigurationSO troopConfig = AssetDatabase.LoadAssetAtPath<TroopConfigurationSO>(assetPath);
                    if (troopConfig != null)
                    {
                        TroopConfigurationAsset = troopConfig;
                        TroopConfigCsvFolder = Path.GetDirectoryName(csvFile);
                        TroopConfigCsvFilename = Path.GetFileName(csvFile);
                        
                        ImportTroopConfigFromCSV();
                        importedCount++;
                        continue;
                    }
                    
                    Debug.LogWarning($"Could not determine the type of ScriptableObject for {csvFile}");
                }
                
                EditorUtility.DisplayDialog("Batch Import Complete", 
                    $"Imported {importedCount} CSV files to ScriptableObjects.", 
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in batch import: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to complete batch import: {ex.Message}", "OK");
            }
        }
        
        [TabGroup("Auto", "Auto-Sync")]
        [BoxGroup("Auto/Auto-Sync/Settings")]
        [LabelText("Enable Auto-Sync")]
        [ToggleLeft]
        public bool EnableAutoSync = false;
        
        [BoxGroup("Auto/Auto-Sync/Settings")]
        [ShowIf("EnableAutoSync")]
        [LabelText("Auto-Sync Interval (minutes)")]
        [Range(1, 60)]
        public int AutoSyncInterval = 5;
        
        [BoxGroup("Auto/Auto-Sync/Settings")]
        [ShowIf("EnableAutoSync")]
        [LabelText("Last Sync Time")]
        [ReadOnly]
        public string LastSyncTime = "Never";
        
        [BoxGroup("Auto/Auto-Sync/Settings")]
        [ShowIf("EnableAutoSync")]
        [LabelText("Sync Direction")]
        [EnumToggleButtons]
        public SyncDirection Direction = SyncDirection.SOtoCSV;
        
        [BoxGroup("Auto/Auto-Sync/Actions")]
        [Button("Start Auto-Sync"), GUIColor(0.4f, 0.8f, 0.4f)]
        public void StartAutoSync()
        {
            // In an actual implementation, you would set up a file system watcher or EditorApplication.update
            // handler to periodically check for changes and sync files.
            
            // For this tutorial, we'll just demonstrate a manual sync
            if (Direction == SyncDirection.SOtoCSV)
            {
                BatchExportAllToCSV();
            }
            else
            {
                BatchImportAllFromCSV();
            }
            
            LastSyncTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            EditorUtility.DisplayDialog("Auto-Sync", 
                $"Auto-sync {(Direction == SyncDirection.SOtoCSV ? "SO to CSV" : "CSV to SO")} completed.", 
                "OK");
            
            // Note: In a real implementation, you would set up a timer or EditorApplication.update
            // to periodically check and sync based on the AutoSyncInterval
        }
        
        public enum SyncDirection
        {
            SOtoCSV,
            CSVtoSO
        }
    }
}