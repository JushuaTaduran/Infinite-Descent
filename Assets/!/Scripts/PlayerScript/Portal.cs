using UnityEngine;
using System.Collections.Generic;

public class Portal : MonoBehaviour
{
    public List<string> targetScenes = new List<string>();
    public List<EnemyController> assignedEnemies = new List<EnemyController>();

    private bool canTeleport = false;
    private bool isOpening = false;
    private Transform portalTransform;
    private Collider2D portalCollider;
    private SpriteRenderer portalRenderer;
    
    // Static variable to track the last visited scene across all portals
    private static string lastVisitedScene = "";

    private void Start()
    {
        portalTransform = transform;
        portalCollider = GetComponent<Collider2D>();
        portalRenderer = GetComponent<SpriteRenderer>();

        // Deactivate portal at start
        portalCollider.enabled = false;
        portalRenderer.enabled = false;
        portalTransform.localScale = Vector3.zero;
    }

    private void Update()
    {
        if (!canTeleport && assignedEnemies.Count > 0)
        {
            // Check if all assigned enemies are defeated
            bool allEnemiesDefeated = assignedEnemies.TrueForAll(enemy => enemy == null || enemy.IsDead());

            if (allEnemiesDefeated && !isOpening)
            {
                OpenPortal();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && canTeleport)
        {
            canTeleport = false;
            Invoke(nameof(ResetTeleport), 1f); // Teleport cooldown

            if (targetScenes.Count > 0)
            {
                string nextScene = GetNextScene();
                lastVisitedScene = nextScene; // Update the last visited scene
                LevelManager.instance.LoadScene(nextScene);
            }
        }
    }

    private string GetNextScene()
    {
        // If we only have one target scene, just return it
        if (targetScenes.Count == 1)
            return targetScenes[0];
            
        // If this is the first teleport (no last scene), just pick randomly
        if (string.IsNullOrEmpty(lastVisitedScene))
            return targetScenes[Random.Range(0, targetScenes.Count)];
            
        // Create a list of scenes excluding the last visited one
        List<string> availableScenes = new List<string>();
        foreach (string scene in targetScenes)
        {
            if (scene != lastVisitedScene)
                availableScenes.Add(scene);
        }
        
        // If somehow all scenes were removed (should not happen with your setup)
        if (availableScenes.Count == 0)
            return targetScenes[Random.Range(0, targetScenes.Count)];
            
        // Return a random scene from the available ones
        return availableScenes[Random.Range(0, availableScenes.Count)];
    }

    private void OpenPortal()
    {
        isOpening = true;
        canTeleport = true;

        // Enable portal visuals and collider
        portalRenderer.enabled = true;
        portalCollider.enabled = true;

        // Animate portal opening
        LeanTween.scale(portalTransform.gameObject, Vector3.one, 1f)
            .setEase(LeanTweenType.easeOutBounce);
    }

    private void ResetTeleport()
    {
        canTeleport = true;
    }
}