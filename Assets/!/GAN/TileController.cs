using UnityEngine;

public class TileController : MonoBehaviour
{
    public TileData tileData; // Reference to the ScriptableObject

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (tileData == null)
        {
            Debug.LogWarning($"TileData is not assigned for {gameObject.name}");
        }
        
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"No SpriteRenderer found in {gameObject.name}");
        }
        
        UpdateTile();
    }

    public void UpdateTile()
    {
        if (tileData == null)
        {
            Debug.LogWarning($"Cannot update tile. TileData is null for {gameObject.name}");
            return;
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"Cannot update tile. SpriteRenderer is null for {gameObject.name}");
            return;
        }

        if (tileData.sprite == null)
        {
            Debug.LogWarning($"Sprite is missing for TileData: {tileData.name}");
            return;
        }

        spriteRenderer.sprite = tileData.sprite;
        spriteRenderer.sortingOrder = tileData.sortingOrder;
        spriteRenderer.sortingLayerName = tileData.sortingLayerName;
    }
}