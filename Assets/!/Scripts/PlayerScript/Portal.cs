using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Portal : MonoBehaviour
{
    public List<MonoBehaviour> assignedEnemies = new List<MonoBehaviour>(); // Support both EnemyController & SkellyController
    public List<string> targetScenes = new List<string>();

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
            // Check if all assigned enemies (EnemyController & SkellyController) are defeated
            bool allEnemiesDefeated = assignedEnemies.All(enemy => enemy == null || IsEnemyDead(enemy));

            if (allEnemiesDefeated && !isOpening)
            {
                OpenPortal();
            }
        }
    }

    private bool IsEnemyDead(MonoBehaviour enemy)
    {
        if (enemy is EnemyController enemyController)
        {
            return enemyController.IsDead();
        }
        if (enemy is SkellyController skellyController)
        {
            return skellyController.isDead; // Make sure `isDead` is public in SkellyController
        }
        return true; // Default to true if it's an unknown enemy type
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
        if (targetScenes.Count == 1)
            return targetScenes[0];

        if (string.IsNullOrEmpty(lastVisitedScene))
            return targetScenes[Random.Range(0, targetScenes.Count)];

        List<string> availableScenes = targetScenes.Where(scene => scene != lastVisitedScene).ToList();

        return availableScenes.Count > 0 ? availableScenes[Random.Range(0, availableScenes.Count)] : targetScenes[Random.Range(0, targetScenes.Count)];
    }

    private void OpenPortal()
    {
        isOpening = true;
        canTeleport = true;

        portalRenderer.enabled = true;
        portalCollider.enabled = true;

        LeanTween.scale(portalTransform.gameObject, Vector3.one, 1f)
            .setEase(LeanTweenType.easeOutBounce);
    }

    private void ResetTeleport()
    {
        canTeleport = true;
    }
}
