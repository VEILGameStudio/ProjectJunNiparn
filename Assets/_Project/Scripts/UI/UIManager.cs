// UIManager
// The one place other scripts use to reach the shared screen parts: the mini
// dialogue popup, the dialogue panels, and the cutscene player. Example:
//   UIManager.Instance.MiniDialogue.Show("Hello");
// The PersistentCanvas survives scene changes, so these are always available.
//
// Put this on: the root of the PersistentCanvas prefab.
// Assign in Inspector: Mini Dialogue, Dialogue, Cutscene. Leave them empty when
//   those components are on this object or its children - they are found automatically.

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

    // The parts other scripts can reach through UIManager.Instance.
    public MiniDialogueUI MiniDialogue => miniDialogue;
    public DialogueManager Dialogue => dialogue;
    public CutsceneManager Cutscene => cutscene;

    protected override void Awake()
    {
        base.Awake();
        FindMissingParts();
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
            Debug.LogError($"UIManager on '{name}': no {componentName} found. Add a {componentName} to the PersistentCanvas, or drag one into its field.", this);
        }
    }
}
