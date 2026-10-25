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

    [Header("Timing")]
    [SerializeField] private float cutToOpenDelay = 1f;
    [SerializeField] private float lidShowDelay = 0.4f;
    [SerializeField] private Vector3 lidSlideOffset = new Vector3(1f, 0.2f, 0f);
    [SerializeField] private float lidSlideTime = 0.5f;
    [SerializeField] private float keyShowTime = 1f;

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
        // เลเยอร์ 1: ตัดเทป -> cookie ทับ และเศษเทปโผล่
        if (cutLayer != null) cutLayer.SetActive(true);
        SetScraps(true);

        yield return new WaitForSeconds(cutToOpenDelay);

        // เลเยอร์ 2: กล่องเปิด -> Incookie ทับ แล้วฝาโผล่
        if (openedLayer != null) openedLayer.SetActive(true);

        yield return new WaitForSeconds(lidShowDelay);

        if (lid != null)
        {
            lid.localPosition = lidStartPos;
            lid.gameObject.SetActive(true);
        }
        yield return SlideLid();

        // เลเยอร์ 3: กุญแจโผล่
        if (keyLayer != null) keyLayer.SetActive(true);

        yield return new WaitForSeconds(keyShowTime);

        // กุญแจเด้งเข้ากระเป๋า
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
            t += Time.deltaTime / lidSlideTime;
            lid.localPosition = Vector3.Lerp(from, to, t);
            yield return null;
        }
    }

    // หลังโหลดเซฟที่เปิดกล่องไปแล้ว: แสดงสภาพกล่องที่เปิดแล้ว ไม่ให้ของซ้ำ
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