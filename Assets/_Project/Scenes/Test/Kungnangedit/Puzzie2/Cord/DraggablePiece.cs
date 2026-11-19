 using UnityEngine;
using UnityEngine.EventSystems;

public enum PieceType { Leaf, Branch, RedFruit, YellowFruit, BlueFruit }

public class DraggablePiece : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PieceType type;

    Canvas canvas;
    RectTransform rect;
    CanvasGroup group;
    Transform startParent;
    int startSibling;
    Vector2 startPos;
    bool placed;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        rect = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        startParent = transform.parent;
        startSibling = transform.GetSiblingIndex();
        startPos = rect.anchoredPosition;

        transform.SetParent(canvas.transform);   // ลอยอยู่บนสุด
        transform.SetAsLastSibling();
        group.blocksRaycasts = false;            // ให้ช่องรับ Drop ได้
    }

    public void OnDrag(PointerEventData e)
    {
        rect.anchoredPosition += e.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData e)
    {
        group.blocksRaycasts = true;

        if (placed)
        {
            gameObject.SetActive(false);         // วางถูก → ชิ้นซ้ายหายไป
        }
        else
        {
            transform.SetParent(startParent);    // วางผิด → กลับที่เดิม
            transform.SetSiblingIndex(startSibling);
            rect.anchoredPosition = startPos;
        }
    }

    public void MarkPlaced()
    {
        placed = true;
    }
}