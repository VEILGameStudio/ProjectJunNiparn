using UnityEngine;
using UnityEngine.UI;

// ตัวกลางเก็บ "รหัสที่ถูกต้อง" ไว้ที่เดียว
// คำใบ้ทุกจุด (กรอบรูป/นาฬิกา/ลิ้นชัก/ปฏิทิน) ดึงรูปจากรหัสนี้ จึงตรงกับรหัสปลดล็อคแน่นอน
public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance;

    [Header("ใส่รูปสัญลักษณ์ทั้ง 4 แบบ")]
    public Sprite[] symbolSprites = new Sprite[4];

    [Header("รหัสที่ถูกต้อง (ช่อง 1-4) ค่า = ลำดับของรูปใน symbolSprites (0-3)")]
    public int[] code = { 2, 0, 3, 1 };

    bool[] found = new bool[4];

    void Awake() { Instance = this; }

    [Header("UI")]
    public GameObject chestPuzzle;
    public Image[] icons;
    public Button[] buttons;
    public Button closeBtn;

    private int[] currentIcon = { 0, 0, 0, 0 };
    private bool isResolved = false;

    // รูปคำใบ้ของช่องที่ slot (0-3)
    public Sprite GetClueSprite(int slot) { return symbolSprites[code[slot]]; }

    void Start()
    {
        closeBtn.onClick.AddListener(() => SetActiveChestPuzzle(false));
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[index].onClick.AddListener(() => UpdateSlotIcon(index));
        }
        SetActiveChestPuzzle(false);
    }

    public void MarkFound(int slot)
    {
        found[slot] = true;
        Debug.Log("พบคำใบ้ช่องที่ " + (slot + 1));
    }

    public bool CheckCode(int[] input)
    {
        for (int i = 0; i < code.Length; i++)
            if (input[i] != code[i]) return false;
        return true;
    }

    public void SetActiveChestPuzzle(bool active)
    {
        if (isResolved) return;
        chestPuzzle.SetActive(active);
    }

    private void UpdateSlotIcon(int index)
    {
        int current = currentIcon[index];
        current++;
        if (current > symbolSprites.Length - 1) current = 0;
        icons[index].sprite = symbolSprites[current];
        currentIcon[index] = current;

        if (CheckCode(currentIcon))
        {
            SetActiveChestPuzzle(false);
            isResolved = true;
            // PUZZLE แก้ได้แล้ว จะทำอะไรต่อก็ทำเลย
        }
    }
}

