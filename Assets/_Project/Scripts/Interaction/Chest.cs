using UnityEngine;

public class Chest : InteractableBase
{
    
    protected override void OnInteract(PlayerContext player)
    {
        PuzzleManager.Instance.SetActiveChestPuzzle(true);
    }
}
