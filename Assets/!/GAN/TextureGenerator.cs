using UnityEngine;
using Unity.Barracuda;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TextureGenerator : MonoBehaviour
{
    public List<NNModel> modelAssets;
    public TileData[] tileDataAssets; // Reference to your TileData ScriptableObjects
    public int tileSize = 32;
    public int modelInputSize = 256;

    private Model runtimeModel;
    private IWorker worker;

    public static bool TextureGenerationComplete { get; private set; } = false;
    public bool generateOnStart = false;

    void Start()
    {
        TextureGenerationComplete = false;

    #if UNITY_EDITOR
        // Clear output directories only in the Unity Editor
        ClearOutputDirectories();
        LoadRandomModel();

        // Start texture generation if enabled in the Editor
        if (generateOnStart)
        {
            StartCoroutine(GenerateAndUpdateTiles());
        }
    #else
        // Load a random model for texture generation
        LoadRandomModel();

        // Always start texture generation in builds
        StartCoroutine(GenerateAndUpdateTiles());
    #endif
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }

#if UNITY_EDITOR
    private void ClearOutputDirectories()
    {
        string baseDir = Path.Combine(Application.dataPath, "GeneratedTextures", "Dungeon");
        Debug.Log($"Base directory for clearing: {baseDir}");

        if (Directory.Exists(baseDir))
        {
            string[] categories = { "General", "Icy", "Forest" };
            Debug.Log($"Found base directory. Categories to clear: {string.Join(", ", categories)}");

            foreach (string category in categories)
            {
                string categoryPath = Path.Combine(baseDir, category);
                Debug.Log($"Checking category path: {categoryPath}");

                if (Directory.Exists(categoryPath))
                {
                    // Delete all files in the directory
                    string[] files = Directory.GetFiles(categoryPath, "*.png");
                    Debug.Log($"Found {files.Length} files in {categoryPath}");

                    foreach (string file in files)
                    {
                        File.Delete(file);
                        Debug.Log($"Deleted file: {file}");
                    }

                    Debug.Log($"Cleared directory: {categoryPath}");
                }
                else
                {
                    Debug.LogWarning($"Category path does not exist: {categoryPath}");
                }
            }

            // Refresh the AssetDatabase to reflect the changes
            AssetDatabase.Refresh();
            Debug.Log("AssetDatabase refreshed after clearing directories.");
        }
        else
        {
            Debug.LogWarning($"Base directory does not exist: {baseDir}");
        }
    }
#endif

    private void LoadRandomModel()
    {
        if (modelAssets == null || modelAssets.Count == 0)
        {
            Debug.LogWarning("❌ No model assets assigned!");
            return;
        }

        NNModel selectedModel = modelAssets[Random.Range(0, modelAssets.Count)];
        runtimeModel = ModelLoader.Load(selectedModel);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);
    }

    private IEnumerator GenerateAndUpdateTiles()
    {
        if (tileDataAssets == null || tileDataAssets.Length == 0)
        {
            Debug.LogWarning("❌ No TileData assets assigned!");
            TextureGenerationComplete = true;
            yield break;
        }

        for (int i = 0; i < tileDataAssets.Length; i++)
        {
            TileData tileData = tileDataAssets[i];
            if (tileData == null)
            {
                Debug.LogWarning($"❌ TileData at index {i} is null!");
                continue;
            }

            Debug.Log($"Processing TileData: {tileData.name}");

            // Validate input images
            if (tileData.inputImages == null || tileData.inputImages.Count == 0)
            {
                Debug.LogWarning($"❌ No input images found for TileData: {tileData.name}");
                continue;
            }

            Sprite selectedInputImage = tileData.inputImages[Random.Range(0, tileData.inputImages.Count)];

            // Generate a texture from the input image
            Tensor input = TextureToTensor(selectedInputImage);
            if (input == null)
            {
                Debug.LogWarning("❌ Failed to create tensor from sprite!");
                continue;
            }

            Tensor resizedInput = ResizeTensor(input, modelInputSize, modelInputSize);
            worker.Execute(resizedInput);
            Tensor output = worker.PeekOutput();
            Tensor resizedOutput = ResizeTensor(output, tileSize, tileSize);

            // Generate the texture from the tensor
            Texture2D texture = TensorToTexture2D(resizedOutput);

            // Save the texture and create a sprite
            string textureSavePath = SaveGeneratedTexture(texture, tileData.name);
            Sprite sprite = LoadSpriteFromPath(textureSavePath);

            if (sprite == null)
            {
                Debug.LogWarning($"❌ Failed to create sprite for {tileData.name}");
                continue;
            }

            // Update the TileData ScriptableObject
            tileData.sprite = sprite;
            tileData.sortingOrder = GetSortingOrderForTile(tileData.name);
            tileData.sortingLayerName = "Default";

#if UNITY_EDITOR
            // Save changes to the ScriptableObject in the Editor
            EditorUtility.SetDirty(tileData);
            AssetDatabase.SaveAssets();
            Debug.Log($"TileData updated: {tileData.name}");
#endif

            // Dispose of tensors to free memory
            input.Dispose();
            resizedInput.Dispose();
            output.Dispose();
            resizedOutput.Dispose();

            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("Texture generation complete!");
        TextureGenerationComplete = true;
    }

    private string SaveGeneratedTexture(Texture2D texture, string tileName)
    {
        // Save the texture to a runtime-accessible location
        string directoryPath = Path.Combine(Application.persistentDataPath, "GeneratedTextures");
        Directory.CreateDirectory(directoryPath);

        string filename = $"{tileName}_Generated_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string fullPath = Path.Combine(directoryPath, filename);

        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(fullPath, pngData);

        Debug.Log($"Texture saved: {fullPath}");
        return fullPath;
    }

    private Sprite LoadSpriteFromPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Invalid texture path");
            return null;
        }

        byte[] fileData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        if (!texture.LoadImage(fileData))
        {
            Debug.LogWarning($"Failed to load texture from path: {path}");
            return null;
        }

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32);
    }

    private int GetSortingOrderForTile(string tileName)
    {
        if (tileName.Contains("Floor"))
        {
            return 0;
        }
        else if (tileName.Contains("Wall"))
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    private Tensor TextureToTensor(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
        {
            Debug.LogError("❌ Sprite or texture is null!");
            return null;
        }

        Texture2D texture = sprite.texture;

        if (!texture.isReadable)
        {
            Debug.LogError($"❌ Texture {texture.name} is not readable!");
            return null;
        }

        Color[] pixels = texture.GetPixels();
        float[] tensorData = new float[pixels.Length * 3];

        for (int i = 0; i < pixels.Length; i++)
        {
            tensorData[i * 3] = pixels[i].r * 2 - 1;
            tensorData[i * 3 + 1] = pixels[i].g * 2 - 1;
            tensorData[i * 3 + 2] = pixels[i].b * 2 - 1;
        }

        return new Tensor(1, texture.height, texture.width, 3, tensorData);
    }

    private Texture2D TensorToTexture2D(Tensor tensor)
    {
        Texture2D texture = new Texture2D(tileSize, tileSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < tileSize; y++)
        {
            for (int x = 0; x < tileSize; x++)
            {
                texture.SetPixel(x, y, new Color(
                    Mathf.Clamp((tensor[0, y, x, 0] + 1) / 2, 0, 1),
                    Mathf.Clamp((tensor[0, y, x, 1] + 1) / 2, 0, 1),
                    Mathf.Clamp((tensor[0, y, x, 2] + 1) / 2, 0, 1),
                    1f
                ));
            }
        }

        texture.Apply();
        return texture;
    }

    private Tensor ResizeTensor(Tensor input, int targetWidth, int targetHeight)
    {
        Tensor resizedTensor = new Tensor(1, targetHeight, targetWidth, 3);

        float widthScale = (float)input.width / targetWidth;
        float heightScale = (float)input.height / targetHeight;

        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                int srcX = Mathf.FloorToInt(x * widthScale);
                int srcY = Mathf.FloorToInt(y * heightScale);

                for (int c = 0; c < 3; c++)
                {
                    resizedTensor[0, y, x, c] = input[0, srcY, srcX, c];
                }
            }
        }

        return resizedTensor;
    }
}