using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    public GameObject heartPrefab; // Full heart prefab (UI Image)
    public GameObject emptyHeartPrefab; // Empty heart prefab (UI Image)
    public Transform healthPanel; // Reference to the HealthPanel (HorizontalLayoutGroup)

    private List<GameObject> hearts = new List<GameObject>();

    public void InitializeHealth(int maxHealth)
    {
        Debug.LogWarning("Initializing health UI with max health: " + maxHealth);
        
        // Clear existing hearts
        foreach (GameObject heart in hearts)
        {
            Destroy(heart);
        }
        hearts.Clear();

        // Populate HealthPanel with full hearts (with pop-in animation)
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject heart = Instantiate(heartPrefab, healthPanel);
            heart.transform.localScale = Vector3.zero; // Start hidden
            LeanTween.scale(heart, Vector3.one, 0.3f).setEase(LeanTweenType.easeOutBack); // Animate pop-in
            hearts.Add(heart);
        }
    }

    public void UpdateHealth(int currentHealth)
    {
        Debug.LogWarning("Updating health UI. Current Health: " + currentHealth);

        for (int i = 0; i < hearts.Count; i++)
        {
            Image heartImage = hearts[i].GetComponent<Image>(); // Get heart UI Image
            
            if (i < currentHealth)
            {
                // Set full heart
                heartImage.sprite = heartPrefab.GetComponent<Image>().sprite;
            }
            else
            {
                // Set empty heart
                heartImage.sprite = emptyHeartPrefab.GetComponent<Image>().sprite;
            }
        }
    }
}
