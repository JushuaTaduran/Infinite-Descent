using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WFCUIManager : MonoBehaviour
{
    public Slider widthSlider;
    public Slider heightSlider;
    public Button generateButton;
    public WFCTesting tileGenerator;
    public TextMeshProUGUI widthText;
    public TextMeshProUGUI heightText;

    void Start()
    {
        generateButton.onClick.AddListener(GenerateTiles);
        widthSlider.onValueChanged.AddListener(UpdateWidthText);
        heightSlider.onValueChanged.AddListener(UpdateHeightText);

        tileGenerator.generateButton = generateButton; // Assign button

        UpdateWidthText(widthSlider.value);
        UpdateHeightText(heightSlider.value);
    }

    void GenerateTiles()
    {
        int width = Mathf.RoundToInt(widthSlider.value);
        int height = Mathf.RoundToInt(heightSlider.value);

        tileGenerator.SetGridSize(width, height);
        tileGenerator.GenerateNewGrid();
    }

    void UpdateWidthText(float value)
    {
        widthText.text = "Width: " + Mathf.RoundToInt(value);
    }

    void UpdateHeightText(float value)
    {
        heightText.text = "Height: " + Mathf.RoundToInt(value);
    }
}
