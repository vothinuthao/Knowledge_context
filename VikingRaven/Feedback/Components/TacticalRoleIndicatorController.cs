using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VikingRaven.Combat.Components;

namespace VikingRaven.Feedback.Components
{
    public class TacticalRoleIndicatorController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _roleText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _objectiveIcon;
        
        private TacticalComponent _tacticalComponent;
        private TacticalRole _lastRole;
        private TacticalObjective _lastObjective;
        
        private Dictionary<TacticalRole, Color> _roleColors = new Dictionary<TacticalRole, Color>
        {
            { TacticalRole.Frontline, new Color(0.7f, 0.2f, 0.2f, 0.7f) },      // Red
            { TacticalRole.Support, new Color(0.2f, 0.7f, 0.2f, 0.7f) },        // Green
            { TacticalRole.Flanker, new Color(0.7f, 0.5f, 0.2f, 0.7f) },        // Orange
            { TacticalRole.Defender, new Color(0.2f, 0.2f, 0.7f, 0.7f) },       // Blue
            { TacticalRole.Scout, new Color(0.4f, 0.3f, 0.7f, 0.7f) }           // Purple
        };
        
        private Dictionary<TacticalObjective, Sprite> _objectiveIcons;
        
        private void Awake()
        {
            // Load objective icons (in a real implementation, these would be proper icons)
            _objectiveIcons = new Dictionary<TacticalObjective, Sprite>();
            
            // This is a simplified approach - in a real game you'd load actual icon sprites
            // These placeholder shapes are just for demonstration
            CreatePlaceholderIcons();
        }
        
