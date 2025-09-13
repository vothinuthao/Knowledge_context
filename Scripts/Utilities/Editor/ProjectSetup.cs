#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace RavenDeckbuilding.Utilities.Editor
{
    public class ProjectSetup
{
    [MenuItem("Tools/Setup Project Structure")]
    public static void CreateFolderStructure()
    {
        string[] folders = {
            "_MainProject/Art/UI",
            "_MainProject/Art/Cards", 
            "_MainProject/Art/VFX",
            "_MainProject/Art/Materials",
            "_MainProject/Art/Textures",
            "_MainProject/Audio/SFX",
            "_MainProject/Audio/Music", 
            "_MainProject/Audio/UI",
            "_MainProject/Data/Cards",
            "_MainProject/Data/Players",
            "_MainProject/Data/Settings",
            "_MainProject/Prefabs/Cards",
            "_MainProject/Prefabs/UI",
            "_MainProject/Prefabs/VFX", 
            "_MainProject/Prefabs/Environment",
            "_MainProject/Scenes/Game",
            "_MainProject/Scenes/UI",
            "_MainProject/Scenes/Test",
            "_MainProject/Scripts/Core/Architecture",
            "_MainProject/Scripts/Core/Data",
            "_MainProject/Scripts/Core/Interfaces",
            "_MainProject/Scripts/Systems/Input",
            "_MainProject/Scripts/Systems/Commands",
            "_MainProject/Scripts/Systems/Cards", 
            "_MainProject/Scripts/Systems/Events",
            "_MainProject/Scripts/Systems/VFX",
            "_MainProject/Scripts/Gameplay/Player",
            "_MainProject/Scripts/Gameplay/Combat",
            "_MainProject/Scripts/Gameplay/Arena",
            "_MainProject/Scripts/UI/Menus",
            "_MainProject/Scripts/UI/HUD", 
            "_MainProject/Scripts/UI/Feedback",
            "_MainProject/Scripts/Utilities/Extensions",
            "_MainProject/Scripts/Utilities/Helpers",
            "_MainProject/Scripts/Utilities/Debug",
            "_MainProject/Scripts/Tests/EditMode",
            "_MainProject/Scripts/Tests/PlayMode"
        };
        
        foreach (string folder in folders)
        {
            string path = Path.Combine(Application.dataPath, folder);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        AssetDatabase.Refresh();
        Debug.Log("Project structure created successfully!");
    }
    }
}
#endif