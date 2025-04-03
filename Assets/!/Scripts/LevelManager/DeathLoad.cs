using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DeathLoad : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(LoadGameAfterDelay());
    }

    IEnumerator LoadGameAfterDelay()
    {
        // Wait for 3 seconds before loading the Game scene
        yield return new WaitForSeconds(3f);

        // Load the Game scene
        SceneManager.LoadScene("Game");
    }
}
