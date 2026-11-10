// LocalizedString
// A piece of text written in every supported language. Drop this inside any
// ScriptableObject or script (for example an item name or a dialogue line) and
// fill in each language in the Inspector.
// To turn it into the player's chosen language at runtime, call:
//   GameManager.Instance.Localization.Get(myLocalizedString)
//
// Put this on: nothing directly. It is a field used inside other data objects.
// Assign in Inspector: the English and Thai text boxes.

using UnityEngine;

[System.Serializable]
public class LocalizedString
{
    [Tooltip("The text shown when the language is English.")]
    [TextArea] public string english;

    [Tooltip("The text shown when the language is Thai.")]
    [TextArea] public string thai;

    // Returns the text for the given language. Falls back to English if a
    // translation is empty so the game never shows a blank line.
    public string ForLanguage(Language language)
    {
        switch (language)
        {
            case Language.Thai:
                return string.IsNullOrEmpty(thai) ? english : thai;
            default:
                return english;
        }
    }
}
