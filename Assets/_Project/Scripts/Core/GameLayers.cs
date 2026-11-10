// GameLayers
// The physics layer names this game uses, kept in one place so no script types a
// layer name by hand. The layers themselves live in Project Settings > Tags and Layers.
//   Blocking     - small solid colliders at the base of props, and walls. The player bumps into them.
//   Interactable - trigger boxes around the visible sprite or mesh. The mouse ray clicks them.
//   TouchZone    - trigger areas the player walks into to use something
//                  (a floor plate, a cutscene spot, a walk-through doorway).
//
// Put this on: nothing. It is a static helper used by other scripts.

using UnityEngine;

public static class GameLayers
{
    public const string Blocking = "Blocking";
    public const string Interactable = "Interactable";
    public const string TouchZone = "TouchZone";

    // A layer mask with only the Interactable layer in it (used for mouse clicks).
    public static int InteractableMask => LayerMask.GetMask(Interactable);

    // True if the GameObject is on the named layer.
    public static bool IsOnLayer(GameObject target, string layerName)
    {
        return target.layer == LayerMask.NameToLayer(layerName);
    }

    // Logs a clear error if a layer this game needs has not been created yet.
    public static void CheckExists(string layerName, Object context)
    {
        if (LayerMask.NameToLayer(layerName) < 0)
        {
            Debug.LogError($"GameLayers: the layer '{layerName}' does not exist. Add it in Project Settings > Tags and Layers.", context);
        }
    }
}
