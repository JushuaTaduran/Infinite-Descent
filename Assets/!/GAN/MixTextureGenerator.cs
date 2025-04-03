using UnityEngine;
using Unity.Barracuda;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[System.Serializable]
public class ThemeModelPair
{
    public string themeName;
    public List<NNModel> modelAssets;
}

public class MixTextureGenerator : MonoBehaviour
{
    public List<ThemeModelPair> themeModels;
    public TileData[] tileDataAssets;
    public int tileSize = 32;
    public int modelInputSize = 256;
    public int maxRetries = 3;
    public int validationRetries = 5;
    public bool generateOnStart = false;

    public static bool TextureGenerationComplete { get; private set; } = false; //Just incase a flag

    void Start()
    {
        TextureGenerationComplete = false;

    #if UNITY_EDITOR
        // Clear output directories only in the Unity Editor
        ClearOutputDirectories();

        // Start texture generation if enabled in the Editor
        if (generateOnStart)
        {
            StartCoroutine(GenerateByTheme());
        }
    #else
        StartCoroutine(GenerateByTheme());
    #endif
    }

#if UNITY_EDITOR
    private void ClearOutputDirectories()
    {

        string baseDir = Path.Combine(Application.dataPath, "GeneratedTextures", "Mixed");
        Debug.Log($"Base directory for clearing: {baseDir}");

        if (Directory.Exists(baseDir))
        {
            // Get all theme names from themeModels
            List<string> categories = new List<string>();
            foreach (var theme in themeModels)
            {
                if (!categories.Contains(theme.themeName))
                {
                    categories.Add(theme.themeName);
                }
            }

            foreach (string category in categories)
            {
                string categoryPath = Path.Combine(baseDir, category);
                if (Directory.Exists(categoryPath))
                {
                    string[] files = Directory.GetFiles(categoryPath, "*.png");
                    foreach (string file in files)
                    {
                        File.Delete(file);
                    }
                    Debug.Log($"Cleared directory: {categoryPath}");
                }
            }

            AssetDatabase.Refresh();
            generateOnStart = true;
        }
        else
        {
            Directory.CreateDirectory(baseDir);
        }

    }
#endif

    private IEnumerator GenerateByTheme()
    {
        if (tileDataAssets == null || tileDataAssets.Length == 0)
        {
            Debug.LogError("❌ No TileData assets assigned!");
            yield break;
        }

        Dictionary<string, List<TileData>> tilesByTheme = new Dictionary<string, List<TileData>>();
        
        foreach (TileData tile in tileDataAssets)
        {
            if (tile == null) continue;
            
            string theme = ExtractThemeFromTileName(tile.name);
            if (!tilesByTheme.ContainsKey(theme))
            {
                tilesByTheme[theme] = new List<TileData>();
            }
            tilesByTheme[theme].Add(tile);
        }

        foreach (var themePair in tilesByTheme)
        {
            string theme = themePair.Key;
            List<TileData> themeTiles = themePair.Value;
            
            ThemeModelPair modelPair = themeModels.Find(p => p.themeName == theme);
            if (modelPair == null || modelPair.modelAssets == null || modelPair.modelAssets.Count == 0)
            {
                Debug.LogWarning($"No models found for theme: {theme}, skipping");
                continue;
            }
            
            NNModel selectedModel = modelPair.modelAssets[Random.Range(0, modelPair.modelAssets.Count)];
            Model runtimeModel = ModelLoader.Load(selectedModel);
            IWorker worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);
            
            foreach (TileData tile in themeTiles)
            {
                yield return ProcessTileWithRetry(tile, worker, maxRetries);
            }
            
            worker.Dispose();
            yield return null;
        }
        
