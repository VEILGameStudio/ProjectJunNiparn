// YSortObject
// Draws things that are further back behind things that are nearer. It sets the
// sorting order from the Y position: higher on the screen means further back.
//   sortingOrder = Mathf.RoundToInt(-y * 100)
//
// Sort Mode:
//   Auto (default) - sorts by this object's own Y. The sprite pivot must be at the feet.
//   Manual Pivot   - sorts by the Y of a child object you pick instead.
//
// When to use Manual Pivot: for anything drawn HIGH on the screen that really stands
// in FRONT of what is around it - a cat sitting in a tree, a lantern hanging from the
// ceiling, a sign on a wall, a branch overhead. Its own Y is high on the screen, so
// Auto thinks it is far back and draws it behind things it should cover.
// The fix: add an empty child object, move that child DOWN to the spot on the floor
// directly under the object (where it would stand if it were on the ground), and
// drag that child into Sort Pivot.
//
// Tick Static for props that never move: the order is worked out once, in Start.
// Leave it unticked for anything that moves (the player, a box that gets pushed).
//
// Put this on: every sprite object that should sort by depth (props, characters).
//   For an object made of several sprites, also add a Sorting Group to its root;
//   this script then sorts the whole group together.
// Assign in Inspector: Sort Mode, Sort Pivot (Manual Pivot only), Static.

using UnityEngine;
using UnityEngine.Rendering;

public class YSortObject : MonoBehaviour
{
    private enum SortMode { Auto, ManualPivot }

    [Header("Sorting")]
    [Tooltip("Auto = sort by this object's own Y. Manual Pivot = sort by the Sort Pivot child's Y (for things drawn high up but standing in front, like a hanging lantern).")]
    [SerializeField] private SortMode sortMode = SortMode.Auto;

    [Tooltip("Manual Pivot only. A child object placed on the floor directly under this object. Its Y decides the draw order.")]
    [SerializeField] private Transform sortPivot;

    [Tooltip("Tick for props that never move. The order is worked out once when the scene starts.")]
    [SerializeField] private bool isStatic;

    // Unity's sorting order only holds numbers from -32768 to 32767.
    private const int MaxSortingOrder = 32767;

    private SortingGroup sortingGroup;
    private SpriteRenderer spriteRenderer;
    private bool hasWarnedOutOfRange;

    // The Y this object sorts by. Lower Y is drawn in front.
    public float SortY
    {
        get
        {
            if (sortMode == SortMode.ManualPivot && sortPivot != null)
            {
                return sortPivot.position.y;
            }
            return transform.position.y;
        }
    }

    private void Awake()
    {
        sortingGroup = GetComponent<SortingGroup>();
        if (sortingGroup == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (sortingGroup == null && spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (sortingGroup == null && spriteRenderer == null)
        {
            Debug.LogError($"YSortObject on '{name}': no SpriteRenderer or Sorting Group was found on this object or its children, so nothing can be sorted.", this);
        }
        if (sortMode == SortMode.ManualPivot && sortPivot == null)
        {
            Debug.LogError($"YSortObject on '{name}': Sort Mode is Manual Pivot but Sort Pivot is empty, so the object's own Y is used. Add a child object on the floor under this object and drag it into Sort Pivot.", this);
        }
    }

    private void Start()
    {
        ApplySortingOrder();
    }

    private void LateUpdate()
    {
        if (!isStatic)
        {
            ApplySortingOrder();
        }
    }

    // Turns a Y position into a sorting order. Higher Y = further back = lower order.
    public static int CalculateSortingOrder(float y)
    {
        return Mathf.RoundToInt(-y * 100f);
    }

    // Sets the order on the Sorting Group if there is one, otherwise on the SpriteRenderer.
    private void ApplySortingOrder()
    {
        int order = KeepInRange(CalculateSortingOrder(SortY));

        if (sortingGroup != null)
        {
            sortingGroup.sortingOrder = order;
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = order;
        }
    }

    // Keeps the order inside what Unity allows, and warns once if the object is too far away.
    private int KeepInRange(int order)
    {
        if (order >= -MaxSortingOrder && order <= MaxSortingOrder)
        {
            return order;
        }

        if (!hasWarnedOutOfRange)
        {
            hasWarnedOutOfRange = true;
            Debug.LogWarning($"YSortObject on '{name}': Y is too far from 0 to sort correctly. Keep levels between Y -327 and 327.", this);
        }
        return Mathf.Clamp(order, -MaxSortingOrder, MaxSortingOrder);
    }
}
