using System;
using System.Collections;
using System.Collections.Generic;
<<<<<<< Updated upstream
using System.Linq;
using Unity.Mathematics;
=======
>>>>>>> Stashed changes
using UnityEngine;

public class WFCTileGeneration : MonoBehaviour
{
    public int roomWidth;
    public int roomHeight; 
    public Tile[] tileObjects;
    public List<Cell> gridComponents;
    public Cell cellObj;

    private RoomManager roomManager;
    int iterations = 0;

    void Awake()
    {
        gridComponents = new List<Cell>();
        roomManager = FindObjectOfType<RoomManager>();
<<<<<<< Updated upstream
=======
        mixRoomManager = FindObjectOfType<MixRoomManager>();
        gedRoomManager = FindObjectOfType<GEDRoomManager>();

        if (roomManager == null && mixRoomManager == null && gedRoomManager == null)
        {
            return;
        }

>>>>>>> Stashed changes
        StartCoroutine(WaitForGenerationComplete());
    }

    // Waits for room generation to complete before initializing the grid
    IEnumerator WaitForGenerationComplete()
    {
        // Wait until generationComplete is true
        while (!roomManager.generationComplete)
        {
<<<<<<< Updated upstream
=======
            if (roomManager != null)
                generationComplete = roomManager.generationComplete;
            else if (mixRoomManager != null)
                generationComplete = mixRoomManager.generationComplete;
            else if (gedRoomManager != null)
                generationComplete = gedRoomManager.generationComplete;

>>>>>>> Stashed changes
            yield return null;
        }

        // Initialize the grid once generation is complete
        InitializeGrid();
    }

    // Initializes the grid with cells
    void InitializeGrid()
    {
        // Get the position of the GameObject holding this script and add 0.5 to x and y
        Vector2 startPosition = (Vector2)transform.position + new Vector2(0.5f, 0.5f);

        for (int y = 0; y < roomHeight; y++)
        {
            for (int x = 0; x < roomWidth; x++)
            {
                // Offset each cell's position based on the startPosition
                Vector2 cellPosition = startPosition + new Vector2(x, y);

                // Instantiate the cell at the calculated position
                Cell newCell = Instantiate(cellObj, cellPosition, Quaternion.identity);

                // Initialize the cell with default properties
                newCell.CreateCell(false, tileObjects);

                // Add the cell to the gridComponents list
                gridComponents.Add(newCell);
            }
        }

        // Start the Wave Function Collapse process
        StartCoroutine(CheckEntropy());
    }

<<<<<<< Updated upstream
=======
    // Clears the grid and resets iterations
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

    // Checks and collapses cells with the lowest entropy
>>>>>>> Stashed changes
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

    // Collapses a random cell and selects a tile
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

    // Updates the grid and propagates constraints
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
                    newGenerationCell[index] = gridComponents[index];
                }
                else
                {
                    List<Tile> options = new List<Tile>();
                    foreach (Tile t in tileObjects)
                    {
                        options.Add(t);
                    }

                    //update above
                    if (y > 0)
                    {
                        Cell up = gridComponents[x + (y - 1) * roomWidth];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (Tile possibleOptions in up.tileOptions)
                        {
                            var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[valOption].upNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    //update right
                    if (x < roomWidth - 1)
                    {
                        Cell right = gridComponents[x + 1 + y * roomWidth];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (Tile possibleOptions in right.tileOptions)
                        {
                            var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[valOption].leftNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    //look down
                    if (y < roomHeight - 1)
                    {
                        Cell down = gridComponents[x + (y + 1) * roomWidth];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (Tile possibleOptions in down.tileOptions)
                        {
                            var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[valOption].downNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    //look left
                    if (x > 0)
                    {
                        Cell left = gridComponents[x - 1 + y * roomWidth];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (Tile possibleOptions in left.tileOptions)
                        {
                            var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[valOption].rightNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
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

    }

    // Filters invalid tiles from the options list
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
}