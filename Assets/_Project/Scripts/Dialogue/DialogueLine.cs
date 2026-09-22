// DialogueLine
// One line in a full conversation: who is speaking and what they say. Both are
// LocalizedStrings so they can be shown in any language.
//
// Put this on: nothing. It is a small data class used inside FullDialogueData.

[System.Serializable]
public class DialogueLine
{
    // The name of the person speaking this line.
    public LocalizedString speaker;

    // What they say.
    public LocalizedString text;
}
