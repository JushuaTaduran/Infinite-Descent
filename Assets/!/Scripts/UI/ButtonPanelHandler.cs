using UnityEngine;
using UnityEngine.UI;

public class ButtonPanelHandler : MonoBehaviour
{
    public GameObject currentPanel;
    public GameObject goToPanel;
    public float slideSpeed = 0.5f;
    public float fadeSpeed = 0.3f;

    private Vector2 originalGoToPanelPos;

    private void Start()
    {
        if (goToPanel != null)
        {
            RectTransform rt = goToPanel.GetComponent<RectTransform>();
            originalGoToPanelPos = rt.anchoredPosition; // Save panel's original position
        }
    }

    public void OnClick()
    {
        if (currentPanel != null)
        {
            FadeOut(currentPanel, () => currentPanel.SetActive(false));
        }

        if (goToPanel != null)
        {
            goToPanel.SetActive(true);
            SlideIn(goToPanel);
        }
    }

    private void SlideIn(GameObject panel)
    {
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(originalGoToPanelPos.x, Screen.height); // Start above original position
        LeanTween.moveY(rt, originalGoToPanelPos.y, slideSpeed).setEaseOutQuad();
    }

    private void FadeOut(GameObject panel, System.Action onComplete)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        LeanTween.alphaCanvas(canvasGroup, 0, fadeSpeed).setOnComplete(() => 
        {
            canvasGroup.alpha = 1;
            onComplete();
        });
    }
}
