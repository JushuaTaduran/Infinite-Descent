using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GEDRoomInitializer : MonoBehaviour
{
    [SerializeField] private GameObject roomManagerPrefab;
    [SerializeField] private int maxColumns = 5;
    [SerializeField] private int roommanagersToGenerate = 10;
    [SerializeField] private float roomWidth = 11f;
    [SerializeField] private float roomHeight = 11f;
    [SerializeField] private int gridSizeX = 20;
    [SerializeField] private int gridSizeY = 20;

    [SerializeField] private GEDUIManager uiManager; // Reference to UI Manager

    public List<GEDRoomManager> roomManagers = new List<GEDRoomManager>();

    private void Start()
    {
        InitializeRoomManagers();
        StartCoroutine(WaitForGenerationCompletion());
    }

    public void InitializeRoomManagers()
    {
        for (int i = 0; i < roommanagersToGenerate; i++)
        {
            int row = i / maxColumns;
            int column = i % maxColumns;

            float posX = column * (roomWidth * gridSizeX);
            float posY = -row * (roomHeight * gridSizeY);

            Vector3 position = new Vector3(posX, posY, 0);

            GameObject roomManagerObject = Instantiate(roomManagerPrefab, position, Quaternion.identity, transform);
            GEDRoomManager roomManager = roomManagerObject.GetComponent<GEDRoomManager>();

            roomManagers.Add(roomManager);
        }

        Debug.Log($"Initialized {roommanagersToGenerate} RoomManagers in a grid layout with {maxColumns} columns.");
    }

    private IEnumerator WaitForGenerationCompletion()
    {
        Debug.Log("[DungeonGraph] Waiting for all RoomManagers to complete generation...");

        foreach (var roomManager in roomManagers)
        {
            yield return new WaitUntil(() => roomManager.generationComplete);
            Debug.Log($"[DungeonGraph] RoomManager {roomManager.name} generation complete.");
        }

        Debug.Log("[DungeonGraph] All RoomManagers have completed generation. Capturing screenshots...");

        StartCoroutine(CaptureScreenshots());
    }

    private IEnumerator CaptureScreenshots()
    {
        yield return new WaitForSeconds(1f); // Wait a bit to ensure final updates

        List<Texture2D> screenshots = new List<Texture2D>();

        foreach (var roomManager in roomManagers)
        {
            Texture2D screenshot = CaptureRoomScreenshot(roomManager);
            screenshots.Add(screenshot);
        }

        uiManager.UpdateUIWithScreenshots(screenshots);
    }

    private Texture2D CaptureRoomScreenshot(GEDRoomManager roomManager)
    {
        Vector3 roomPosition = roomManager.transform.position;
        float roomW = roomManager.roomWidth * roomManager.gridSizeX;
        float roomH = roomManager.roomHeight * roomManager.gridSizeY;

        GameObject screenshotCameraObj = new GameObject("ScreenshotCamera");
        Camera screenshotCamera = screenshotCameraObj.AddComponent<Camera>();
        screenshotCamera.orthographic = true;
        screenshotCamera.orthographicSize = roomH / 2f;
        screenshotCamera.aspect = roomW / roomH;
        screenshotCamera.transform.position = new Vector3(roomPosition.x, roomPosition.y, -10f);

        screenshotCamera.clearFlags = CameraClearFlags.SolidColor;
        screenshotCamera.backgroundColor = new Color(0f, 0f, 0f, 0f); // Fully transparent

        int resolutionX = (int)(roomW * 10);
        int resolutionY = (int)(roomH * 10);
        RenderTexture renderTexture = new RenderTexture(resolutionX, resolutionY, 24, RenderTextureFormat.ARGB32);
        screenshotCamera.targetTexture = renderTexture;
        Texture2D screenshot = new Texture2D(resolutionX, resolutionY, TextureFormat.RGBA32, false);

        screenshotCamera.Render();
        RenderTexture.active = renderTexture;
        screenshot.ReadPixels(new Rect(0, 0, resolutionX, resolutionY), 0, 0);
        screenshot.Apply();

        RenderTexture.active = null;
        Destroy(renderTexture);
        Destroy(screenshotCameraObj);

        return screenshot;
    }
}
