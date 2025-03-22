using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static int nextSceneIndex; // Stores the next scene index
    public Image fadeImage; // Reference to a UI Image for fading

    public static void LoadScene(int sceneIndex)
    {
        nextSceneIndex = sceneIndex; // Store the scene index
        Debug.Log("Next Scene Index Set: " + nextSceneIndex);
        SceneManager.LoadScene(1); // Load LoaderScene (Make sure it's index 1 in Build Settings)
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().buildIndex == 1) // If in LoaderScene
        {
            Debug.Log("Retrieved Scene Index: " + nextSceneIndex);
            StartCoroutine(LoadNextScene());
        }
    }

    IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(1f); // Short wait before fade starts

        if (fadeImage != null)
        {
            yield return StartCoroutine(FadeOut()); // Fade out before loading
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(nextSceneIndex);
        operation.allowSceneActivation = false; // Prevent immediate activation

        while (operation.progress < 0.9f) // Wait until the scene is almost ready
        {
            Debug.Log("Loading Progress: " + operation.progress);
            yield return null;
        }

        Debug.Log("Scene Loaded, Fading Out");
        
        yield return new WaitForSeconds(1f); // Extra delay for transition

        operation.allowSceneActivation = true; // Now activate the scene
    }

    public IEnumerator FadeOut()
    {
        float duration = 1f;
        float elapsedTime = 0f;
        Color color = fadeImage.color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(0, 1, elapsedTime / duration);
            fadeImage.color = color;
            yield return null;
        }
    }
}