        yield return ValidateTilesForMissingSprites();
        TextureGenerationComplete = true;
        Debug.Log("All themes processed successfully");
    }

    private IEnumerator ProcessTileWithRetry(TileData tileData, IWorker worker, int retries)
    {
        int attempt = 0;
        bool success = false;

        while (attempt < retries && !success)
        {
            attempt++;
            yield return ProcessTile(tileData, worker, () => success = true);
            
            if (!success)
            {
                Debug.LogWarning($"Attempt {attempt} failed for TileData: {tileData.name}");
                yield return new WaitForSeconds(0.1f);
            }
        }

        if (!success)
        {
            Debug.LogError($"❌ Failed to process TileData: {tileData.name} after {retries} attempts");
        }
    }

    private IEnumerator ProcessTile(TileData tileData, IWorker worker, System.Action onSuccess)
    {
        if (tileData.inputImages == null || tileData.inputImages.Count == 0)
        {
            Debug.LogError($"❌ No input images found for TileData: {tileData.name}");
            yield break;
        }

        Sprite selectedInputImage = tileData.inputImages[Random.Range(0, tileData.inputImages.Count)];
        Tensor input = TextureToTensor(selectedInputImage);
        if (input == null) yield break;

        Tensor resizedInput = ResizeTensor(input, modelInputSize, modelInputSize);
        worker.Execute(resizedInput);
        Tensor output = worker.PeekOutput();
        Tensor resizedOutput = ResizeTensor(output, tileSize, tileSize);
        Texture2D texture = TensorToTexture2D(resizedOutput);

#if UNITY_EDITOR
        string theme = ExtractThemeFromTileName(tileData.name);
        string texturePath = SaveGeneratedTexture(texture, tileData.name, theme);

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);

        if (sprite == null)
        {
            Debug.LogError($"❌ Failed to create sprite for TileData: {tileData.name}");
            yield break;
        }

        tileData.sprite = sprite;
        tileData.sortingOrder = GetSortingOrderForTile(tileData.name);
        tileData.sortingLayerName = "Default";

        EditorUtility.SetDirty(tileData);
        AssetDatabase.SaveAssets();
#else

        Sprite sprite = Sprite.Create(texture, 
            new Rect(0, 0, texture.width, texture.height), 
            new Vector2(0.5f, 0.5f), 32);
        tileData.sprite = sprite;
#endif

        input.Dispose();
        resizedInput.Dispose();
        output.Dispose();
        resizedOutput.Dispose();

        onSuccess?.Invoke();
        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator ValidateTilesForMissingSprites()
    {
        Dictionary<string, List<TileData>> missingTilesByTheme = new Dictionary<string, List<TileData>>();

        foreach (TileData tile in tileDataAssets)
        {
            if (tile == null || tile.sprite != null) continue;

            string theme = ExtractThemeFromTileName(tile.name);
            if (!missingTilesByTheme.ContainsKey(theme))
            {
                missingTilesByTheme[theme] = new List<TileData>();
            }
            missingTilesByTheme[theme].Add(tile);
        }

        foreach (var themePair in missingTilesByTheme)
        {
            string theme = themePair.Key;
            ThemeModelPair modelPair = themeModels.Find(p => p.themeName == theme);
            if (modelPair == null) continue;

            NNModel selectedModel = modelPair.modelAssets[Random.Range(0, modelPair.modelAssets.Count)];
            Model runtimeModel = ModelLoader.Load(selectedModel);
            IWorker worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);

            foreach (TileData tile in themePair.Value)
            {
                yield return ProcessTileWithRetry(tile, worker, validationRetries);
            }

            worker.Dispose();
            yield return null;
        }
    }

    private string ExtractThemeFromTileName(string tileName)
    {
        if (tileName.Contains("(") && tileName.Contains(")"))
        {
            int start = tileName.IndexOf("(") + 1;
            int end = tileName.IndexOf(")");
            if (start < end) return tileName.Substring(start, end - start);
        }
        
        if (tileName.Contains("Forest")) return "Forest";
        if (tileName.Contains("Icy")) return "Icy";
        return "General";
    }

    private int GetSortingOrderForTile(string tileName)
    {
        if (tileName.Contains("Floor")) return 0;
        if (tileName.Contains("Wall")) return 1;
        return 0;
    }

    private Tensor TextureToTensor(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return null;

        Texture2D texture = sprite.texture;
        if (!texture.isReadable) return null;

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

#if UNITY_EDITOR
    private string SaveGeneratedTexture(Texture2D texture, string tileName, string category)
    {
        string directoryPath = Path.Combine(Application.dataPath, "GeneratedTextures", "Mixed", category);
        Directory.CreateDirectory(directoryPath);

        string filename = $"{tileName}_Generated_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string fullPath = Path.Combine(directoryPath, filename);
        string relativePath = Path.Combine("Assets", "GeneratedTextures", "Mixed", category, filename);

        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(fullPath, pngData);

        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;

            AssetDatabase.ImportAsset(relativePath);
        }

        Debug.Log($"Texture saved: {relativePath}");
        return relativePath;

    }
#endif
}