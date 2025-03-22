using UnityEngine;

public class TileController : MonoBehaviour
{
    public TileData tileData; // Reference to the ScriptableObject

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        UpdateTile();
    }

    public void UpdateTile()
    {
        if (tileData != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = tileData.sprite;
            spriteRenderer.sortingOrder = tileData.sortingOrder;
            spriteRenderer.sortingLayerName = tileData.sortingLayerName;
        }
    }
}