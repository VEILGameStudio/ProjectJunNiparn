// ChoiceDialogueData
// A yes/no question shown to the player. It only holds the TEXT (question and
// button labels). What actually happens on Yes or No is set in the scene on a
// ChoiceInteractable, because those actions point at scene objects.
//
// Create by hand with:
//   Right-click in Project > Create > Game > Dialogue > Choice
//
// Put this on: nothing. It is a data asset.
// Assign in Inspector: Id, Question, Yes Label, No Label.

using UnityEngine;

[CreateAssetMenu(fileName = "ChoiceDialogue", menuName = "Game/Dialogue/Choice")]
public class ChoiceDialogueData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("A unique short id, for example 'take_gem'. The importer uses this to name the asset.")]
    [SerializeField] private string id;

    [Header("Text")]
    [Tooltip("The question shown to the player.")]
    [SerializeField] private LocalizedString question;

    [Tooltip("The label on the Yes button.")]
    [SerializeField] private LocalizedString yesLabel;

    [Tooltip("The label on the No button.")]
    [SerializeField] private LocalizedString noLabel;

    // The unique id of this choice.
    public string Id => id;

    // The question text.
    public LocalizedString Question => question;

    // The Yes button label.
    public LocalizedString YesLabel => yesLabel;

    // The No button label.
    public LocalizedString NoLabel => noLabel;

    // Used by the importer to fill this asset from JSON.
    public void SetData(string newId, LocalizedString newQuestion, LocalizedString newYesLabel, LocalizedString newNoLabel)
    {
        id = newId;
        question = newQuestion;
        yesLabel = newYesLabel;
        noLabel = newNoLabel;
    }
}
