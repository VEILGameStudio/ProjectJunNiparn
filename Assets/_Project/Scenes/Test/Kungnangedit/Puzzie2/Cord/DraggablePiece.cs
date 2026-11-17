using UnityEngine;
using UnityEngine.EventSystems;

public enum PieceType { Leaf, Branch, RedFruit, YellowFruit, BlueFruit }

public class DraggablePiece : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PieceType type;

    Canvas canvas;
    GameObject dragCopy;
    RectTransform copyRect;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        // ชิ้นส่วนหมดแล้ว ลากไม่ได้
        if (ForestPuzzleManager.Instance.GetCount(type) <= 0) return;

        // สร้างสำเนาให้ลอยตามเมาส์ ตัวจริงอยู่ที่เดิม
        dragCopy = Instantiate(gameObject, canvas.transform);
        Destroy(dragCopy.GetComponent<DraggablePiece>());

        var cg = dragCopy.GetComponent<CanvasGroup>();
        if (cg == null) cg = dragCopy.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;   // ไม่บังเมาส์ ให้ Slot รับ Drop ได้

        copyRect = dragCopy.GetComponent<RectTransform>();
        copyRect.anchorMin = copyRect.anchorMax = new Vector2(0.5f, 0.5f);
        copyRect.sizeDelta = ((RectTransform)transform).rect.size;
        copyRect.position = e.position;
        copyRect.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData e)
    {
        if (dragCopy != null) copyRect.position = e.position;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (dragCopy != null) Destroy(dragCopy);
    }
}