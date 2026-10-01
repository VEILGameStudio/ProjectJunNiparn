// CameraPreset
// The camera presets a level can pick in LevelSettings, and the numbers behind each one.
// The whole game uses ONE camera angle, because every sprite is drawn for that angle.
// The angle is isometric-style: the camera is turned 45 degrees (Yaw), so rooms look like
// diamonds on screen, and it looks 30 degrees down (Pitch), so a square floor tile is
// drawn twice as wide as it is tall.
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
                    Pitch = 30f,
                    Yaw = 45f,
                    OrthographicSize = 5f,
                    CameraDistance = 20f,
                    Damping = new Vector2(1f, 1.3f)
                };
        }
    }
}
