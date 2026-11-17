using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropSlot : MonoBehaviour, IDropHandler
{
    public PieceType accepts;   // ช่องนี้รับชิ้นอะไร
    public Image placedImage;   // ภาพชิ้นส่วนจริงที่จะโผล่ (ปิดไว้ก่อน)
    public bool filled;

    public void OnDrop(PointerEventData e)
    {
        if (filled || e.pointerDrag == null) return;

        var piece = e.pointerDrag.GetComponent<DraggablePiece>();
        if (piece == null) return;

        if (piece.type == accepts)
        {
            filled = true;
            if (placedImage != null) placedImage.enabled = true;
            ForestPuzzleManager.Instance.OnPiecePlaced(piece.type);
        }
        // ผิดชนิด → ไม่ทำอะไร สำเนาจะหายไปเอง
    }
}