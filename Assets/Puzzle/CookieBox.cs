using System.Collections;
using UnityEngine;

public class CookieBox : InteractableBase
{
    [Header("Layers (ลูกของกล่อง ปิดไว้ตอนเริ่ม)")]
    [Tooltip("Layer cookie: shown after the tape is cut.")]
    [SerializeField] private GameObject cutLayer;
    [Tooltip("Layer Incookie: the inside of the opened box.")]
    [SerializeField] private GameObject openedLayer;
    [SerializeField] private GameObject[] tapeScraps;   // tape1
    [SerializeField] private Transform lid;             // Cookie jar lid
    [SerializeField] private GameObject keyLayer;       // Key

    [Header("Key")]
    [Tooltip("The item given to the player when the box opens.")]
    [SerializeField] private ItemData keyItem;

    [Header("Timing (วินาที)")]
    [Tooltip("เวลาที่ค้างภาพ cookie ก่อนฝาเลื่อนออก")]
    [SerializeField] private float cutToOpenDelay = 2f;
    [SerializeField] private float lidSlideTime = 1.2f;
    [Tooltip("เวลาที่กุญแจค้างให้เห็นก่อนเข้ากระเป๋า")]
    [SerializeField] private float keyShowTime = 1.5f;

    [Header("Lid")]
    [SerializeField] private Vector3 lidSlideOffset = new Vector3(30f, 8f, 0f);

    private Vector3 lidStartPos;
    private bool lidMoved;

    // The box is used only once, and the save file remembers it.
    protected override bool UsedOnlyOnce => true;

    protected override void OnStarted()
    {
        if (lid != null) lidStartPos = lid.localPosition;
    }

    protected override void OnInteract(PlayerContext player)
    {
        MarkUsed();   // กันคลิกซ้ำระหว่างที่กำลังเปิด
        StartCoroutine(OpenSequence(player));
    }

    private IEnumerator OpenSequence(PlayerContext player)
    {
        // 1) ตัดเทป: เปลี่ยนเป็นภาพ cookie และเศษเทปโผล่ทันที (ไม่มี fade)
        if (cutLayer != null) cutLayer.SetActive(true);
        SetScraps(true);

        // 2) ค้างภาพ cookie
        yield return new WaitForSeconds(cutToOpenDelay);

        // 3) สลับเป็นกล่องเปิด: Incookie อยู่ใต้ฝา ฝาอยู่ที่เดิม แล้วเลื่อนออกทันที
        if (openedLayer != null) openedLayer.SetActive(true);
        if (lid != null)
        {
            lid.localPosition = lidStartPos;
            lid.gameObject.SetActive(true);
        }
        yield return SlideLid();

        // 4) กุญแจโผล่ ค้างไว้ แล้วเข้ากระเป๋า
        if (keyLayer != null) keyLayer.SetActive(true);
        yield return new WaitForSeconds(keyShowTime);

        if (player != null && player.Inventory != null && keyItem != null)
        {
            player.Inventory.Add(keyItem, 1);
        }
        if (keyLayer != null) keyLayer.SetActive(false);
    }

    private IEnumerator SlideLid()
    {
        if (lid == null || lidMoved) yield break;
        lidMoved = true;

        Vector3 from = lidStartPos;
        Vector3 to = lidStartPos + lidSlideOffset;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, lidSlideTime);
            lid.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            yield return null;
        }
        lid.localPosition = to;
    }

    // หลังโหลดเซฟที่เปิดกล่องไปแล้ว: แสดงสภาพกล่องที่เปิดแล้วทันที ไม่ให้ของซ้ำ
    protected override void OnRestoredAsUsed()
    {
        base.OnRestoredAsUsed();
        if (cutLayer != null) cutLayer.SetActive(true);
        if (openedLayer != null) openedLayer.SetActive(true);
        SetScraps(true);
        if (lid != null && !lidMoved)
        {
            lid.gameObject.SetActive(true);
            lid.localPosition += lidSlideOffset;
            lidMoved = true;
        }
        if (keyLayer != null) keyLayer.SetActive(false);
    }

    private void SetScraps(bool visible)
    {
        if (tapeScraps == null) return;
        foreach (GameObject scrap in tapeScraps)
        {
            if (scrap != null) scrap.SetActive(visible);
        }
    }
}