// CameraPreset
// The camera presets a level can pick in LevelSettings, and the numbers behind each one.
// The whole game uses ONE camera angle, because every sprite is drawn for that angle.
// That is why there is only "Default" for now. Never add a preset with a different
// Pitch or Yaw: that would mean redrawing every sprite (ask Oak first).
//
// Put this on: nothing. LevelSettings uses it.

using UnityEngine;

public enum CameraPreset
{
    Default
}

// The numbers a camera preset stands for.
public struct CameraPresetValues
{
    public float Pitch;             // How many degrees the camera looks down (0 = straight ahead, 90 = straight down).
    public float Yaw;               // How many degrees the camera is turned around the vertical axis (0 = looks along +Z).
    public float OrthographicSize;  // Zoom: half the screen height in world units. Bigger = sees more.
    public float CameraDistance;    // How far back from the player the camera sits, along its view direction.
    public Vector2 Damping;         // Follow smoothing: X = left/right on screen, Y = up/down on screen.

    // Returns the numbers for a preset.
    public static CameraPresetValues For(CameraPreset preset)
    {
        switch (preset)
        {
            case CameraPreset.Default:
            default:
                return new CameraPresetValues
                {
                    Pitch = 45f,
                    Yaw = 0f,
                    OrthographicSize = 5f,
                    CameraDistance = 20f,
                    Damping = new Vector2(1f, 1.3f)
                };
        }
    }
}
