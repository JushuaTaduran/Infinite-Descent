using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TileData", menuName = "ScriptableObjects/TileData", order = 1)]
public class TileData : ScriptableObject
{
    public List<Sprite> inputImages; // List of input sprites for texture generation
    public Sprite sprite; // The generated sprite to use for the tile
    public int sortingOrder; // Sorting order for the SpriteRenderer
    public string sortingLayerName; // Sorting layer for the SpriteRenderer
}