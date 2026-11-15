// UIManager
// The one place other scripts use to reach the shared screen parts, and the one place
// that opens and closes panels.
//   UIManager.Instance.MiniDialogue.Show("Hello");
//   UIManager.Instance.Open("Pause");      // a UIPanel with Panel Id "Pause"
//   UIManager.Instance.CloseTop();         // closes the panel opened last
// The UIRoot prefab survives scene changes, so every scene shares this one UI.
// Panels are every UIPanel inside UIRoot. They are found automatically when the game
// starts and all start closed, except those with Start Open ticked.
//
// Put this on: the root of the UIRoot prefab (Assets/_Project/Prefabs/UI/UIRoot.prefab).
// Assign in Inspector: Mini Dialogue, Dialogue, Cutscene. Leave them empty when
//   those components are on this object or its children - they are found automatically.

using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [Header("Screen Parts (found automatically if on this object or its children)")]
    [Tooltip("The MiniDialogueUI that shows short popup messages.")]
    [SerializeField] private MiniDialogueUI miniDialogue;

    [Tooltip("The DialogueManager that plays full and choice dialogues.")]
    [SerializeField] private DialogueManager dialogue;

    [Tooltip("The CutsceneManager that plays Timeline and video cutscenes.")]
    [SerializeField] private CutsceneManager cutscene;

    // Every UIPanel inside UIRoot.
    private readonly List<UIPanel> panels = new List<UIPanel>();

    // The panels open right now, oldest first.
    private readonly List<UIPanel> openPanels = new List<UIPanel>();

    // The parts other scripts can reach through UIManager.Instance.
    public MiniDialogueUI MiniDialogue => miniDialogue;
    public DialogueManager Dialogue => dialogue;
    public CutsceneManager Cutscene => cutscene;

    // True while at least one panel is open.
    public bool IsAnyPanelOpen => openPanels.Count > 0;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return; // A duplicate that is being destroyed.
        }
        FindMissingParts();
        FindPanels();
    }

    private void Start()
    {
        foreach (UIPanel panel in panels)
        {
            if (panel.StartOpen)
            {
                Open(panel);
            }
        }
    }

    // Opens the panel with this Panel Id.
    public void Open(string panelId)
    {
        UIPanel panel = GetPanel(panelId);
        if (panel != null)
        {
            Open(panel);
        }
    }

    // Opens a panel. Panels already open stay open; the new one draws where it sits in UIRoot.
    public void Open(UIPanel panel)
    {
        if (!IsKnownPanel(panel) || openPanels.Contains(panel))
        {
            return;
        }

        openPanels.Add(panel);
        SetPaused(panel, true);
        panel.ShowNow();
    }

    // Closes the panel with this Panel Id.
    public void Close(string panelId)
    {
        UIPanel panel = GetPanel(panelId);
        if (panel != null)
        {
            Close(panel);
        }
    }

    // Closes a panel. The game unfreezes only when no other pausing panel is still open.
    public void Close(UIPanel panel)
    {
        if (panel == null || !openPanels.Remove(panel))
        {
            return;
        }

        SetPaused(panel, false);
        panel.HideNow();
    }

    // Opens the panel when it is closed, closes it when it is open.
    public void Toggle(UIPanel panel)
    {
        if (IsOpen(panel))
        {
            Close(panel);
        }
        else
        {
            Open(panel);
        }
    }

    // Closes the panel that was opened last. Use it for a Back or Cancel button.
    public void CloseTop()
    {
        if (openPanels.Count > 0)
        {
            Close(openPanels[openPanels.Count - 1]);
        }
    }

    // Closes every open panel.
    public void CloseAll()
    {
        for (int i = openPanels.Count - 1; i >= 0; i--)
        {
            Close(openPanels[i]);
        }
    }

    public bool IsOpen(UIPanel panel)
    {
        return panel != null && openPanels.Contains(panel);
    }

    // Finds a panel by its Panel Id, or logs an error naming the id.
    public UIPanel GetPanel(string panelId)
    {
        foreach (UIPanel panel in panels)
        {
            if (panel.PanelId == panelId)
            {
                return panel;
            }
        }
        Debug.LogError($"UIManager on '{name}': no UIPanel with Panel Id '{panelId}'. Check the spelling, and that the panel prefab sits inside the UIRoot prefab.", this);
        return null;
    }

    // Collects every UIPanel inside UIRoot, closes them, and reports empty or repeated ids.
    private void FindPanels()
    {
        panels.Clear();
        panels.AddRange(GetComponentsInChildren<UIPanel>(true));

        HashSet<string> seenIds = new HashSet<string>();
        foreach (UIPanel panel in panels)
        {
            if (string.IsNullOrEmpty(panel.PanelId))
            {
                Debug.LogError($"UIManager on '{name}': the UIPanel on '{panel.name}' has no Panel Id. Type a short unique name into its Panel Id field.", panel);
            }
            else if (!seenIds.Add(panel.PanelId))
            {
                Debug.LogError($"UIManager on '{name}': two panels use the Panel Id '{panel.PanelId}'. Give '{panel.name}' a different Panel Id.", panel);
            }
            panel.HideSilently();
        }
    }

    private bool IsKnownPanel(UIPanel panel)
    {
        if (panel == null)
        {
            Debug.LogError($"UIManager on '{name}': asked to open an empty panel. Drag a UIPanel into the button's box.", this);
            return false;
        }
        if (!panels.Contains(panel))
        {
            Debug.LogError($"UIManager on '{name}': '{panel.name}' is not inside the UIRoot prefab, so it cannot be opened. Move the panel prefab inside UIRoot.", panel);
            return false;
        }
        return true;
    }

    // Freezes or unfreezes the game for a panel that has Pauses Game ticked.
    private void SetPaused(UIPanel panel, bool paused)
    {
        if (!panel.PausesGame)
        {
            return;
        }
        if (GameManager.Instance == null || GameManager.Instance.PauseManager == null)
        {
            if (paused)
            {
                Debug.LogError($"UIManager on '{name}': panel '{panel.PanelId}' wants to pause the game, but no GameManager with a PauseManager was found. Make sure the GameSystems prefab is in the scene.", panel);
            }
            return;
        }

        if (paused)
        {
            GameManager.Instance.PauseManager.Pause(panel);
        }
        else
        {
            GameManager.Instance.PauseManager.Resume(panel);
        }
    }

    // Fills empty fields from this object or its children, then reports anything still missing.
    private void FindMissingParts()
    {
        if (miniDialogue == null) miniDialogue = GetComponentInChildren<MiniDialogueUI>(true);
        if (dialogue == null) dialogue = GetComponentInChildren<DialogueManager>(true);
        if (cutscene == null) cutscene = GetComponentInChildren<CutsceneManager>(true);

        ReportIfMissing(miniDialogue, "MiniDialogueUI");
        ReportIfMissing(dialogue, "DialogueManager");
        ReportIfMissing(cutscene, "CutsceneManager");
    }

    // Logs a clear error if a part could not be found.
    private void ReportIfMissing(Object part, string componentName)
    {
        if (part == null)
        {
            Debug.LogError($"UIManager on '{name}': no {componentName} found. Add a {componentName} to the UIRoot prefab, or drag one into its field.", this);
        }
    }
}
