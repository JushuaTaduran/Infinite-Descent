using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GEDUIManager : MonoBehaviour
{
    [SerializeField] private Transform contentPanel; // Parent for Panel_X
    [SerializeField] private GameObject panelPrefab; // Panel_X prefab
    [SerializeField] private GameObject rawImagePrefab; // RawImage prefab
    [SerializeField] private int maxPerPanel = 5; // Number of RawImages per panel

    private List<GameObject> panels = new List<GameObject>();

    public void UpdateUIWithScreenshots(List<Texture2D> screenshots)
    {
        ClearExistingPanels();

        for (int i = 0; i < screenshots.Count; i++)
        {
            if (i % maxPerPanel == 0)
            {
                // Create a new panel every 5 RawImages
                GameObject newPanel = Instantiate(panelPrefab, contentPanel);
                newPanel.name = $"Panel_{(i / maxPerPanel) + 1}";
                panels.Add(newPanel);
            }

            // Get the latest panel
            GameObject currentPanel = panels[panels.Count - 1];

            // Create RawImage and assign screenshot
            GameObject rawImageObj = Instantiate(rawImagePrefab, currentPanel.transform);
            RawImage rawImage = rawImageObj.GetComponent<RawImage>();

            // Apply the Texture2D directly
            rawImage.texture = screenshots[i];

            // Set UI Image properties
            rawImage.color = Color.white;
        }

        Debug.Log("UI Updated with RoomManager Screenshots.");
    }

    private void ClearExistingPanels()
    {
        foreach (GameObject panel in panels)
        {
            Destroy(panel);
        }
        panels.Clear();
    }
}
