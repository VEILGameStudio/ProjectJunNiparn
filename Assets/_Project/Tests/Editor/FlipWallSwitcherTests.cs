// FlipWallSwitcherTests
// EditMode tests for the wall swap that follows the camera flip.
// Put this file in: Assets/_Project/Tests/Editor/
// Run with: Window > General > Test Runner > EditMode > Run All.

using NUnit.Framework;
using UnityEngine;

public class FlipWallSwitcherTests
{
    [Test]
    public void ShouldShowNormalSide_CameraNotFlipped_ShowsNormalGroup()
    {
        bool showNormal = FlipWallSwitcher.ShouldShowNormalSide(false);

        Assert.IsTrue(showNormal);
    }

    [Test]
    public void ShouldShowNormalSide_CameraFlipped_ShowsFlippedGroup()
    {
        bool showNormal = FlipWallSwitcher.ShouldShowNormalSide(true);

        Assert.IsFalse(showNormal);
    }

    [Test]
    public void ApplyGroups_CameraFlipped_SwitchesNormalOffAndFlippedOn()
    {
        GameObject normalWall = new GameObject("NormalWall");
        GameObject flippedWall = new GameObject("FlippedWall");

        FlipWallSwitcher.ApplyGroups(new[] { normalWall }, new[] { flippedWall }, true, null);

        Assert.IsFalse(normalWall.activeSelf);
        Assert.IsTrue(flippedWall.activeSelf);

        Object.DestroyImmediate(normalWall);
        Object.DestroyImmediate(flippedWall);
    }

    [Test]
    public void ApplyGroups_CameraNormal_SwitchesNormalOnAndFlippedOff()
    {
        GameObject normalWall = new GameObject("NormalWall");
        GameObject flippedWall = new GameObject("FlippedWall");
        normalWall.SetActive(false);

        FlipWallSwitcher.ApplyGroups(new[] { normalWall }, new[] { flippedWall }, false, null);

        Assert.IsTrue(normalWall.activeSelf);
        Assert.IsFalse(flippedWall.activeSelf);

        Object.DestroyImmediate(normalWall);
        Object.DestroyImmediate(flippedWall);
    }
}
