using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ForestPuzzleManager : MonoBehaviour
{
    public static ForestPuzzleManager Instance;

    [Header("ตัวเลขจำนวนชิ้นส่วน")]
    public TMP_Text leafCount, branchCount, redCount, yellowCount, blueCount;

    [Header("จุดความคืบหน้า")]
    public Image[] progressDots;
    public Color dotOn = Color.yellow, dotOff = Color.gray;

    public int totalSlots = 5;   // จำนวน Slot ทั้งหมดบนต้นไม้

    Dictionary<PieceType, int> counts = new Dictionary<PieceType, int>()
    {
        { PieceType.Leaf, 4 },
        { PieceType.Branch, 3 },
        { PieceType.RedFruit, 4 },
        { PieceType.YellowFruit, 3 },
        { PieceType.BlueFruit, 3 }
    };
    int placed;

    void Awake()
    {
        Instance = this;
        RefreshUI();
    }

    public int GetCount(PieceType t)
    {
        return counts[t];
    }

    public void OnPiecePlaced(PieceType t)
    {
        counts[t]--;
        placed++;
        RefreshUI();

        if (placed >= totalSlots)
            Debug.Log("ต้นไม้สมบูรณ์!");   // ใส่ตรงนี้: เปิดทาง/เล่นเอฟเฟกต์
    }

    void RefreshUI()
    {
        if (leafCount) leafCount.text = counts[PieceType.Leaf].ToString();
        if (branchCount) branchCount.text = counts[PieceType.Branch].ToString();
        if (redCount) redCount.text = counts[PieceType.RedFruit].ToString();
        if (yellowCount) yellowCount.text = counts[PieceType.YellowFruit].ToString();
        if (blueCount) blueCount.text = counts[PieceType.BlueFruit].ToString();

        for (int i = 0; i < progressDots.Length; i++)
            progressDots[i].color = i < placed ? dotOn : dotOff;
    }
}