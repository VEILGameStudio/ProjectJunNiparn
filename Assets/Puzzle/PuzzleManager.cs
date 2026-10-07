using UnityEngine;
using UnityEngine.UI;

// ตัวกลางเก็บ "รหัสที่ถูกต้อง" ไว้ที่เดียว
// คำใบ้ทุกจุด (กรอบรูป/นาฬิกา/ลิ้นชัก/ปฏิทิน) ดึงรูปจากรหัสนี้ จึงตรงกับรหัสปลดล็อคแน่นอน
public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance;
    [SerializeField] private GameObject boxImage;

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
    private bool isCodeCorrect = false;   // รหัสถูกแล้ว พร้อมกดไปต่อ


    // รูปคำใบ้ของช่องที่ slot (0-3)
    public Sprite GetClueSprite(int slot) { return symbolSprites[code[slot]]; }

    public Button confirmBtn;

    void Start()
    {
        closeBtn.onClick.AddListener(() => SetActiveChestPuzzle(false));
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[index].onClick.AddListener(() => UpdateSlotIcon(index));

            confirmBtn.interactable = false;
            confirmBtn.onClick.AddListener(ConfirmPuzzle);
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
        if (isResolved) return;

        int current = currentIcon[index];
        current++;
        if (current > symbolSprites.Length - 1) current = 0;
        icons[index].sprite = symbolSprites[current];
        currentIcon[index] = current;

        // เช็ครหัสทุกครั้งที่เปลี่ยนรูป แต่ยังไม่ปลดล็อค
        isCodeCorrect = CheckCode(currentIcon);
        confirmBtn.interactable = isCodeCorrect;
        if (isCodeCorrect)Debug.Log("รหัสถูกต้อง! กดยืนยันเพื่อไปต่อ");
    }

    // ผูกกับปุ่ม "ยืนยัน"
    public void ConfirmPuzzle()
    {
        if (isResolved) return;

        if (isCodeCorrect)
        {
            Debug.Log("ยืนยันคำตอบถูกต้อง!");
            isResolved = true;
            chestPuzzle.SetActive(false);
            // PUZZLE แก้ได้แล้ว จะทำอะไรต่อก็ทำตรงนี้
            isResolved = true;
            chestPuzzle.SetActive(false);
            boxImage.SetActive(true);   // โชว์ภาพกล่อง
        }
        else
        {
            Debug.Log("คำตอบไม่ถูกต้อง!");
        }
    }

    
  
}
    




