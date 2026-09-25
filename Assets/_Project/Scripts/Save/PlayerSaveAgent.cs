// PlayerSaveAgent
// Saves and restores the player's position (X and Y). It registers with the SaveManager, so
// when the game saves it writes where the player is, and when a saved game loads it
// moves the player back to that spot. The scene name is saved by the SaveManager
// itself, so this only handles position.
//
// Put this on: the Player GameObject.
// Assign in Inspector: nothing required.

using UnityEngine;

public class PlayerSaveAgent : MonoBehaviour, ISaveParticipant
{
    private void Start()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Register(this);
        }
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Unregister(this);
        }
    }

    // Writes the player's current position into the save.
    public void CaptureState(GameSaveData data)
    {
        data.playerPositionX = transform.position.x;
        data.playerPositionY = transform.position.y;
    }

    // Moves the player to the saved position (keeps the current Z).
    public void RestoreState(GameSaveData data)
    {
        transform.position = new Vector3(data.playerPositionX, data.playerPositionY, transform.position.z);
    }
}
