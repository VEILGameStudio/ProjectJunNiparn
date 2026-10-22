// ViewStyle
// The two art styles a level can use, and the default numbers for each one.
//   Side View     - the camera looks nearly straight on. Parallax layers move.
//   Three Quarter - a steep diorama view looking down on the floor. No parallax,
//                   and the player shrinks a little as it walks deeper.
// LevelSettings uses these defaults when a scene starts.
//
// To add a new style: add one value to the ViewStyle list, then add its numbers
// in ViewStyleValues.For. Nothing else needs to change.
//
// Put this on: nothing. It is a list of choices plus their default values.

using UnityEngine;

public enum ViewStyle
{
    SideView,
    ThreeQuarter
}

public struct ViewStyleValues
{
    // Up/down walking speed compared to left/right.
    public float DepthSpeedMultiplier;

    // True if parallax layers should move with the camera.
    public bool UseParallax;

    // True if characters shrink a little as they walk deeper (see DepthScaler).
    public bool UseDepthScaling;

    // How smoothly the camera follows the player on X and Y (higher = slower, softer).
    public Vector2 CameraDamping;

    // Returns the default numbers for a view style.
    public static ViewStyleValues For(ViewStyle style)
    {
        switch (style)
        {
            case ViewStyle.ThreeQuarter:
                return new ViewStyleValues
                {
                    DepthSpeedMultiplier = 0.85f,
                    UseParallax = false,
                    UseDepthScaling = true,
                    CameraDamping = new Vector2(1f, 1.3f)
                };
            default:
                return new ViewStyleValues
                {
                    DepthSpeedMultiplier = 0.55f,
                    UseParallax = true,
                    UseDepthScaling = false,
                    CameraDamping = new Vector2(1f, 1.3f)
                };
        }
    }
}
