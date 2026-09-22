// PlayerSaveAgent
// Saves and restores the player's position. It registers with the SaveManager, so
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
        Vector3 position = transform.position;
        data.playerPositionX = position.x;
        data.playerPositionY = position.y;
        data.playerPositionZ = position.z;
    }

    // Moves the player to the saved position.
    public void RestoreState(GameSaveData data)
    {
        transform.position = new Vector3(data.playerPositionX, data.playerPositionY, data.playerPositionZ);
    }
}
