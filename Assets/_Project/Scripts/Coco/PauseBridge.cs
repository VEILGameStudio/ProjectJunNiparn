// PauseBridge
// Lets Inspector buttons and Screen Switcher events pause and resume the game
// through the PauseManager. PauseManager needs a "source" object, which the
// Inspector cannot pass, so this script passes itself as the source.
//
// Put this on: the "Screens" object in the Test_Zones scene.
// Assign in Inspector: nothing required.
// Use it: Screen Switcher > Pause_Popup > On Opened -> PauseBridge.PauseGame
//         Screen Switcher > Pause_Popup > On Closed -> PauseBridge.ResumeGame

using UnityEngine;

public class PauseBridge : MonoBehaviour
{
    // Freezes the game.
    public void PauseGame()
    {
        if (GameManager.Instance == null || GameManager.Instance.PauseManager == null)
        {
            Debug.LogError("PauseBridge: GameManager or PauseManager not found in this scene. Add the Managers object.", this);
            return;
        }

        GameManager.Instance.PauseManager.Pause(this);
    }

    // Unfreezes the game (only when nothing else is still asking for a pause).
    public void ResumeGame()
    {
        if (GameManager.Instance == null || GameManager.Instance.PauseManager == null)
        {
            return;
        }

        GameManager.Instance.PauseManager.Resume(this);
    }
}