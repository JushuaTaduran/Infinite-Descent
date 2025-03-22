using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro; // Required for TextMeshPro

public class GEDWFCTileGeneration : MonoBehaviour
{
    public int roomWidth;
    public int roomHeight;
    public Tile[] tileObjects;
    public List<Cell> gridComponents;
    public Cell cellObj;

    private RoomManager roomManager;
    private MixRoomManager mixRoomManager;
    private GEDRoomManager gedRoomManager;
    public int iterations = 0;

    // Reference to TextMeshPro UI element
    public TextMeshProUGUI tileCountText;

    void Awake()
    {
        gridComponents = new List<Cell>();
        roomManager = FindObjectOfType<RoomManager>();
        mixRoomManager = FindObjectOfType<MixRoomManager>();
        gedRoomManager = FindObjectOfType<GEDRoomManager>();

        if (roomManager == null && mixRoomManager == null && gedRoomManager == null)
        {
            Debug.LogError("❌ Neither RoomManager nor MixRoomManager found in the scene!");
            return;
        }

        StartCoroutine(WaitForGenerationComplete());
    }

    IEnumerator WaitForGenerationComplete()
    {
        bool generationComplete = false;

        while (!generationComplete)
        {
            if (roomManager != null)
            {
                generationComplete = roomManager.generationComplete;
            }
            else if (mixRoomManager != null)
            {
                generationComplete = mixRoomManager.generationComplete;
            }
            else if (gedRoomManager != null)
            {
                generationComplete = gedRoomManager.generationComplete;
            }

            yield return null;
        }

        InitializeGrid();
    }

    void InitializeGrid()
    {
        Vector2 startPosition = (Vector2)transform.position + new Vector2(0.5f, 0.5f);

        for (int y = 0; y < roomHeight; y++)
        {
            for (int x = 0; x < roomWidth; x++)
            {
                Vector2 cellPosition = startPosition + new Vector2(x, y);
                Cell newCell = Instantiate(cellObj, cellPosition, Quaternion.identity);
                newCell.CreateCell(false, tileObjects);

                SpriteRenderer spriteRenderer = newCell.GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sortingOrder = 1;
                    spriteRenderer.sortingLayerName = "Default";
                }

                gridComponents.Add(newCell);
            }
        }

