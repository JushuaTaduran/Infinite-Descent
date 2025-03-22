using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [SerializeField] private Transform portal; // Reference to the portal
    [SerializeField] private Transform player; // Reference to the player
    [SerializeField] private float distanceFromPlayer = 2f; // Distance from the player

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                Debug.LogError("Player not found in the scene!");
            }
        }
    }

    private void Update()
    {
        // Find the portal in the scene if not already assigned
        if (portal == null)
        {
            GameObject portalObject = GameObject.FindGameObjectWithTag("Portal");
            if (portalObject != null)
            {
                portal = portalObject.transform;
            }
            else
            {
                Debug.LogError("Portal not found in the scene!");
            }
        }

        if (portal != null && player != null)
        {
            // Calculate the direction from the player to the portal
            Vector3 direction = portal.position - player.position;
            direction.z = 0; // Ignore the z-axis

            // Calculate the angle to rotate the arrow
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Rotate the arrow to point towards the portal
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

            // Position the arrow at a fixed distance from the player
            Vector3 offset = direction.normalized * distanceFromPlayer;
            transform.position = player.position + offset;
        }
    }
}