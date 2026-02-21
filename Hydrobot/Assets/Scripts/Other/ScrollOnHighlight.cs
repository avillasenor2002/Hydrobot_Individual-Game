using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollOnSelect : MonoBehaviour, ISelectHandler
{
    public ScrollToSelected scrollController;

    public void OnSelect(BaseEventData eventData)
    {
        scrollController.ScrollTo((RectTransform)transform);
    }
}