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
// Every number of a preset is written onto the camera by this code when the level starts.
// Do not type them onto a camera in a scene by hand: the code writes over them anyway.
//
// It also works out the box the camera may move in, from the box the player may walk in
// (the Play Area of LevelSettings), for any screen shape.
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
    public Vector3 TargetOffset;    // The point the camera looks at, measured from the player's feet.
    public Vector3 Damping;         // Follow smoothing: X = left/right on screen, Y = up/down on screen, Z = toward/away from the camera.

    // The camera box is never thinner than this. A box that is 0 thick can confuse the physics engine.
    public const float MinimumCameraBoxSize = 0.05f;

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
                    TargetOffset = new Vector3(0f, 2f, 0f),
                    Damping = new Vector3(1f, 1f, 1.3f)
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
                    TargetOffset = new Vector3(0f, 1.25f, 0f),
                    Damping = new Vector3(1f, 1f, 1.3f)
                };
        }
    }

    // Puts a preset onto a Cinemachine camera: projection, zoom, angle, distance, target offset and smoothing.
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

    // Works out the box the CAMERA may move in, from the box the PLAYER may walk in (the play area).
    // The camera stops when the edge of the screen reaches the edge of the play area, so the
    // play area is shrunk by half of what the screen shows, then moved to where the camera sits.
    //   playAreaCenter / playAreaSize / playAreaRotation: the play area box in the world. It may be turned.
    //   aspect: screen width divided by screen height. A wider screen shows more, so the camera moves less.
    //   boxSize is measured along the camera's own directions: X = left/right on screen, Y = up,
    //   Z = into the screen. So the box must be turned by the camera's Yaw.
    // Public and static so tests can check it without a scene.
    public static void CalculateCameraBox(Vector3 playAreaCenter, Vector3 playAreaSize, Quaternion playAreaRotation, CameraPresetValues values, float aspect, out Vector3 boxCenter, out Vector3 boxSize)
    {
        // The camera's left/right and into-the-screen directions, flat on the floor.
        Quaternion yawRotation = Quaternion.Euler(0f, values.Yaw, 0f);
        Vector3 acrossScreen = yawRotation * Vector3.right;
        Vector3 intoScreen = yawRotation * Vector3.forward;

        float halfAcross = MeasureHalfSize(playAreaSize, playAreaRotation, acrossScreen);
        float halfDeep = MeasureHalfSize(playAreaSize, playAreaRotation, intoScreen);
        float halfHigh = MeasureHalfSize(playAreaSize, playAreaRotation, Vector3.up);

        CalculateVisibleFloor(values, aspect, out float visibleHalfWidth, out float visibleHalfDepth);

        boxSize = new Vector3(
            Mathf.Max(MinimumCameraBoxSize, 2f * (halfAcross - visibleHalfWidth)),
            Mathf.Max(MinimumCameraBoxSize, 2f * halfHigh),
            Mathf.Max(MinimumCameraBoxSize, 2f * (halfDeep - visibleHalfDepth)));

        // The camera sits behind and above the point it looks at.
        Vector3 viewDirection = Quaternion.Euler(values.Pitch, values.Yaw, 0f) * Vector3.forward;
        boxCenter = playAreaCenter + values.TargetOffset - viewDirection * values.CameraDistance;
    }

    // How much of the floor the screen shows around the point the camera looks at:
    // half the width (left to right) and half the depth (bottom of the screen to the top).
    public static void CalculateVisibleFloor(CameraPresetValues values, float aspect, out float visibleHalfWidth, out float visibleHalfDepth)
    {
        float pitchRadians = values.Pitch * Mathf.Deg2Rad;

        if (values.Orthographic)
        {
            visibleHalfWidth = values.OrthographicSize * aspect;

            // Looking down at an angle stretches the screen's height across more floor.
            float sinPitch = Mathf.Sin(pitchRadians);
            visibleHalfDepth = sinPitch > 0.0001f ? values.OrthographicSize / sinPitch : float.PositiveInfinity;
            return;
        }

        float halfFieldOfViewRadians = values.FieldOfView * 0.5f * Mathf.Deg2Rad;
        visibleHalfWidth = values.CameraDistance * Mathf.Tan(halfFieldOfViewRadians) * aspect;

        // The bottom of the screen looks down more steeply than the top of the screen.
        float bottomEdgeAngle = pitchRadians + halfFieldOfViewRadians;
        float topEdgeAngle = pitchRadians - halfFieldOfViewRadians;
        if (topEdgeAngle <= 0.0001f)
        {
            // The top of the screen looks at or above the horizon, so the floor it shows never ends.
            // This is why a Side View camera does not follow the player into depth.
            visibleHalfDepth = float.PositiveInfinity;
            return;
        }

        float cameraHeight = values.CameraDistance * Mathf.Sin(pitchRadians);
        float nearestFloor = cameraHeight / Mathf.Tan(bottomEdgeAngle);
        float farthestFloor = cameraHeight / Mathf.Tan(topEdgeAngle);
        visibleHalfDepth = (farthestFloor - nearestFloor) * 0.5f;
    }

    // Half the size of a (possibly turned) box, measured along one direction in the world.
    private static float MeasureHalfSize(Vector3 boxSize, Quaternion boxRotation, Vector3 direction)
    {
        float alongRight = Mathf.Abs(Vector3.Dot(boxRotation * Vector3.right, direction)) * boxSize.x;
        float alongUp = Mathf.Abs(Vector3.Dot(boxRotation * Vector3.up, direction)) * boxSize.y;
        float alongForward = Mathf.Abs(Vector3.Dot(boxRotation * Vector3.forward, direction)) * boxSize.z;
        return (alongRight + alongUp + alongForward) * 0.5f;
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
            Debug.LogWarning($"Camera '{camera.name}': its Rotation in the scene was not the preset's angle, so it was turned to ({values.Pitch}, {values.Yaw}, 0). Somebody turned this camera by hand. Tell Oak, so the scene can be put back.", camera);
        }
        camera.transform.rotation = angle;
    }

    // Sets how far back the camera sits, which point it looks at, and how smoothly it follows the player.
    private static void ApplyFollow(CinemachineCamera camera, CameraPresetValues values)
    {
        CinemachinePositionComposer composer = camera.GetComponent<CinemachinePositionComposer>();
        if (composer == null)
        {
            Debug.LogError($"Camera '{camera.name}': it has no CinemachinePositionComposer, so it cannot follow the player. Select it and set Position Control to Position Composer.", camera);
            return;
        }

        composer.CameraDistance = values.CameraDistance;
        composer.TargetOffset = values.TargetOffset;
        composer.Damping = values.Damping;
    }
}
