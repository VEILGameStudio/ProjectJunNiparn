// CookieBoxCloseUp
// The close-up view of the taped cookie box. It opens when the player clicks the cookie box
// in the world, and the game is paused while it is open.
//   - Player has no Cutter: only the taped box is shown.
//   - Player has the Cutter: the cutter is shown next to the box. Clicking it cuts the tape,
//     the lid slides off, and the box stays open from then on.
// There is no key: what the open box unlocks is wired on CookieBoxInteractable's On Opened.
//
// Put this on: the "CloseUp" panel inside the CookieBoxPuzzle prefab (under its own Canvas).
//   The panel starts switched off. CookieBoxInteractable switches it on.
// Assign in Inspector: Cutter Item, Cutter Button, Cut Layer, Tape Scraps, Opened Layer, Lid,
//   Close Button. The prefab made by Tools > JunNiparn > Create Cookie Box Prefab has them all.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CookieBoxCloseUp : MonoBehaviour
{
    [Header("Cutter")]
    [Tooltip("The item that cuts the tape (Cutter_Item). The cutter only appears in the close-up when the player has this item.")]
    [SerializeField] private ItemData cutterItem;

    [Tooltip("Tick if carrying the cutter in the bag is not enough: the player must be HOLDING it (hand slot of the inventory screen).")]
    [SerializeField] private bool cutterMustBeHeld;

    [Tooltip("The cutter picture the player clicks to cut the tape. Hidden when the player has no cutter.")]
    [SerializeField] private Button cutterButton;

    [Header("Box Layers (all switched off at the start)")]
    [Tooltip("Shown as soon as the tape is cut: the box with the tape gone (cookie.png).")]
    [SerializeField] private GameObject cutLayer;

    [Tooltip("The bits of cut tape (tape1.png). Shown when the tape is cut.")]
    [SerializeField] private GameObject[] tapeScraps;

    [Tooltip("The inside of the open box (Incookie.png). Shown when the lid comes off.")]
    [SerializeField] private GameObject openedLayer;

    [Tooltip("The lid (Cookie jar lid.png). It slides away when the box opens.")]
    [SerializeField] private RectTransform lid;

    [Header("Buttons")]
    [Tooltip("The button that closes the close-up and goes back to the game.")]
    [SerializeField] private Button closeButton;

    [Header("Timing (real seconds: the game is paused while this is open)")]
    [Tooltip("How long the cut box is shown before the lid comes off.")]
    [SerializeField] private float cutToOpenDelay = 0.6f;

    [Tooltip("How long the lid takes to slide away.")]
    [SerializeField] private float lidSlideTime = 0.8f;

    [Tooltip("How far the lid slides, in canvas units (the canvas is 1920 x 1080).")]
    [SerializeField] private Vector2 lidSlideOffset = new Vector2(300f, 80f);

    // Raised once, when the lid is off. CookieBoxInteractable listens to it.
    public event System.Action Opened;

    private Inventory inventory;
    private bool boxOpened;
    private bool lidStartSaved;
    private Vector2 lidStartPosition;
    private Coroutine cutRoutine;

    // True once the tape was cut and the box opened (now or in a loaded save).
    public bool BoxOpened => boxOpened;

    private void OnEnable()
    {
        if (cutterButton != null)
        {
            cutterButton.onClick.AddListener(CutTape);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
    }

    private void OnDisable()
    {
        if (cutterButton != null)
        {
            cutterButton.onClick.RemoveListener(CutTape);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
        }

        // Closed in the middle of cutting: finish at once, so the cut is never lost.
        if (cutRoutine != null)
        {
            cutRoutine = null;
            FinishOpening();
        }

        ResumeGame();
    }

    // Opens the close-up for a player with this inventory and pauses the game.
    public void Show(Inventory playerInventory)
    {
        inventory = playerInventory;
        CheckReferences();
        gameObject.SetActive(true);
        PauseGame();
        RefreshCutter();
    }

    // Closes the close-up. The game unpauses in OnDisable.
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // Puts the box straight into its open state, with no animation and no event.
    // Used after loading a save where the box was already opened. Safe to call twice.
    public void ShowOpenedState()
    {
        SaveLidStart();
        boxOpened = true;

        SetActive(cutLayer, true);
        SetScraps(true);
        SetActive(openedLayer, true);
        if (lid != null)
        {
            lid.gameObject.SetActive(true);
            lid.anchoredPosition = lidStartPosition + lidSlideOffset;
        }
        RefreshCutter();
    }

    // True when the cutter should appear: the box is still taped and the player has the cutter.
    // Public and static so a test can check it without a scene.
    public static bool CanShowCutter(Inventory inventory, ItemData cutter, bool mustBeHeld, bool boxAlreadyOpened)
    {
        if (boxAlreadyOpened || cutter == null)
        {
            return false;
        }
        return InteractableBase.CheckRequiredItem(inventory, cutter, mustBeHeld) == RequiredItemResult.Ready;
    }

    // Shows or hides the cutter for the current inventory.
    private void RefreshCutter()
    {
        if (cutterButton == null)
        {
            return;
        }
        bool show = cutRoutine == null && CanShowCutter(inventory, cutterItem, cutterMustBeHeld, boxOpened);
        cutterButton.gameObject.SetActive(show);
    }

    // Called by the cutter button: starts the cut-and-open sequence.
    private void CutTape()
    {
        if (boxOpened || cutRoutine != null)
        {
            return;
        }
        cutRoutine = StartCoroutine(CutAndOpen());
        RefreshCutter();
    }

    // Tape cut -> short pause -> lid slides off. Uses real time because the game is paused.
    private IEnumerator CutAndOpen()
    {
        SaveLidStart();
        SetActive(cutLayer, true);
        SetScraps(true);

        yield return new WaitForSecondsRealtime(cutToOpenDelay);

        SetActive(openedLayer, true);
        if (lid != null)
        {
            lid.gameObject.SetActive(true);
            Vector2 from = lidStartPosition;
            Vector2 to = lidStartPosition + lidSlideOffset;
            float progress = 0f;
            while (progress < 1f)
            {
                progress += Time.unscaledDeltaTime / Mathf.Max(0.01f, lidSlideTime);
                lid.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress)));
                yield return null;
            }
        }

        cutRoutine = null;
        FinishOpening();
    }

    // Leaves the box open and tells CookieBoxInteractable, once.
    private void FinishOpening()
    {
        bool firstTime = !boxOpened;
        ShowOpenedState();
        if (firstTime && Opened != null)
        {
            Opened.Invoke();
        }
    }

    private void SaveLidStart()
    {
        if (lidStartSaved || lid == null)
        {
            return;
        }
        lidStartPosition = lid.anchoredPosition;
        lidStartSaved = true;
    }

    private void SetScraps(bool visible)
    {
        if (tapeScraps == null)
        {
            return;
        }
        foreach (GameObject scrap in tapeScraps)
        {
            SetActive(scrap, visible);
        }
    }

    private static void SetActive(GameObject target, bool visible)
    {
        if (target != null)
        {
            target.SetActive(visible);
        }
    }

    private void PauseGame()
    {
        if (GameManager.Instance != null && GameManager.Instance.PauseManager != null)
        {
            GameManager.Instance.PauseManager.Pause(this);
        }
    }

    private void ResumeGame()
    {
        if (GameManager.Instance != null && GameManager.Instance.PauseManager != null)
        {
            GameManager.Instance.PauseManager.Resume(this);
        }
    }

    // Logs a clear message for every missing reference, so a broken prefab is easy to fix.
    private void CheckReferences()
    {
        if (cutterItem == null)
        {
            Debug.LogError($"CookieBoxCloseUp on '{name}': Cutter Item is not assigned. Drag Cutter_Item (Assets/Puzzle/box) here.", this);
        }
        if (cutterButton == null)
        {
            Debug.LogError($"CookieBoxCloseUp on '{name}': Cutter Button is not assigned. Drag the 'Cutter' child here.", this);
        }
        if (closeButton == null)
        {
            Debug.LogError($"CookieBoxCloseUp on '{name}': Close Button is not assigned. Drag the 'CloseButton' child here, or the player cannot leave the close-up.", this);
        }
    }
}
