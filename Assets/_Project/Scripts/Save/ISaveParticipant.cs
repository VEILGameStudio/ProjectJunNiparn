// ISaveParticipant
// Any system that wants its data saved (for example the Inventory or the Player's
// position) implements this. It registers with the SaveManager, which then asks
// it to write its data into the save (CaptureState) and read it back (RestoreState).
// This keeps the SaveManager simple: it does not need to know about every system.
//
// Put this on: nothing directly. Scripts implement this interface, e.g.:
//   public class Inventory : MonoBehaviour, ISaveParticipant { ... }

public interface ISaveParticipant
{
    // Copy this system's current data INTO the save object.
    void CaptureState(GameSaveData data);

    // Read this system's data FROM the save object and apply it.
    void RestoreState(GameSaveData data);
}
