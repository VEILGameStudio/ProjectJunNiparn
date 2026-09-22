// FullDialogueData
// A whole conversation: a list of lines the player clicks through, each with a
// speaker and text. The writer normally creates this from a JSON file using the
// menu Tools > Dialogue > Import JSON, but you can also make one by hand.
//
// Create by hand with:
//   Right-click in Project > Create > Game > Dialogue > Full
//
// Put this on: nothing. It is a data asset.
// Assign in Inspector: Id and the Lines list.

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FullDialogue", menuName = "Game/Dialogue/Full")]
public class FullDialogueData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("A unique short id, for example 'intro_grandpa'. The importer uses this to name the asset.")]
    [SerializeField] private string id;

    [Header("Lines")]
    [Tooltip("The lines shown one after another, in order.")]
    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    // The unique id of this conversation.
    public string Id => id;

    // The lines to play, in order.
    public IReadOnlyList<DialogueLine> Lines => lines;

    // Used by the importer to fill this asset from JSON.
    public void SetData(string newId, List<DialogueLine> newLines)
    {
        id = newId;
        lines = newLines;
    }
}
