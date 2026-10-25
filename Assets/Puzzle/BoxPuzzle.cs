using System.Collections;
using UnityEngine;

public class BoxPuzzle : MonoBehaviour
{
    public enum BoxState { Hidden, Closed, Cut, Opened }

    [Header("ตัวกระปุก (SpriteRenderer เดียว สลับภาพ)")]
    [SerializeField] private SpriteRenderer cookieRenderer;
    [SerializeField] private Sprite tapedCookieSprite; // Tape-on cookie
    [SerializeField] private Sprite cookieSprite;      // cookie (ไม่มีเทป)
    [SerializeField] private Sprite inCookieSprite;    // Incookie (ด้านใน)

    [Header("ชิ้นส่วนที่โผล่ตามสถานะ")]
    [SerializeField] private GameObject[] tapeScraps;  // tape1, tape2
    [SerializeField] private Transform lid;            // Cookie jar lid
    [SerializeField] private GameObject key;           // Key

    [Header("คัดเตอร์")]
    [SerializeField] private BoxPuzzle cutter;

    [Header("ตั้งค่า")]
    [SerializeField] private float openDelay = 1f;
    [SerializeField] private Vector3 lidSlideOffset = new Vector3(2f, 0.5f, 0f);
    [SerializeField] private float lidSlideTime = 0.5f;

    public BoxState State { get; private set; } = BoxState.Hidden;

    private Vector3 lidStartPos;

    private void Awake()
    {
        if (lid != null) lidStartPos = lid.position;
        SetState(BoxState.Hidden);
    }

    // ผูกกับปุ่ม Confirm (OnClick)
    public void ShowBox()
    {
        if (State != BoxState.Hidden) return;
        SetState(BoxState.Closed);
    }

    // เรียกเมื่อตัดเทปครบ (จาก Tape/Cutter)
    public void OnTapeCut()
    {
        if (State != BoxState.Closed) return;
       // if (cutter == null || !cutter.IsHeld) return; // ต้องถือคัดเตอร์อยู่

        SetState(BoxState.Cut);
        StartCoroutine(OpenAfterDelay());
    }

    //[Header("cutter")]
    //[SerializeField] private Cutter cutter;   // เดิมเป็น BoxPuzzle
    private IEnumerator OpenAfterDelay()
    {
        yield return new WaitForSeconds(openDelay);
        SetState(BoxState.Opened);
        yield return SlideLid();
    }

    private IEnumerator SlideLid()
    {
        if (lid == null) yield break;

        Vector3 from = lidStartPos;
        Vector3 to = lidStartPos + lidSlideOffset;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / lidSlideTime;
            lid.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
    }

    private void SetState(BoxState newState)
    {
        State = newState;

        bool showCookie = newState != BoxState.Hidden;
        cookieRenderer.enabled = showCookie;

        foreach (var scrap in tapeScraps)
            scrap.SetActive(newState == BoxState.Cut || newState == BoxState.Opened);

        if (lid != null)
        {
            lid.position = lidStartPos;
            lid.gameObject.SetActive(newState == BoxState.Opened);
        }

        if (key != null)
            key.SetActive(newState == BoxState.Opened);

        switch (newState)
        {
            case BoxState.Closed: cookieRenderer.sprite = tapedCookieSprite; break;
            case BoxState.Cut: cookieRenderer.sprite = cookieSprite; break;
            case BoxState.Opened: cookieRenderer.sprite = inCookieSprite; break;
        }
    }
}