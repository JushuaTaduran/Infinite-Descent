using UnityEngine;
using Unity.Barracuda;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class MixTextureGenerator : MonoBehaviour
{
    [System.Serializable]
    public class ThemeModelPair
    {
        public string themeName;
        public List<NNModel> modelAssets;
    }

    public List<ThemeModelPair> themeModels;
    public TileData[] tileDataAssets;
    public int tileSize = 32;
    public int modelInputSize = 256;
    public int maxRetries = 3; // Maximum number of retries for failed generations
    public int validationRetries = 5; // Additional retries for validation pass

    void Start()
    {
        StartCoroutine(GenerateByTheme());
    }

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

        // Process each theme separately
        foreach (var themePair in tilesByTheme)
        {
            string theme = themePair.Key;
            List<TileData> themeTiles = themePair.Value;
            
            Debug.Log($"Processing theme: {theme} with {themeTiles.Count} tiles");
            
            // Find matching models for this theme
            ThemeModelPair modelPair = themeModels.Find(p => p.themeName == theme);
            if (modelPair == null || modelPair.modelAssets == null || modelPair.modelAssets.Count == 0)
            {
                Debug.LogWarning($"No models found for theme: {theme}, skipping");
                continue;
            }
            
            // Load a random model for this theme
            NNModel selectedModel = modelPair.modelAssets[Random.Range(0, modelPair.modelAssets.Count)];
            Debug.Log($"Selected model for {theme}: {selectedModel.name}");
            
            Model runtimeModel = ModelLoader.Load(selectedModel);
            IWorker worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);
            
            // Process all tiles for this theme
            foreach (TileData tile in themeTiles)
            {
                if (tile.sprite == null) // Only process tiles with missing sprites
                {
                    yield return ProcessTileWithRetry(tile, worker, maxRetries);
                }
            }
            
            // Clean up worker
            worker.Dispose();
            
            yield return null;
        }
        
        // Perform final validation pass
        yield return ValidateTilesForMissingSprites();
        
        Debug.Log("All themes processed successfully");
    }

    private IEnumerator ProcessTileWithRetry(TileData tileData, IWorker worker, int retries)
    {
        int attempt = 0;
        bool success = false;

        while (attempt < retries && !success)
        {
            attempt++;
            Debug.Log($"Attempt {attempt} for TileData: {tileData.name}");
            yield return ProcessTile(tileData, worker, () => success = true); // Pass a callback to set success

            if (!success)
            {
                Debug.LogWarning($"Attempt {attempt} failed for TileData: {tileData.name}");
                yield return new WaitForSeconds(0.1f); // Small delay before retry
            }
        }

        if (!success)
        {
            Debug.LogError($"❌ Failed to process TileData: {tileData.name} after {retries} attempts");
        }
    }

    private IEnumerator ProcessTile(TileData tileData, IWorker worker, System.Action onSuccess)
    {
        Debug.Log($"Processing TileData: {tileData.name}");

        if (tileData.inputImages == null || tileData.inputImages.Count == 0)
        {
            Debug.LogError($"❌ No input images found for TileData: {tileData.name}");
            yield break;
        }

        try
        {
            // Select a random input image
            Sprite selectedInputImage = tileData.inputImages[Random.Range(0, tileData.inputImages.Count)];
            
            if (selectedInputImage == null)
            {
                Debug.LogError($"❌ Selected input image is null for TileData: {tileData.name}");
                yield break;
            }

            if (selectedInputImage.texture == null)
            {
                Debug.LogError($"❌ Selected input texture is null for TileData: {tileData.name}");
                yield break;
            }

            // Generate a texture from the input image
            Tensor input = TextureToTensor(selectedInputImage);
            if (input == null)
            {
                Debug.LogError($"❌ Failed to create tensor from sprite for TileData: {tileData.name}");
                yield break;
            }

            Tensor resizedInput = null;
            Tensor output = null;
            Tensor resizedOutput = null;
            Texture2D texture = null;
            
            try
            {
                resizedInput = ResizeTensor(input, modelInputSize, modelInputSize);
                worker.Execute(resizedInput);
                output = worker.PeekOutput();
                
                if (output == null)
                {
                    Debug.LogError($"❌ Neural network output is null for TileData: {tileData.name}");
                    input.Dispose();
                    if (resizedInput != null) resizedInput.Dispose();
                    yield break;
                }
                
                resizedOutput = ResizeTensor(output, tileSize, tileSize);

                // Generate the texture from the tensor
                texture = TensorToTexture2D(resizedOutput);
                
                if (texture == null)
                {
                    Debug.LogError($"❌ Failed to generate texture from tensor for TileData: {tileData.name}");
                    yield break;
                }

                // Create a sprite from the texture and ensure it's properly saved
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, tileSize, tileSize),
                    new Vector2(0.5f, 0.5f),
                    32
                );

                if (sprite == null)
                {
                    Debug.LogError($"❌ Failed to create sprite from texture for TileData: {tileData.name}");
                    yield break;
                }

                // Important: Give the sprite a proper name to help with debugging
                sprite.name = $"{tileData.name}_Generated";
                
                // Save texture as an asset to prevent it from being garbage collected
#if UNITY_EDITOR
                // Save the texture as an asset if in editor
                string textureFolder = "Assets/GeneratedTextures";
                if (!System.IO.Directory.Exists(textureFolder))
                {
                    System.IO.Directory.CreateDirectory(textureFolder);
                }
                
                string texturePath = $"{textureFolder}/{tileData.name}_Texture.asset";
                AssetDatabase.CreateAsset(texture, texturePath);
                
                // Create sprite asset
                string spritePath = $"{textureFolder}/{tileData.name}_Sprite.asset";
                Sprite savedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (savedSprite == null)
                {
                    AssetDatabase.CreateAsset(sprite, spritePath);
                    savedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                }
                
                // Update the TileData with the persistent sprite
                tileData.sprite = savedSprite;
#else
                // In build, just use the runtime sprite
                tileData.sprite = sprite;
#endif
                
                tileData.sortingOrder = GetSortingOrderForTile(tileData.name);
                tileData.sortingLayerName = "Default";

                // Save the changes to the TileData ScriptableObject
#if UNITY_EDITOR
                EditorUtility.SetDirty(tileData);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"TileData updated successfully: {tileData.name} with sprite: {tileData.sprite.name}");
