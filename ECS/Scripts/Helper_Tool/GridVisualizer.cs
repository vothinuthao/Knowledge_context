// GridVisualizer.cs

using Core.Grid;
using UnityEngine;

namespace Helper_Tool
{
    public class GridVisualizer : MonoBehaviour
    {
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private Material _gridMaterial;
        [SerializeField] private Color _gridColor = new Color(1, 1, 1, 0.2f);
    
        private void OnDrawGizmos()
        {
            if (_gridManager == null) return;
        
            Gizmos.color = _gridColor;
        
            for (int x = 0; x <= 20; x++)
            {
                Vector3 start = new Vector3(x * 3, 0, 0);
                Vector3 end = new Vector3(x * 3, 0, 20 * 3);
                Gizmos.DrawLine(start, end);
            }
        
            for (int y = 0; y <= 20; y++)
            {
                Vector3 start = new Vector3(0, 0, y * 3);
                Vector3 end = new Vector3(20 * 3, 0, y * 3);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}