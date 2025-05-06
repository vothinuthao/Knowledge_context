using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VikingRaven.Feedback.Components;

namespace VikingRaven.Game
{
    public class SimplePrefabCreator : MonoBehaviour
    {
        public static GameObject CreateHealthBarPrefab()
        {
            var prefab = new GameObject("HealthBarPrefab");
            
            // Add a Canvas
            var canvas = prefab.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Add a CanvasScaler
            prefab.AddComponent<CanvasScaler>();
            
            // Add health bar background
            var background = new GameObject("Background");
            background.transform.SetParent(prefab.transform);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            backgroundImage.rectTransform.sizeDelta = new Vector2(100, 20);
            
            // Add health bar fill
            var fill = new GameObject("Fill");
            fill.transform.SetParent(background.transform);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = Color.green;
            fillImage.rectTransform.sizeDelta = new Vector2(100, 20);
            fillImage.rectTransform.anchorMin = Vector2.zero;
            fillImage.rectTransform.anchorMax = Vector2.one;
            fillImage.rectTransform.offsetMin = Vector2.zero;
            fillImage.rectTransform.offsetMax = Vector2.zero;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 1f;
            
            // Add damage indicator
            var damage = new GameObject("DamageIndicator");
            damage.transform.SetParent(background.transform);
            var damageImage = damage.AddComponent<Image>();
            damageImage.color = Color.red;
            damageImage.rectTransform.sizeDelta = new Vector2(100, 20);
            damageImage.rectTransform.anchorMin = Vector2.zero;
            damageImage.rectTransform.anchorMax = Vector2.one;
            damageImage.rectTransform.offsetMin = Vector2.zero;
            damageImage.rectTransform.offsetMax = Vector2.zero;
            damageImage.type = Image.Type.Filled;
            damageImage.fillMethod = Image.FillMethod.Horizontal;
            damageImage.fillOrigin = 0;
            damageImage.fillAmount = 1f;
            
            // Add controller
            var controller = prefab.AddComponent<HealthBarController>();
            
            // Set references
            var healthFillField = controller.GetType().GetField("_healthFill", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (healthFillField != null)
                healthFillField.SetValue(controller, fillImage);
                
            var damageIndicatorField = controller.GetType().GetField("_damageIndicator", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageIndicatorField != null)
                damageIndicatorField.SetValue(controller, damageImage);
            
            return prefab;
        }
        
        public static GameObject CreateStateIndicatorPrefab()
        {
            var prefab = new GameObject("StateIndicatorPrefab");
            
            // Add a Canvas
            var canvas = prefab.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Add a CanvasScaler
            prefab.AddComponent<CanvasScaler>();
            
            // Add background
            var background = new GameObject("Background");
            background.transform.SetParent(prefab.transform);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.8f, 0.7f);
            backgroundImage.rectTransform.sizeDelta = new Vector2(100, 30);
            
            // Add text
            var text = new GameObject("Text");
            text.transform.SetParent(background.transform);
            var tmp = text.AddComponent<TextMeshProUGUI>();
            tmp.text = "Idle";
            tmp.color = Color.white;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(100, 30);
            tmp.rectTransform.anchorMin = Vector2.zero;
            tmp.rectTransform.anchorMax = Vector2.one;
            tmp.rectTransform.offsetMin = Vector2.zero;
            tmp.rectTransform.offsetMax = Vector2.zero;
            
            // Add controller
            var controller = prefab.AddComponent<StateIndicatorController>();
            
            // Set references
            var stateTextField = controller.GetType().GetField("_stateText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (stateTextField != null)
                stateTextField.SetValue(controller, tmp);
                
            var backgroundImageField = controller.GetType().GetField("_backgroundImage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (backgroundImageField != null)
                backgroundImageField.SetValue(controller, backgroundImage);
            
            return prefab;
        }
        
        public static GameObject CreateBehaviorIndicatorPrefab()
        {
            var prefab = new GameObject("BehaviorIndicatorPrefab");
            
            // Add a Canvas
            var canvas = prefab.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Add a CanvasScaler
            prefab.AddComponent<CanvasScaler>();
            
            // Add background
            var background = new GameObject("Background");
            background.transform.SetParent(prefab.transform);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.7f, 0.2f, 0.7f);
            backgroundImage.rectTransform.sizeDelta = new Vector2(100, 30);
            
            // Add text
            var text = new GameObject("Text");
            text.transform.SetParent(background.transform);
            var tmp = text.AddComponent<TextMeshProUGUI>();
            tmp.text = "Move";
            tmp.color = Color.white;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(100, 30);
            tmp.rectTransform.anchorMin = Vector2.zero;
            tmp.rectTransform.anchorMax = Vector2.one;
            tmp.rectTransform.offsetMin = Vector2.zero;
            tmp.rectTransform.offsetMax = Vector2.zero;
            
            // Add controller
            var controller = prefab.AddComponent<BehaviorIndicatorController>();
            
            // Set references
            var behaviorTextField = controller.GetType().GetField("_behaviorText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (behaviorTextField != null)
                behaviorTextField.SetValue(controller, tmp);
                
            var backgroundImageField = controller.GetType().GetField("_backgroundImage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (backgroundImageField != null)
                backgroundImageField.SetValue(controller, backgroundImage);
            
            return prefab;
        }
        
        public static GameObject CreateTacticalRoleIndicatorPrefab()
        {
            var prefab = new GameObject("TacticalRoleIndicatorPrefab");
            
            // Add a Canvas
            var canvas = prefab.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Add a CanvasScaler
            prefab.AddComponent<CanvasScaler>();
            
            // Add background
            var background = new GameObject("Background");
            background.transform.SetParent(prefab.transform);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.7f, 0.2f, 0.2f, 0.7f);
            backgroundImage.rectTransform.sizeDelta = new Vector2(120, 30);
            
            // Add text
            var text = new GameObject("Text");
            text.transform.SetParent(background.transform);
            var tmp = text.AddComponent<TextMeshProUGUI>();
            tmp.text = "Frontline";
            tmp.color = Color.white;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(90, 30);
            tmp.rectTransform.anchorMin = new Vector2(0, 0);
            tmp.rectTransform.anchorMax = new Vector2(0.75f, 1);
            tmp.rectTransform.offsetMin = Vector2.zero;
            tmp.rectTransform.offsetMax = Vector2.zero;
            
            // Add objective icon
            var icon = new GameObject("ObjectiveIcon");
            icon.transform.SetParent(background.transform);
            var iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.rectTransform.sizeDelta = new Vector2(30, 30);
            iconImage.rectTransform.anchorMin = new Vector2(0.75f, 0);
            iconImage.rectTransform.anchorMax = new Vector2(1, 1);
            iconImage.rectTransform.offsetMin = Vector2.zero;
            iconImage.rectTransform.offsetMax = Vector2.zero;
            
            // Add controller
            var controller = prefab.AddComponent<TacticalRoleIndicatorController>();
            
            // Set references
            var roleTextField = controller.GetType().GetField("_roleText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (roleTextField != null)
                roleTextField.SetValue(controller, tmp);
                
            var backgroundImageField = controller.GetType().GetField("_backgroundImage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (backgroundImageField != null)
                backgroundImageField.SetValue(controller, backgroundImage);
                
            var objectiveIconField = controller.GetType().GetField("_objectiveIcon", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (objectiveIconField != null)
                objectiveIconField.SetValue(controller, iconImage);
            
            return prefab;
        }
    }
}