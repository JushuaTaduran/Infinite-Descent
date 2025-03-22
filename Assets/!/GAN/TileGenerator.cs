using UnityEngine;
using Unity.Barracuda;
using System.Collections;
using System.Collections.Generic;

public class TileGenerator : MonoBehaviour
{
    public List<NNModel> modelAssets;
    public GameObject[] tilePrefabs;
    public int tileSize = 32;
    public int modelInputSize = 256;

    private Model runtimeModel;
    private IWorker worker;

    void Start()
    {
        LoadRandomModel();
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }

    public void GenerateTiles()
    {
        StartCoroutine(GenerateTilesWithDelay());
    }

    private void LoadRandomModel()
    {
        NNModel selectedModel = modelAssets[Random.Range(0, modelAssets.Count)];
        runtimeModel = ModelLoader.Load(selectedModel);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);
    }

    private IEnumerator GenerateTilesWithDelay()
    {
        for (int i = 0; i < tilePrefabs.Length; i++)
        {
            var tilePrefab = tilePrefabs[i];
            TileInputImages tileInputImages = tilePrefab.GetComponent<TileInputImages>();
            if (tileInputImages == null || tileInputImages.inputImages.Count == 0)
            {
                Debug.LogError($"❌ No input images found for tile prefab: {tilePrefab.name}");
                continue;
            }

            // Access the SpriteRenderer component of the child "Sprite Image"
            SpriteRenderer spriteRenderer = tilePrefab.transform.Find("Sprite Image").GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError($"❌ SpriteRenderer not found in child 'Sprite Image' of prefab: {tilePrefab.name}");
                continue;
            }

            List<Sprite> inputImages = tileInputImages.inputImages;
            Sprite selectedInputImage = inputImages[Random.Range(0, inputImages.Count)];

            Tensor input = TextureToTensor(selectedInputImage);
            if (input == null)
            {
                Debug.LogError("❌ Failed to create tensor from sprite!");
                continue;
            }

            Tensor resizedInput = ResizeTensor(input, modelInputSize, modelInputSize);
            worker.Execute(resizedInput);
            Tensor output = worker.PeekOutput();
            Tensor resizedOutput = ResizeTensor(output, tileSize, tileSize);

            // Generate the texture from the tensor
            Texture2D texture = TensorToTexture2D(resizedOutput);

            // Create a sprite from the texture
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, tileSize, tileSize),
                new Vector2(0.5f, 0.5f),
                32
            );

            // Assign the sprite to the SpriteRenderer of the child "Sprite Image"
            LeanTween.scale(tilePrefab.gameObject, Vector3.zero, 0.2f).setEaseInBack().setOnComplete(() =>
            {
                spriteRenderer.sprite = sprite;
                LeanTween.scale(tilePrefab.gameObject, Vector3.one, 0.2f).setEaseOutBack();
            });

            // Dispose of tensors to free memory
            input.Dispose();
            resizedInput.Dispose();
            output.Dispose();
            resizedOutput.Dispose();

            yield return new WaitForSeconds(0.1f);
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