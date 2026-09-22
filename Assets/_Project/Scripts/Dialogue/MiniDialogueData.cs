// MiniDialogueData
// One short line of text, shown as a small popup (for example a hint or a comment).
// It is displayed by the MiniDialogueUI.
//
// Create by hand with:
//   Right-click in Project > Create > Game > Dialogue > Mini
//
// Put this on: nothing. It is a data asset.
// Assign in Inspector: Id and Text.

using UnityEngine;

[CreateAssetMenu(fileName = "MiniDialogue", menuName = "Game/Dialogue/Mini")]
public class MiniDialogueData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("A unique short id, for example 'locked_door'. The importer uses this to name the asset.")]
    [SerializeField] private string id;

    [Header("Text")]
    [Tooltip("The short line shown in the popup.")]
    [SerializeField] private LocalizedString text;

    // The unique id of this mini dialogue.
    public string Id => id;

    // The short line to show.
    public LocalizedString Text => text;

    // Used by the importer to fill this asset from JSON.
    public void SetData(string newId, LocalizedString newText)
    {
        id = newId;
        text = newText;
    }
}