#endif

                // Verify the sprite is valid
                if (tileData.sprite == null || tileData.sprite.texture == null)
                {
                    Debug.LogError($"❌ Sprite reference is still null after generation for TileData: {tileData.name}");
                    yield break;
                }
                
                // Call success callback
                onSuccess?.Invoke();
            }
            finally
            {
                // Clean up tensors to free memory
                input?.Dispose();
                resizedInput?.Dispose();
                output?.Dispose();
                resizedOutput?.Dispose();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Exception during processing of TileData {tileData.name}: {e.Message}\n{e.StackTrace}");
            yield break;
        }

        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator ValidateTilesForMissingSprites()
    {
        Debug.Log("Starting validation pass for missing sprites...");

        Dictionary<string, List<TileData>> missingTilesByTheme = new Dictionary<string, List<TileData>>();

        // Scan for tiles with missing sprites
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

        // Process missing tiles with additional retries
        foreach (var themePair in missingTilesByTheme)
        {
            string theme = themePair.Key;
            List<TileData> missingTiles = themePair.Value;

            Debug.Log($"Found {missingTiles.Count} missing sprites for theme: {theme}");

            ThemeModelPair modelPair = themeModels.Find(p => p.themeName == theme);
            if (modelPair == null || modelPair.modelAssets == null || modelPair.modelAssets.Count == 0)
            {
                Debug.LogWarning($"No models found for theme: {theme}, skipping validation");
                continue;
            }

            NNModel selectedModel = modelPair.modelAssets[Random.Range(0, modelPair.modelAssets.Count)];
            Model runtimeModel = ModelLoader.Load(selectedModel);
            IWorker worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);

            foreach (TileData tile in missingTiles)
            {
                yield return ProcessTileWithRetry(tile, worker, validationRetries);
            }

            worker.Dispose();
            yield return null;
        }

        Debug.Log("Validation pass completed");
    }

    public IEnumerator VerifySpriteAssignmentsInScene()
    {
        Debug.Log("Verifying sprite assignments in scene...");
        
        // Find all SpriteRenderer components in the scene
        SpriteRenderer[] allRenderers = FindObjectsOfType<SpriteRenderer>();
        
        int fixCount = 0;
        foreach (SpriteRenderer renderer in allRenderers)
        {
            if (renderer.sprite == null || renderer.sprite.texture == null)
            {
                Debug.LogWarning($"Found missing sprite on GameObject: {renderer.gameObject.name}");
                
                // Try to find a matching TileData asset
                string objectName = renderer.gameObject.name;
                TileData matchingTile = null;
                
                foreach (TileData tile in tileDataAssets)
                {
                    if (tile != null && objectName.Contains(tile.name))
                    {
                        matchingTile = tile;
                        break;
                    }
                }
                
                if (matchingTile != null && matchingTile.sprite != null)
                {
                    Debug.Log($"Found matching TileData, fixing sprite for: {objectName}");
                    renderer.sprite = matchingTile.sprite;
                    fixCount++;
                    
#if UNITY_EDITOR
                    EditorUtility.SetDirty(renderer);
#endif
                }
            }
        }
        
        Debug.Log($"Sprite verification complete. Fixed {fixCount} sprite references in scene.");
        
#if UNITY_EDITOR
        if (fixCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
#endif
        
        yield return null;
    }

    public void FixMissingSprites()
    {
        StartCoroutine(GenerateByTheme());
        StartCoroutine(VerifySpriteAssignmentsInScene());
    }

    private string ExtractThemeFromTileName(string tileName)
    {
        // Look for (ThemeName) pattern in the tile name
        if (tileName.Contains("(") && tileName.Contains(")"))
        {
            int start = tileName.IndexOf("(") + 1;
            int end = tileName.IndexOf(")");
            if (start < end)
            {
                return tileName.Substring(start, end - start);
            }
        }
        
        // Fallback to checking for theme names directly in the tile name
        if (tileName.Contains("Forest"))
            return "Forest";
        else if (tileName.Contains("Icy"))
            return "Icy";
        
        // Default theme
        return "Normal";
    }

    private int GetSortingOrderForTile(string tileName)
    {
        // Custom logic to determine the sorting order for each tile
        if (tileName.Contains("Floor"))
        {
            return 0; // Floors should be rendered below walls
        }
        else if (tileName.Contains("Wall"))
        {
            return 1; // Walls should be rendered above floors
        }
        else
        {
            return 0; // Default sorting order
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
        texture.anisoLevel = 0;

        for (int y = 0; y < tileSize; y++)
        {
            for (int x = 0; x < tileSize; x++)
            {
                Color generatedColor = new Color(
                    Mathf.Clamp((tensor[0, y, x, 0] + 1) / 2, 0, 1),
                    Mathf.Clamp((tensor[0, y, x, 1] + 1) / 2, 0, 1),
                    Mathf.Clamp((tensor[0, y, x, 2] + 1) / 2, 0, 1),
                    1f
                );

                texture.SetPixel(x, y, generatedColor);
            }
        }

        texture.Apply();
        Debug.Log($"Texture generated: {texture.width}x{texture.height}, Format: {texture.format}");
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