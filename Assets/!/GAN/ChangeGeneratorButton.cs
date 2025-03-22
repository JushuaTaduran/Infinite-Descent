using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChangeGeneratorButton : MonoBehaviour
{
    public TMP_Dropdown generatorDropdown;
    public Button generateButton;
    public GameObject forestGenerator;
    public GameObject icyGenerator;
    public GameObject forestTilesParent;
    public GameObject icyTilesParent;

    private TileGenerator forestTileGenerator;
    private TileGenerator icyTileGenerator;

    void Start()
    {
        forestTileGenerator = forestGenerator.GetComponent<TileGenerator>();
        icyTileGenerator = icyGenerator.GetComponent<TileGenerator>();

        generatorDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        generateButton.onClick.AddListener(OnGenerateButtonClicked);

        // Initialize the state based on the default dropdown value
        OnDropdownValueChanged(generatorDropdown.value);
    }

    void OnDropdownValueChanged(int index)
    {
        if (index == 0) // Forest selected
        {
            forestGenerator.SetActive(true);
            icyGenerator.SetActive(false);
            forestTilesParent.SetActive(true);
            icyTilesParent.SetActive(false);
        }
        else if (index == 1) // Icy selected
        {
            forestGenerator.SetActive(false);
            icyGenerator.SetActive(true);
            forestTilesParent.SetActive(false);
            icyTilesParent.SetActive(true);
        }
    }

    public void OnGenerateButtonClicked()
    {
        if (generatorDropdown.value == 0) // Forest selected
        {
            if (forestGenerator.activeInHierarchy)
            {
                forestTileGenerator.GenerateTiles();
            }
        }
        else if (generatorDropdown.value == 1) // Icy selected
        {
            if (icyGenerator.activeInHierarchy)
            {
                icyTileGenerator.GenerateTiles();
            }
        }
    }
}