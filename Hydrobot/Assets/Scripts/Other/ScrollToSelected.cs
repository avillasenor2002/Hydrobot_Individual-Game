using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ScrollToSelected : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform viewport;

    [Header("Scroll Settings")]
    public float scrollSpeed = 12f;
    public float edgePadding = 20f;   // prevents top/bottom clipping

    Vector2 targetPos;

    void Awake()
    {
        targetPos = scrollRect.normalizedPosition;
    }

    void Update()
    {
        scrollRect.normalizedPosition = Vector2.Lerp(
            scrollRect.normalizedPosition,
            targetPos,
            scrollSpeed * Time.unscaledDeltaTime
        );
    }

    public void ScrollTo(RectTransform target)
    {
        Canvas.ForceUpdateCanvases();

        RectTransform content = scrollRect.content;

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        float hiddenHeight = contentHeight - viewportHeight;
        if (hiddenHeight <= 0) return;

        // position of target inside content
        float targetTop = Mathf.Abs(target.localPosition.y);
        float targetBottom = targetTop + target.rect.height;

        // current visible region
        float currentScrollY = (1f - scrollRect.verticalNormalizedPosition) * hiddenHeight;
        float viewTop = currentScrollY;
        float viewBottom = currentScrollY + viewportHeight;

        // adjust if target is outside view with padding
        if (targetTop - edgePadding < viewTop)
        {
            currentScrollY = targetTop - edgePadding;
        }
        else if (targetBottom + edgePadding > viewBottom)
        {
            currentScrollY = targetBottom + edgePadding - viewportHeight;
        }
        else
        {
            return; // already fully visible
        }

        currentScrollY = Mathf.Clamp(currentScrollY, 0, hiddenHeight);

        float normalizedY = 1f - (currentScrollY / hiddenHeight);
        targetPos = new Vector2(scrollRect.normalizedPosition.x, normalizedY);
    }
}