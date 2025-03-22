using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomChangeSceneButton : MonoBehaviour
{
    [SerializeField] private List<string> sceneNames; // List of scene names for random selection

    public void ChangeSceneRandomly()
    {
        if (sceneNames.Count > 0)
        {
            int randomIndex = Random.Range(0, sceneNames.Count);
            string selectedScene = sceneNames[randomIndex];
            LevelManager.instance.LoadScene(selectedScene);
        }
        else
        {
            Debug.LogWarning("No scenes available to load.");
        }
    }
}