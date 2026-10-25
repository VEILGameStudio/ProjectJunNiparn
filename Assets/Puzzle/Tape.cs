using UnityEngine;

public class Tape : MonoBehaviour
{
    //[SerializeField] private Cutter cutter;
    [SerializeField] private BoxPuzzle boxPuzzle;

    [Tooltip("ระยะที่ต้องลากผ่านเทปให้ครบ ถึงจะถือว่าตัดขาด")]
    [SerializeField] private float cutDistanceNeeded = 3f;

    private Collider2D area;
    private Vector3 lastTipPos;
    private float cutDistance;
    private bool done;
    private bool wasHeld;

    private void Awake()
    {
        area = GetComponent<Collider2D>();
    }

    private void Update()
    {
        //if (cutter == null || boxPuzzle == null) return;

      //  Vector3 tipPos = cutter.TipPosition;

      //  bool canCut = !done
          //  && wasHeld // ต้องถือมาตั้งแต่เฟรมก่อน กันตำแหน่งกระโดดตอนหยิบ
          //  && cutter.IsHeld
          //  && boxPuzzle.State == BoxPuzzle.BoxState.Closed
         //   && Input.GetMouseButton(0)
          //  && area.OverlapPoint(tipPos);

       // if (canCut)
        {
          //  cutDistance += Vector3.Distance(tipPos, lastTipPos);

            if (cutDistance >= cutDistanceNeeded)
            {
                done = true;
                boxPuzzle.OnTapeCut();
            }
        }

       // lastTipPos = tipPos;
        //wasHeld = cutter.IsHeld;
    }
}