        StartCoroutine(CheckEntropy());
    }

    public void ClearGrid()
    {
        foreach (var cell in gridComponents)
        {
            if (cell != null)
            {
                Destroy(cell.gameObject);
            }
        }
        gridComponents.Clear();
        iterations = 0;
    }

    IEnumerator CheckEntropy()
    {
        List<Cell> tempGrid = new List<Cell>(gridComponents);

        tempGrid.RemoveAll(c => c.collapsed);

        tempGrid.Sort((a, b) => { return a.tileOptions.Length - b.tileOptions.Length; });

        int arrLength = tempGrid[0].tileOptions.Length;
        int stopIndex = default;

        for (int i = 1; i < tempGrid.Count; i++)
        {
            if (tempGrid[i].tileOptions.Length > arrLength)
            {
                stopIndex = i;
                break;
            }
        }

        if (stopIndex > 0)
        {
            tempGrid.RemoveRange(stopIndex, tempGrid.Count - stopIndex);
        }

        yield return new WaitForSeconds(0.01f);

        CollapseCell(tempGrid);
    }

    void CollapseCell(List<Cell> tempGrid)
    {
        int randIndex = UnityEngine.Random.Range(0, tempGrid.Count);

        Cell cellToCollapse = tempGrid[randIndex];

        cellToCollapse.collapsed = true;
        Tile selectedTile = cellToCollapse.tileOptions[UnityEngine.Random.Range(0, cellToCollapse.tileOptions.Length)];
        cellToCollapse.tileOptions = new Tile[] { selectedTile };

        Tile foundTile = cellToCollapse.tileOptions[0];
        Instantiate(foundTile, cellToCollapse.transform.position, Quaternion.identity);

        UpdateGeneration();
    }

    void UpdateGeneration()
    {
        List<Cell> newGenerationCell = new List<Cell>(gridComponents);

        for (int y = 0; y < roomHeight; y++)
        {
            for (int x = 0; x < roomWidth; x++)
            {
                var index = x + y * roomWidth;
                if (gridComponents[index].collapsed)
                {
                    Debug.Log("called");
                    newGenerationCell[index] = gridComponents[index];
                }
                else
                {
                    List<Tile> options = new List<Tile>();
                    foreach (Tile t in tileObjects)
                    {
                        options.Add(t);
                    }

                    if (y > 0)
                    {
                        Cell up = gridComponents[x + (y - 1) * roomWidth];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (Tile possibleOptions in up.tileOptions)
                        {
                            var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[valOption].upNeighbours;

                            validOptions.AddRange(valid);
                        }

                        CheckValidity(options, validOptions);
                    }

                    if (x < roomWidth - 1)
                    {
                        Cell right = gridComponents[x + 1 + y * roomWidth];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (Tile possibleOptions in right.tileOptions)
                        {
                            var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[valOption].leftNeighbours;

                            validOptions.AddRange(valid);
                        }

                        CheckValidity(options, validOptions);
                    }

                    if (y < roomHeight - 1)
                    {
                        Cell down = gridComponents[x + (y + 1) * roomWidth];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (Tile possibleOptions in down.tileOptions)
                        {
                            var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[valOption].downNeighbours;

                            validOptions.AddRange(valid);
                        }

                        CheckValidity(options, validOptions);
                    }

                    if (x > 0)
                    {
                        Cell left = gridComponents[x - 1 + y * roomWidth];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (Tile possibleOptions in left.tileOptions)
                        {
                            var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[valOption].rightNeighbours;

                            validOptions.AddRange(valid);
                        }

                        CheckValidity(options, validOptions);
                    }

                    Tile[] newTileList = new Tile[options.Count];

                    for (int i = 0; i < options.Count; i++)
                    {
                        newTileList[i] = options[i];
                    }

                    newGenerationCell[index].RecreateCell(newTileList);
                }
            }
        }

        gridComponents = newGenerationCell;
        iterations++;

        if (iterations < roomWidth * roomHeight)
        {
            StartCoroutine(CheckEntropy());
        }
        else
        {
            // Grid is fully collapsed, count the tiles
            CountTiles();
        }
    }

    void CheckValidity(List<Tile> optionList, List<Tile> validOption)
    {
        for (int x = optionList.Count - 1; x >= 0; x--)
        {
            var element = optionList[x];
            if (!validOption.Contains(element))
            {
                optionList.RemoveAt(x);
            }
        }
    }

    void CountTiles()
    {
        // Dictionary to store tile counts
        Dictionary<string, int> tileCounts = new Dictionary<string, int>();

        // Initialize counts for each tile type
        foreach (Tile tile in tileObjects)
        {
            tileCounts[tile.name] = 0;
        }

        // Count the tiles
        foreach (Cell cell in gridComponents)
        {
            if (cell.collapsed && cell.tileOptions.Length > 0)
            {
                string tileName = cell.tileOptions[0].name;
                if (tileCounts.ContainsKey(tileName))
                {
                    tileCounts[tileName]++;
                }
            }
        }

        // Update the UI with tile counts
        UpdateTileCountsUI(tileCounts);
    }

    void UpdateTileCountsUI(Dictionary<string, int> tileCounts)
    {
        if (tileCountText == null)
        {
            Debug.LogError("Tile Count Text UI element is not assigned!");
            return;
        }

        // Build the tile counts string
        string tileCountsString = "<b>Tile Counts</b>\n";
        foreach (var entry in tileCounts)
        {
            tileCountsString += $"{entry.Key}: {entry.Value}\n";
        }

        // Update the UI Text element
        tileCountText.text = tileCountsString;
    }
}