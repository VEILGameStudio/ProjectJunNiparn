// PlayerFacingFlipTests
// EditMode tests for turning the player's facing around when the Side View camera flips.
// Put this file in: Assets/_Project/Tests/Editor/
// Run with: Window > General > Test Runner > EditMode > Run All.

using NUnit.Framework;
using UnityEngine;

public class PlayerFacingFlipTests
{
    [Test]
    public void FlipFacing_FrontBecomesBack()
    {
        Vector2 result = PlayerAnimator.FlipFacing(new Vector2(0f, -1f));

        Assert.AreEqual(new Vector2(0f, 1f), result);
    }

    [Test]
    public void FlipFacing_FrontLeftBecomesBackRight()
    {
        Vector2 result = PlayerAnimator.FlipFacing(new Vector2(-1f, -1f));

        Assert.AreEqual(new Vector2(1f, 1f), result);
    }

    [Test]
    public void FlipFacing_TwiceGivesTheSameFacing()
    {
        Vector2 original = new Vector2(1f, -1f);

        Vector2 result = PlayerAnimator.FlipFacing(PlayerAnimator.FlipFacing(original));

        Assert.AreEqual(original, result);
    }

    [Test]
    public void FlipFacing_ResultIsStillOneOfTheSixSideViewFacings()
    {
        Vector2 flipped = PlayerAnimator.FlipFacing(new Vector2(-1f, 1f));

        Vector2 snapped = PlayerAnimator.SnapToSixFacings(flipped, flipped);

        Assert.AreEqual(flipped, snapped);
    }

    [Test]
    public void ShouldTurnFacing_TurnsWhenTheCameraSideChanged()
    {
        Assert.IsTrue(PlayerAnimator.ShouldTurnFacing(false, true));
        Assert.IsTrue(PlayerAnimator.ShouldTurnFacing(true, false));
    }

    [Test]
    public void ShouldTurnFacing_DoesNotTurnWhenTheSideIsTheSame()
    {
        Assert.IsFalse(PlayerAnimator.ShouldTurnFacing(false, false));
        Assert.IsFalse(PlayerAnimator.ShouldTurnFacing(true, true));
    }

    [Test]
    public void ShouldRememberArea_KeepsOldFlipStateInTheSameArea()
    {
        Assert.IsFalse(PlayerAnimator.ShouldRememberArea(true));
        Assert.IsTrue(PlayerAnimator.ShouldRememberArea(false));
    }
}
