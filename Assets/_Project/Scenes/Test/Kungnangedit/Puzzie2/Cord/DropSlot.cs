using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropSlot : MonoBehaviour, IDropHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    public PieceType accepts;   // ช่องนี้รับชิ้นอะไร
    public bool filled;

    [Header("เรืองแสง")]
    public Color glowColor = new Color(1f, 0.92f, 0.4f, 0.9f);
    public float glowScale = 1.8f;   // ขนาดวงแสงเทียบกับช่อง

    Image img;
    CanvasGroup cg;
    Image glow;
    bool hovering;
    static Sprite glowSprite;

    void Awake()
    {
        img = GetComponent<Image>();
        cg = GetComponent<CanvasGroup>();
        CreateGlow();
    }

    // ---------- สร้างวงแสง ----------
    void CreateGlow()
    {
        var slotRect = (RectTransform)transform;

        var go = new GameObject("Glow_" + name, typeof(RectTransform), typeof(Image));
        var gr = (RectTransform)go.transform;
        gr.SetParent(transform.parent, false);
        gr.SetSiblingIndex(transform.GetSiblingIndex());   // อยู่หลังช่อง

        gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 0.5f);
        gr.pivot = new Vector2(0.5f, 0.5f);
        gr.sizeDelta = Vector2.Scale(slotRect.rect.size, slotRect.localScale) * glowScale;
        gr.position = slotRect.TransformPoint(slotRect.rect.center);
        gr.localScale = Vector3.one;

        glow = go.GetComponent<Image>();
        glow.sprite = GetGlowSprite();
        glow.color = glowColor;
        glow.raycastTarget = false;   // ไม่บังเมาส์
        go.SetActive(false);
    }

    static Sprite GetGlowSprite()
    {
        if (glowSprite != null) return glowSprite;

        int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s * 2f - 1f;
                float dy = (y + 0.5f) / s * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }
        tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, s, s),
                                   new Vector2(0.5f, 0.5f), 100f);
        return glowSprite;
    }

    // ---------- เมาส์ลากเข้า/ออก ----------
    public void OnPointerEnter(PointerEventData e)
    {
        if (filled || e.pointerDrag == null) return;

        var piece = e.pointerDrag.GetComponent<DraggablePiece>();
        if (piece != null && piece.type == accepts) SetHover(true);
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (filled) return;
        SetHover(false);
    }

    void SetHover(bool on)
    {
        hovering = on;
        glow.gameObject.SetActive(on);
        glow.rectTransform.localScale = Vector3.one;
    }

    void Update()
    {
        if (!hovering) return;
        float a = glowColor.a * (0.6f + 0.4f * Mathf.Sin(Time.time * 6f));
        glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, a);
    }

    // ---------- วางชิ้นส่วน ----------
    public void OnDrop(PointerEventData e)
    {
        if (filled || e.pointerDrag == null) return;

        var piece = e.pointerDrag.GetComponent<DraggablePiece>();
        if (piece == null) return;

        if (piece.type == accepts)
        {
            filled = true;
            hovering = false;
            piece.MarkPlaced();

            // ทำให้ช่องเข้มขึ้นทันที
            if (img != null)
            {
                Color c = img.color;
                c.a = 1f;
                img.color = c;
            }
            if (cg != null) cg.alpha = 1f;

            StartCoroutine(Flash());
        }
        // ผิดชนิด → ไม่ทำอะไร ชิ้นจะเด้งกลับเอง
    }

    IEnumerator Flash()
    {
        glow.gameObject.SetActive(true);
        float t = 0f, dur = 0.7f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.4f, k);
            glow.color = new Color(glowColor.r, glowColor.g, glowColor.b,
                                   Mathf.Lerp(glowColor.a, 0f, k));
            yield return null;
        }
        glow.gameObject.SetActive(false);
    }
}