        private void CreatePlaceholderIcons()
        {
            // Create simple shapes as placeholders for icons
            // In a real implementation, you'd load these from resources
            
            // Attack - Red Triangle
            Texture2D attackTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            DrawTriangle(attackTex, Color.red);
            Sprite attackSprite = Sprite.Create(attackTex, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
            _objectiveIcons[TacticalObjective.Attack] = attackSprite;
            
            // Defend - Blue Shield
            Texture2D defendTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            DrawShield(defendTex, Color.blue);
            Sprite defendSprite = Sprite.Create(defendTex, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
            _objectiveIcons[TacticalObjective.Defend] = defendSprite;
            
            // Move - Green Arrow
            Texture2D moveTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            DrawArrow(moveTex, Color.green);
            Sprite moveSprite = Sprite.Create(moveTex, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
            _objectiveIcons[TacticalObjective.Move] = moveSprite;
            
            // Hold - Yellow Circle
            Texture2D holdTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            DrawCircle(holdTex, Color.yellow);
            Sprite holdSprite = Sprite.Create(holdTex, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
            _objectiveIcons[TacticalObjective.Hold] = holdSprite;
            
            // Retreat - Purple Arrow Reversed
            Texture2D retreatTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            DrawArrow(retreatTex, new Color(0.7f, 0.2f, 0.7f), true);
            Sprite retreatSprite = Sprite.Create(retreatTex, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
            _objectiveIcons[TacticalObjective.Retreat] = retreatSprite;
            
            // Scout - Orange Star
            Texture2D scoutTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            DrawStar(scoutTex, new Color(1f, 0.6f, 0f));
            Sprite scoutSprite = Sprite.Create(scoutTex, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
            _objectiveIcons[TacticalObjective.Scout] = scoutSprite;
        }
        
        // Simple shape drawing methods for placeholder icons
        private void DrawTriangle(Texture2D tex, Color color)
        {
            Color[] pixels = new Color[tex.width * tex.height];
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    int index = y * tex.width + x;
                    // Simple triangle shape
                    if (y >= tex.height/2 - x/2 && y >= tex.height/2 - (tex.width-x)/2)
                    {
                        pixels[index] = color;
                    }
                    else
                    {
                        pixels[index] = Color.clear;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
        }
        
        private void DrawShield(Texture2D tex, Color color)
        {
            Color[] pixels = new Color[tex.width * tex.height];
            int centerX = tex.width / 2;
            int centerY = tex.height / 2;
            
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    int index = y * tex.width + x;
                    
                    // Shield shape
                    float xRatio = (float)(x - centerX) / (tex.width / 2);
                    float yRatio = (float)(y - centerY) / (tex.height / 2);
                    
                    if (y < centerY && Mathf.Abs(xRatio) < 0.7f - 0.3f * (y / (float)centerY))
                    {
                        pixels[index] = color;
                    }
                    else if (y >= centerY && Mathf.Abs(xRatio) < 0.7f * (1 - (y - centerY) / (float)centerY))
                    {
                        pixels[index] = color;
                    }
                    else
                    {
                        pixels[index] = Color.clear;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
        }
        
        private void DrawArrow(Texture2D tex, Color color, bool reversed = false)
        {
            Color[] pixels = new Color[tex.width * tex.height];
            
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    int index = y * tex.width + x;
                    
                    // Arrow shape
                    int arrowX = reversed ? tex.width - 1 - x : x;
                    
                    if (y > tex.height/4 && y < tex.height*3/4 && arrowX > tex.width/4 && arrowX < tex.width*3/4)
                    {
                        pixels[index] = color;
                    }
                    else if (arrowX > tex.width/2 && 
                             y > tex.height/2 - (arrowX - tex.width/2) && 
                             y < tex.height/2 + (arrowX - tex.width/2))
                    {
                        pixels[index] = color;
                    }
                    else
                    {
                        pixels[index] = Color.clear;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
        }
        
        private void DrawCircle(Texture2D tex, Color color)
        {
            Color[] pixels = new Color[tex.width * tex.height];
            int centerX = tex.width / 2;
            int centerY = tex.height / 2;
            float radius = tex.width / 2 * 0.8f;
            
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    int index = y * tex.width + x;
                    
                    // Circle shape
                    float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    
                    if (distance < radius)
                    {
                        pixels[index] = color;
                    }
                    else
                    {
                        pixels[index] = Color.clear;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
        }
        
        private void DrawStar(Texture2D tex, Color color)
        {
            Color[] pixels = new Color[tex.width * tex.height];
            int centerX = tex.width / 2;
            int centerY = tex.height / 2;
            
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    int index = y * tex.width + x;
                    pixels[index] = Color.clear;
                }
            }
            
            // Draw a simple star (simplified algorithm)
            int numPoints = 5;
            float outerRadius = tex.width / 2 * 0.8f;
            float innerRadius = outerRadius * 0.4f;
            
            for (int i = 0; i < numPoints * 2; i++)
            {
                float angle = i * Mathf.PI / numPoints;
                float radius = (i % 2 == 0) ? outerRadius : innerRadius;
                
                int x = centerX + Mathf.RoundToInt(radius * Mathf.Sin(angle));
                int y = centerY + Mathf.RoundToInt(radius * Mathf.Cos(angle));
                
                // Draw lines from center to point
                DrawLine(tex, pixels, centerX, centerY, x, y, color);
                
                // Connect to next point
                if (i > 0)
                {
                    int prevX = centerX + Mathf.RoundToInt(((i-1) % 2 == 0 ? outerRadius : innerRadius) * Mathf.Sin((i-1) * Mathf.PI / numPoints));
                    int prevY = centerY + Mathf.RoundToInt(((i-1) % 2 == 0 ? outerRadius : innerRadius) * Mathf.Cos((i-1) * Mathf.PI / numPoints));
                    DrawLine(tex, pixels, prevX, prevY, x, y, color);
                }
                
                // Connect last to first
                if (i == numPoints * 2 - 1)
                {
                    int firstX = centerX + Mathf.RoundToInt(outerRadius * Mathf.Sin(0));
                    int firstY = centerY + Mathf.RoundToInt(outerRadius * Mathf.Cos(0));
                    DrawLine(tex, pixels, x, y, firstX, firstY, color);
                }
            }
            
            tex.SetPixels(pixels);
            tex.Apply();
        }
        
        private void DrawLine(Texture2D tex, Color[] pixels, int x0, int y0, int x1, int y1, Color color)
        {
            // Simple Bresenham's line algorithm
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = (x0 < x1) ? 1 : -1;
            int sy = (y0 < y1) ? 1 : -1;
            int err = dx - dy;
            
            while (true)
            {
                // Check bounds
                if (x0 >= 0 && x0 < tex.width && y0 >= 0 && y0 < tex.height)
                {
                    pixels[y0 * tex.width + x0] = color;
                }
                
                if (x0 == x1 && y0 == y1)
                    break;
                    
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
        
        public void SetTacticalComponent(TacticalComponent tacticalComponent)
        {
            _tacticalComponent = tacticalComponent;
            _lastRole = _tacticalComponent.AssignedRole;
            _lastObjective = _tacticalComponent.CurrentObjective;
            
            // Update initial state
            UpdateRoleIndicator();
            UpdateObjectiveIndicator();
        }
        
        private void Update()
        {
            if (_tacticalComponent != null)
            {
                if (_tacticalComponent.AssignedRole != _lastRole)
                {
                    _lastRole = _tacticalComponent.AssignedRole;
                    UpdateRoleIndicator();
                }
                
                if (_tacticalComponent.CurrentObjective != _lastObjective)
                {
                    _lastObjective = _tacticalComponent.CurrentObjective;
                    UpdateObjectiveIndicator();
                }
            }
        }
        
        private void UpdateRoleIndicator()
        {
            if (_roleText != null)
            {
                _roleText.text = _lastRole.ToString();
                
                // Update color based on role
                if (_backgroundImage != null)
                {
                    if (_roleColors.TryGetValue(_lastRole, out Color color))
                    {
                        _backgroundImage.color = color;
                    }
                    else
                    {
                        _backgroundImage.color = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Default gray
                    }
                }
            }
        }
        
        private void UpdateObjectiveIndicator()
        {
            if (_objectiveIcon != null)
            {
                if (_objectiveIcons.TryGetValue(_lastObjective, out Sprite icon))
                {
                    _objectiveIcon.sprite = icon;
                    _objectiveIcon.enabled = true;
                }
                else
                {
                    _objectiveIcon.enabled = false;
                }
            }
        }
    }
}