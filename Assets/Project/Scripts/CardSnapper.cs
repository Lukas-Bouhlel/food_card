using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class CardSnapper : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Tooltip("How fast the scroll snaps to the target card (higher = snappier).")]
    public float snapSpeed = 10f;

    [SerializeField] private DishSwitcher dishSwitcher;

    private ScrollRect scrollRect;
    private bool isDragging = false;
    private bool isSnapping = false;
    private float targetNormalizedX;
    private int currentSnappedIndex = -1;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    void Start()
    {
        if (dishSwitcher != null)
            dishSwitcher.onDishChanged += ScrollToIndex;
    }

    void OnDestroy()
    {
        if (dishSwitcher != null)
            dishSwitcher.onDishChanged -= ScrollToIndex;
    }

    // Called by DishSwitcher when the dish changes (swipe in AR)
    // Does NOT call back SelectDish to avoid a loop.
    void ScrollToIndex(int index)
    {
        int cardCount = scrollRect.content.childCount;
        if (cardCount <= 1 || index == currentSnappedIndex)
            return;

        currentSnappedIndex = index;
        targetNormalizedX = (float)index / (cardCount - 1);
        isSnapping = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        isSnapping = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        SnapToNearest();
    }

    void Update()
    {
        if (!isSnapping || isDragging)
            return;

        scrollRect.horizontalNormalizedPosition = Mathf.Lerp(
            scrollRect.horizontalNormalizedPosition,
            targetNormalizedX,
            Time.deltaTime * snapSpeed
        );

        if (Mathf.Abs(scrollRect.horizontalNormalizedPosition - targetNormalizedX) < 0.001f)
        {
            scrollRect.horizontalNormalizedPosition = targetNormalizedX;
            isSnapping = false;
        }
    }

    void SnapToNearest()
    {
        int cardCount = scrollRect.content.childCount;
        if (cardCount == 0)
            return;

        float viewportCenterX = scrollRect.viewport.rect.width * 0.5f;
        float closestDist = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < cardCount; i++)
        {
            RectTransform child = scrollRect.content.GetChild(i) as RectTransform;
            if (child == null) continue;

            Vector2 childPos = scrollRect.viewport.InverseTransformPoint(child.position);
            float dist = Mathf.Abs(childPos.x - viewportCenterX);

            if (dist < closestDist)
            {
                closestDist = dist;
                closestIndex = i;
            }
        }

        targetNormalizedX = cardCount <= 1 ? 0f : (float)closestIndex / (cardCount - 1);
        isSnapping = true;

        if (closestIndex != currentSnappedIndex)
        {
            currentSnappedIndex = closestIndex;
            dishSwitcher?.SelectDish(closestIndex);
        }
    }
}
