using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class WFCTesting : MonoBehaviour
{
    public int roomWidth;
    public int roomHeight;
    public Tile[] tileObjects;
    public List<Cell> gridComponents = new List<Cell>();
    public Cell cellObj;
    public Button generateButton;

    private List<GameObject> generatedTiles = new List<GameObject>();
    private Coroutine generationCoroutine; // Store running coroutine

    public void SetGridSize(int width, int height)
    {
        roomWidth = width;
        roomHeight = height;
    }

    public void GenerateNewGrid()
    {
        if (generateButton != null)
            generateButton.interactable = false; // Disable button immediately

        if (generationCoroutine != null)
            StopCoroutine(generationCoroutine); // Stop previous generation if running

        ClearPreviousGrid();
        generationCoroutine = StartCoroutine(InitializeGrid());
    }

    private void ClearPreviousGrid()
    {
        foreach (Cell cell in gridComponents)
        {
            if (cell != null)
                Destroy(cell.gameObject);
        }
        gridComponents.Clear();

        foreach (GameObject tile in generatedTiles)
        {
            if (tile != null)
                Destroy(tile);
        }
        generatedTiles.Clear();
    }

    IEnumerator InitializeGrid()
    {
        Vector2 startPosition = (Vector2)transform.position + new Vector2(0.5f, 0.5f);

        for (int y = 0; y < roomHeight; y++)
        {
            for (int x = 0; x < roomWidth; x++)
            {
                Vector2 cellPosition = startPosition + new Vector2(x, y);
                Cell newCell = Instantiate(cellObj, cellPosition, Quaternion.identity);
                newCell.CreateCell(false, tileObjects);
                gridComponents.Add(newCell);

                newCell.transform.localScale = Vector3.zero;
                LeanTween.scale(newCell.gameObject, Vector3.one, 0.2f).setEaseOutBack();

                yield return new WaitForSeconds(0.02f);
            }
        }

        yield return StartCoroutine(CheckEntropy());

        if (generateButton != null)
            generateButton.interactable = true; // Re-enable button only when complete
    }

    IEnumerator CheckEntropy()
    {
        while (true)
        {
            List<Cell> tempGrid = gridComponents.Where(c => c != null && !c.collapsed).ToList();

            if (tempGrid.Count == 0)
            {
                Debug.Log("✅ All cells collapsed!");
                yield break;
            }

            tempGrid.Sort((a, b) => a.tileOptions.Length.CompareTo(b.tileOptions.Length));

            int minOptions = tempGrid[0].tileOptions.Length;
            tempGrid = tempGrid.Where(c => c.tileOptions.Length == minOptions).ToList();

            yield return new WaitForSeconds(0.05f);
            CollapseCell(tempGrid);
        }
    }

    void CollapseCell(List<Cell> tempGrid)
    {
        if (tempGrid.Count == 0) return;

        int randIndex = Random.Range(0, tempGrid.Count);
        Cell cellToCollapse = tempGrid[randIndex];

        if (cellToCollapse == null || cellToCollapse.gameObject == null) return; // Ensure valid reference

        cellToCollapse.collapsed = true;
        Tile selectedTile = cellToCollapse.tileOptions[Random.Range(0, cellToCollapse.tileOptions.Length)];
        cellToCollapse.tileOptions = new Tile[] { selectedTile };

        GameObject existingTile = generatedTiles.FirstOrDefault(tile => tile != null && tile.transform.position == cellToCollapse.transform.position);
        if (existingTile != null)
        {
            generatedTiles.Remove(existingTile);
            Destroy(existingTile);
        }

        GameObject newTile = Instantiate(selectedTile.gameObject, cellToCollapse.transform.position, Quaternion.identity);
        generatedTiles.Add(newTile);

        newTile.transform.localScale = Vector3.zero;
        LeanTween.scale(newTile, Vector3.one, 0.2f).setEaseOutBack();
    }
}
