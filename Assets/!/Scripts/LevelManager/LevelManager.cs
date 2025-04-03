using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [SerializeField] private GameObject _loadingscreenCanvas;
    [SerializeField] private GameObject _mainMenuCanvas;
    [SerializeField] private TextMeshProUGUI _loadingText;
    [SerializeField] private float _tipChangeInterval = 3f;

    private string[] _loadingTips = new string[]
    {
        "Crafting the perfect dungeon...",
        "Placing rooms... hoping they connect!",
        "Rolling the dice on level layout...",
        "Spawning secrets... shhh!",
        "Generating walls... you can't walk through them, sorry.",
        "Populating the map... enemies included!",
        "Randomizing loot drops... hope you get something good!",
        "Shuffling the terrain... no two runs are the same!",
        "Scattering treasures... but will you find them?",
        "Creating shortcuts... or maybe dead ends!",
        "Collapsing wave functions...",
        "Optimizing randomness... if that even makes sense.",
        "Simulating thousands of possibilities...",
        "Letting the neural network dream of dungeons...",
        "Executing procedural magic... beep boop!",
        "Generating infinite dungeons...",
        "Deleting the boss... just kidding.",
        "Oops, all loot chests!",
        "Debugging the dungeon... oh wait, no bugs!",
        "Generating... but only pretending to.",
        "100% crash-free loading! (*probably*)"
    };

    private Coroutine _loadingTipsCoroutine;
    private GameObject _player; // Reference to the player

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async void LoadScene(string sceneName)
    {
        // Find the player and disable it
        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player != null)
        {
            _player.SetActive(false);
        }

        // Hide main menu if it exists
        if (_mainMenuCanvas != null)
        {
            _mainMenuCanvas.SetActive(false);
        }

        // Show loading screen
        _loadingscreenCanvas.SetActive(true);

        // Start showing loading tips
        _loadingTipsCoroutine = StartCoroutine(ShowRandomLoadingTips());

        // Load scene
        var scene = SceneManager.LoadSceneAsync(sceneName);
        scene.allowSceneActivation = true;

        // Wait for scene to be fully loaded
        while (!scene.isDone)
        {
            await Task.Delay(100);
        }

        // Wait for generation to complete
        StartCoroutine(WaitForGenerationComplete());
    }

    private IEnumerator ShowRandomLoadingTips()
    {
        if (_loadingText == null)
        {
            Debug.LogWarning("Loading text reference is missing!");
            yield break;
        }

        List<int> usedTipIndices = new List<int>();

        while (true)
        {
            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, _loadingTips.Length);
            } while (usedTipIndices.Contains(randomIndex) && usedTipIndices.Count < _loadingTips.Length * 0.75f);

            usedTipIndices.Add(randomIndex);
            if (usedTipIndices.Count > _loadingTips.Length * 0.75f)
            {
                usedTipIndices.RemoveAt(0);
            }

            _loadingText.text = _loadingTips[randomIndex];

            yield return new WaitForSeconds(_tipChangeInterval);
        }
    }

    private IEnumerator WaitForGenerationComplete()
    {
        yield return new WaitForSeconds(0.5f);

        RoomManager roomManager = FindObjectOfType<RoomManager>();
        if (roomManager != null)
        {
            Debug.Log("Waiting for RoomManager to complete generation...");
            while (!roomManager.generationComplete)
            {
                yield return null;
            }
            Debug.Log("RoomManager generation completed!");
        }
        else
        {
            MixRoomManager mixRoomManager = FindObjectOfType<MixRoomManager>();
            if (mixRoomManager != null)
            {
                Debug.Log("Waiting for MixRoomManager to complete generation...");
                while (!mixRoomManager.generationComplete)
                {
                    yield return null;
                }
                Debug.Log("MixRoomManager generation completed!");
            }
        }

        // Stop the loading tips coroutine
        if (_loadingTipsCoroutine != null)
        {
            StopCoroutine(_loadingTipsCoroutine);
            _loadingTipsCoroutine = null;
        }

        // Hide loading screen
        Debug.Log("All generation processes complete, hiding loading screen");
        _loadingscreenCanvas.SetActive(false);

        // Reactivate player after everything is done
        if (_player != null)
        {
            _player.SetActive(true);
        }
    }
}
