// CameraPreset
// The two camera presets of the game, the numbers behind each one, and the code that
// puts a preset onto a Cinemachine camera.
//   ThreeQuarter - puzzle rooms. Orthographic (no perspective). The camera is turned 45
//                  degrees (Yaw) and looks 30 degrees down (Pitch), so rooms look like
//                  diamonds on screen.
//   SideView     - corridors between rooms. Perspective, like a side-scroller. The camera
//                  looks straight along the level (Yaw 0) and only 10 degrees down, so A/D
//                  walk left/right and W/S walk a short way into and out of the screen.
//                  Its Field Of View and Camera Distance are NOT final yet (Oak is choosing).
// There are exactly two presets and there will never be a third: every sprite is drawn for
// one of them. Sprites for the two presets live in separate folders (Art/Sprites/ThreeQuarter
// and Art/Sprites/SideView) and must not be mixed. Never add a preset or change these numbers
// yourself (ask Oak first).
//
// Put this on: nothing. LevelSettings uses it.

using Unity.Cinemachine;
using UnityEngine;

public enum CameraPreset
{
    ThreeQuarter,
    SideView
}

// The numbers a camera preset stands for.
public struct CameraPresetValues
{
    public float Pitch;             // How many degrees the camera looks down (0 = straight ahead, 90 = straight down).
    public float Yaw;               // How many degrees the camera is turned around the vertical axis (0 = looks along +Z).
    public bool Orthographic;       // True = orthographic (near and far look the same size). False = perspective.
    public float OrthographicSize;  // Orthographic only. Zoom: half the screen height in world units. Bigger = sees more.
    public float FieldOfView;       // Perspective only. How wide the camera sees, in degrees (top to bottom of the screen).
    public float CameraDistance;    // How far back from the player the camera sits, along its view direction.
    public Vector2 Damping;         // Follow smoothing: X = left/right on screen, Y = up/down on screen.

    // Returns the numbers for a preset.
    public static CameraPresetValues For(CameraPreset preset)
    {
        switch (preset)
        {
            case CameraPreset.SideView:
                return new CameraPresetValues
                {
                    Pitch = 10f,
                    Yaw = 0f,
                    Orthographic = false,
                    FieldOfView = 25f,      // Not final yet.
                    CameraDistance = 20f,   // Not final yet.
                    Damping = new Vector2(1f, 1.3f)
                };

            case CameraPreset.ThreeQuarter:
            default:
                return new CameraPresetValues
                {
                    Pitch = 30f,
                    Yaw = 45f,
                    Orthographic = true,
                    OrthographicSize = 5f,
                    CameraDistance = 20f,
                    Damping = new Vector2(1f, 1.3f)
                };
        }
    }

    // Puts a preset onto a Cinemachine camera: projection, zoom, angle, distance and smoothing.
    public static void Apply(CinemachineCamera camera, CameraPreset preset)
    {
        Apply(camera, For(preset));
    }

    // The same, for numbers that were already adjusted (LevelSettings does this for its per-level values).
    public static void Apply(CinemachineCamera camera, CameraPresetValues values)
    {
        if (camera == null)
        {
            Debug.LogError("CameraPresetValues.Apply: no camera was given, so the camera preset was not applied. Pass the level's CinemachineCamera.");
            return;
        }

        ApplyLens(camera, values);
        ApplyAngle(camera, values);
        ApplyFollow(camera, values);
    }

    // Checks that the Main Camera will really get the preset's projection. Returns a message that
    // says what to fix, or null when everything is right. It only reads the cameras, never changes them.
    public static string FindProjectionProblem(CinemachineCamera camera, CinemachineBrain brain, CameraPresetValues values)
    {
        string wanted = values.Orthographic ? "Orthographic" : "Perspective";

        if (brain == null)
        {
            return "this scene has no Cinemachine Brain, so nothing moves the Main Camera. Select the Main Camera and add the Cinemachine Brain component.";
        }

        if (!brain.LensModeOverride.Enabled)
        {
            // The Brain leaves the Main Camera's projection alone, so it has to be right already.
            Camera output = brain.OutputCamera;
            if (output != null && output.orthographic != values.Orthographic)
            {
                return $"this level's preset needs the Main Camera to be {wanted}, but the Cinemachine Brain is not allowed to change the projection. Select the Main Camera, find Cinemachine Brain, and tick Lens Mode Override.";
            }
            return null;
        }

        LensSettings.OverrideModes wantedMode = values.Orthographic ? LensSettings.OverrideModes.Orthographic : LensSettings.OverrideModes.Perspective;
        if (camera.Lens.ModeOverride == LensSettings.OverrideModes.None)
        {
            return $"the camera '{camera.name}' has Lens > Mode Override set to None, so the Cinemachine Brain uses its own Default Mode instead of {wanted}. Select '{camera.name}' and set Lens > Mode Override to {wanted}.";
        }
        if (camera.Lens.ModeOverride != wantedMode)
        {
            return $"the camera '{camera.name}' has Lens > Mode Override set to {camera.Lens.ModeOverride}, but this level needs {wanted}. Select '{camera.name}' and set Lens > Mode Override to {wanted}.";
        }
        return null;
    }

    // Sets the projection (orthographic or perspective) and how much of the level the camera shows.
    // The Cinemachine Brain copies this onto the Main Camera; the Main Camera is never written here.
    private static void ApplyLens(CinemachineCamera camera, CameraPresetValues values)
    {
        LensSettings lens = camera.Lens;
        if (values.Orthographic)
        {
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = values.OrthographicSize;
        }
        else
        {
            lens.ModeOverride = LensSettings.OverrideModes.Perspective;
            lens.FieldOfView = values.FieldOfView;
        }
        camera.Lens = lens;
    }

    // Turns the CinemachineCamera to the preset's angle. Cinemachine then moves the Main Camera.
    private static void ApplyAngle(CinemachineCamera camera, CameraPresetValues values)
    {
        if (camera.GetCinemachineComponent(CinemachineCore.Stage.Aim) != null)
        {
            Debug.LogError($"Camera '{camera.name}': it has a Rotation Control, which turns the camera away from the preset's angle. Select it and set Rotation Control to None.", camera);
        }

        Quaternion angle = Quaternion.Euler(values.Pitch, values.Yaw, 0f);
        if (Quaternion.Angle(camera.transform.rotation, angle) > 0.1f)
        {
            Debug.LogWarning($"Camera '{camera.name}': it was turned to the preset's angle ({values.Pitch}, {values.Yaw}, 0). Type that into its Rotation in the Inspector, so the Scene view looks like the game.", camera);
        }
        camera.transform.rotation = angle;
    }

    // Sets how far back the camera sits and how smoothly it follows the player.
    private static void ApplyFollow(CinemachineCamera camera, CameraPresetValues values)
    {
        CinemachinePositionComposer composer = camera.GetComponent<CinemachinePositionComposer>();
        if (composer == null)
        {
            Debug.LogError($"Camera '{camera.name}': it has no CinemachinePositionComposer, so it cannot follow the player. Select it and set Position Control to Position Composer.", camera);
            return;
        }

        composer.CameraDistance = values.CameraDistance;
        composer.Damping = new Vector3(values.Damping.x, values.Damping.y, composer.Damping.z);
    }
}
