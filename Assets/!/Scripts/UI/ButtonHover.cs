using UnityEngine;
using UnityEngine.EventSystems;

public class UIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject highlightBackground;
    [SerializeField] private GameObject selectionMarker;
    [SerializeField] private float slideDuration = 0.3f;

    private RectTransform highlightRectTransform;

    void Start()
    {
        if (highlightBackground)
        {
            highlightRectTransform = highlightBackground.GetComponent<RectTransform>();
            highlightBackground.SetActive(false);
            highlightRectTransform.localScale = new Vector3(0, 1, 1); // Start hidden
            highlightRectTransform.anchoredPosition = new Vector2(-highlightRectTransform.rect.width / 2, highlightRectTransform.anchoredPosition.y); // Start off-screen
        }
        if (selectionMarker) selectionMarker.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightBackground)
        {
            highlightBackground.SetActive(true);
            LeanTween.scaleX(highlightBackground, 1f, slideDuration).setEaseOutQuad();
            LeanTween.moveX(highlightRectTransform, 0, slideDuration).setEaseOutQuad();
        }

        if (selectionMarker) selectionMarker.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightBackground)
        {
            LeanTween.scaleX(highlightBackground, 0f, slideDuration)
                .setEaseOutQuad()
                .setOnComplete(() => highlightBackground.SetActive(false));
            LeanTween.moveX(highlightRectTransform, -highlightRectTransform.rect.width / 2, slideDuration).setEaseOutQuad();
        }

        if (selectionMarker) selectionMarker.SetActive(false);
    }
}
