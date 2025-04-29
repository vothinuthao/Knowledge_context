using Core.Grid;
using UnityEngine;

public class CellVisual : MonoBehaviour
{
    [SerializeField] private int gridX;
    [SerializeField] private int gridZ;
    
    private Renderer _renderer;
    private Material _originalMaterial;
    
    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
            _originalMaterial = _renderer.material;
    }
    
    public void SetGridCoordinates(int x, int z)
    {
        gridX = x;
        gridZ = z;
        name = $"Tile_{x}_{z}";
    }
    
    private void OnMouseDown()
    {
        Debug.Log($"Clicked on Tile_{gridX}_{gridZ}");
        
        if (GridManager.IsInstanceValid())
        {
            GridManager.Instance.SelectTile(gridX, gridZ);
        }
    }
    
    private void OnMouseEnter()
    {
        GridManager.Instance.HighlightTile(gridX, gridZ, Color.white);
    }
    
    private void OnMouseExit()
    {
        GridManager.Instance.ResetTileColor(gridX, gridZ);
    }
}