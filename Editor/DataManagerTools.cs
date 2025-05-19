using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;

namespace Editor
{
    /// <summary>
    /// Cung cấp các tiện ích cho việc quản lý dữ liệu trong Editor
    /// </summary>
    public class DataManagerTools
    {
        private const string UNIT_DATA_PATH = "Assets/Resources/Units/Data";
        private const string SQUAD_DATA_PATH = "Assets/Resources/Squads/Data";
        
        /// <summary>
        /// Tạo thư mục cần thiết cho hệ thống dữ liệu
        /// </summary>
        [MenuItem("Tools/VikingRaven/Create Data Folders")]
        public static void CreateDataFolders()
        {
            // Tạo thư mục Units/Data nếu chưa tồn tại
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Units"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Units");
            }
            
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Units/Data"))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Units", "Data");
            }
            
            // Tạo thư mục Squads/Data nếu chưa tồn tại
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Squads"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Squads");
            }
            
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Squads/Data"))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Squads", "Data");
            }
            
            Debug.Log("DataManagerTools: Data folders created successfully!");
        }
        
        /// <summary>
        /// Gán ID cho tất cả UnitDataSO và SquadDataSO
        /// </summary>
        [MenuItem("Tools/VikingRaven/Assign IDs to All Data")]
        public static void AssignIDsToAllData()
        {
            AssignIDsToUnitData();
            AssignIDsToSquadData();
            
            Debug.Log("DataManagerTools: IDs assigned to all Unit and Squad data!");
        }
        
        /// <summary>
        /// Gán ID cho tất cả UnitDataSO
        /// </summary>
        private static void AssignIDsToUnitData()
        {
            // Tìm tất cả UnitDataSO trong project
            string[] guids = AssetDatabase.FindAssets("t:UnitDataSO");
            int assignedCount = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnitDataSO unitData = AssetDatabase.LoadAssetAtPath<UnitDataSO>(path);
                
                if (unitData != null)
                {
                    // Kiểm tra nếu ID rỗng hoặc null
                    if (string.IsNullOrEmpty(unitData.UnitId))
                    {
                        // Lấy tên file và đổi thành snake_case để dùng làm ID
                        string fileName = Path.GetFileNameWithoutExtension(path);
                        string unitId = fileName.Replace(" ", "_").ToLower();
                        
                        // Sử dụng Reflection để đặt giá trị cho trường _unitId
                        var field = typeof(UnitDataSO).GetField("_unitId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(unitData, unitId);
                            EditorUtility.SetDirty(unitData);
                            assignedCount++;
                        }
                    }
                }
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log($"DataManagerTools: Assigned IDs to {assignedCount} Unit data assets");
        }
        
        /// <summary>
        /// Gán ID cho tất cả SquadDataSO
        /// </summary>
        private static void AssignIDsToSquadData()
        {
            // Tìm tất cả SquadDataSO trong project
            string[] guids = AssetDatabase.FindAssets("t:SquadDataSO");
            int assignedCount = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SquadDataSO squadData = AssetDatabase.LoadAssetAtPath<SquadDataSO>(path);
                
                if (squadData != null)
                {
                    // Kiểm tra nếu ID rỗng hoặc null
                    if (string.IsNullOrEmpty(squadData.SquadId))
                    {
                        // Lấy tên file và đổi thành snake_case để dùng làm ID
                        string fileName = Path.GetFileNameWithoutExtension(path);
                        string squadId = fileName.Replace(" ", "_").ToLower();
                        
                        // Sử dụng Reflection để đặt giá trị cho trường _squadId
                        var field = typeof(SquadDataSO).GetField("_squadId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(squadData, squadId);
                            EditorUtility.SetDirty(squadData);
                            assignedCount++;
                        }
                    }
                }
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log($"DataManagerTools: Assigned IDs to {assignedCount} Squad data assets");
        }
        
        /// <summary>
        /// Di chuyển tất cả UnitDataSO và SquadDataSO vào thư mục Resources
        /// </summary>
        [MenuItem("Tools/VikingRaven/Move Data to Resources")]
        public static void MoveDataToResources()
        {
            // Tạo thư mục nếu cần
            CreateDataFolders();
            
            MoveUnitDataToResources();
            MoveSquadDataToResources();
            
            Debug.Log("DataManagerTools: All data moved to Resources folders!");
        }
        
        /// <summary>
        /// Di chuyển UnitDataSO vào thư mục Resources
        /// </summary>
        private static void MoveUnitDataToResources()
        {
            string[] guids = AssetDatabase.FindAssets("t:UnitDataSO");
            int movedCount = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Bỏ qua nếu đã ở trong thư mục Resources
                if (path.StartsWith(UNIT_DATA_PATH))
                    continue;
                
                UnitDataSO unitData = AssetDatabase.LoadAssetAtPath<UnitDataSO>(path);
                if (unitData != null)
                {
                    // Xác định đường dẫn đích
                    string fileName = Path.GetFileName(path);
                    string targetPath = Path.Combine(UNIT_DATA_PATH, fileName);
                    
                    // Di chuyển asset
                    AssetDatabase.MoveAsset(path, targetPath);
                    movedCount++;
                }
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log($"DataManagerTools: Moved {movedCount} Unit data assets to Resources");
        }
        
        /// <summary>
        /// Di chuyển SquadDataSO vào thư mục Resources
        /// </summary>
        private static void MoveSquadDataToResources()
        {
            string[] guids = AssetDatabase.FindAssets("t:SquadDataSO");
            int movedCount = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Bỏ qua nếu đã ở trong thư mục Resources
                if (path.StartsWith(SQUAD_DATA_PATH))
                    continue;
                
                SquadDataSO squadData = AssetDatabase.LoadAssetAtPath<SquadDataSO>(path);
                if (squadData != null)
                {
                    // Xác định đường dẫn đích
                    string fileName = Path.GetFileName(path);
                    string targetPath = Path.Combine(SQUAD_DATA_PATH, fileName);
                    
                    // Di chuyển asset
                    AssetDatabase.MoveAsset(path, targetPath);
                    movedCount++;
                }
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log($"DataManagerTools: Moved {movedCount} Squad data assets to Resources");
        }
        
        /// <summary>
        /// Tạo mẫu UnitDataSO cho mỗi loại Unit
        /// </summary>
        [MenuItem("Tools/VikingRaven/Create Unit Data Templates")]
        public static void CreateUnitDataTemplates()
        {
            CreateDataFolders();
            
            CreateUnitDataTemplate("Infantry_Template", UnitType.Infantry);
            CreateUnitDataTemplate("Archer_Template", UnitType.Archer);
            CreateUnitDataTemplate("Pike_Template", UnitType.Pike);
            
            Debug.Log("DataManagerTools: Unit data templates created successfully!");
        }
        
        /// <summary>
        /// Tạo mẫu UnitDataSO
        /// </summary>
        private static void CreateUnitDataTemplate(string name, UnitType unitType)
        {
            // Tạo asset mới
            UnitDataSO unitData = ScriptableObject.CreateInstance<UnitDataSO>();
            
            // Đặt các thuộc tính cơ bản
            var nameField = typeof(UnitDataSO).GetField("_displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var idField = typeof(UnitDataSO).GetField("_unitId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descField = typeof(UnitDataSO).GetField("_description", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var typeField = typeof(UnitDataSO).GetField("_unitType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (nameField != null) nameField.SetValue(unitData, unitType.ToString() + " Template");
            if (idField != null) idField.SetValue(unitData, name.ToLower());
            if (descField != null) descField.SetValue(unitData, "Template for " + unitType.ToString() + " units");
            if (typeField != null) typeField.SetValue(unitData, unitType);
            
            // Lưu asset
            string assetPath = Path.Combine(UNIT_DATA_PATH, name + ".asset");
            AssetDatabase.CreateAsset(unitData, assetPath);
            AssetDatabase.SaveAssets();
            
            // Chọn asset mới tạo trong Editor
            Selection.activeObject = unitData;
        }
        
        /// <summary>
        /// Tạo mẫu SquadDataSO
        /// </summary>
        [MenuItem("Tools/VikingRaven/Create Squad Data Templates")]
        public static void CreateSquadDataTemplates()
        {
            CreateDataFolders();
            
            // Tạo Squad template cho từng faction
            CreateSquadDataTemplate("Infantry_Squad_Player", "Player", UnitType.Infantry, 9);
            CreateSquadDataTemplate("Archer_Squad_Player", "Player", UnitType.Archer, 6);
            CreateSquadDataTemplate("Mixed_Squad_Player", "Player", new Dictionary<UnitType, int> {
                { UnitType.Infantry, 4 },
                { UnitType.Archer, 3 },
                { UnitType.Pike, 2 }
            });
            
            CreateSquadDataTemplate("Infantry_Squad_Enemy", "Enemy", UnitType.Infantry, 9);
            CreateSquadDataTemplate("Archer_Squad_Enemy", "Enemy", UnitType.Archer, 6);
            
            Debug.Log("DataManagerTools: Squad data templates created successfully!");
        }
        
        /// <summary>
        /// Tạo mẫu SquadDataSO với một loại đơn vị
        /// </summary>
        private static void CreateSquadDataTemplate(string name, string faction, UnitType mainUnitType, int count)
        {
            // Tạo asset mới
            SquadDataSO squadData = ScriptableObject.CreateInstance<SquadDataSO>();
            
            // Đặt các thuộc tính cơ bản
            var nameField = typeof(SquadDataSO).GetField("_displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var idField = typeof(SquadDataSO).GetField("_squadId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descField = typeof(SquadDataSO).GetField("_description", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var factionField = typeof(SquadDataSO).GetField("_faction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (nameField != null) nameField.SetValue(squadData, name.Replace("_", " "));
            if (idField != null) idField.SetValue(squadData, name.ToLower());
            if (descField != null) descField.SetValue(squadData, "Template for " + mainUnitType.ToString() + " squad");
            if (factionField != null) factionField.SetValue(squadData, faction);
            
            // Tìm UnitDataSO cho loại đơn vị này
            UnitDataSO unitData = FindUnitDataTemplate(mainUnitType);
            
            // Thêm unit composition nếu tìm thấy unitData
            if (unitData != null)
            {
                // Sẽ cần thực hiện trong cách khác vì UnitComposition là class phức tạp
                // Đây chỉ là mẫu code, cần sửa đổi để hoạt động thực tế
            }
            
            // Lưu asset
            string assetPath = Path.Combine(SQUAD_DATA_PATH, name + ".asset");
            AssetDatabase.CreateAsset(squadData, assetPath);
            AssetDatabase.SaveAssets();
            
            // Chọn asset mới tạo trong Editor
            Selection.activeObject = squadData;
        }
        
        /// <summary>
        /// Tạo mẫu SquadDataSO với nhiều loại đơn vị
        /// </summary>
        private static void CreateSquadDataTemplate(string name, string faction, Dictionary<UnitType, int> unitCounts)
        {
            // Tạo asset mới
            SquadDataSO squadData = ScriptableObject.CreateInstance<SquadDataSO>();
            
            // Đặt các thuộc tính cơ bản
            var nameField = typeof(SquadDataSO).GetField("_displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var idField = typeof(SquadDataSO).GetField("_squadId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descField = typeof(SquadDataSO).GetField("_description", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var factionField = typeof(SquadDataSO).GetField("_faction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (nameField != null) nameField.SetValue(squadData, name.Replace("_", " "));
            if (idField != null) idField.SetValue(squadData, name.ToLower());
            if (descField != null) descField.SetValue(squadData, "Mixed unit squad template");
            if (factionField != null) factionField.SetValue(squadData, faction);
            
            // Thêm unit composition sẽ cần thực hiện theo cách khác
            // Đây chỉ là mẫu code, cần sửa đổi để hoạt động thực tế
            
            // Lưu asset
            string assetPath = Path.Combine(SQUAD_DATA_PATH, name + ".asset");
            AssetDatabase.CreateAsset(squadData, assetPath);
            AssetDatabase.SaveAssets();
            
            // Chọn asset mới tạo trong Editor
            Selection.activeObject = squadData;
        }
        
        /// <summary>
        /// Tìm UnitDataSO template cho một loại đơn vị
        /// </summary>
        private static UnitDataSO FindUnitDataTemplate(UnitType unitType)
        {
            string[] guids = AssetDatabase.FindAssets("t:UnitDataSO", new[] { UNIT_DATA_PATH });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnitDataSO unitData = AssetDatabase.LoadAssetAtPath<UnitDataSO>(path);
                
                if (unitData != null && unitData.UnitType == unitType)
                {
                    return unitData;
                }
            }
            
            return null;
        }
    }
}