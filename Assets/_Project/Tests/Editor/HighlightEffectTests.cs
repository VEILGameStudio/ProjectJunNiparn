// HighlightEffectTests
// EditMode tests for how HighlightEffect turns Outline Width (pixels on a 1080-high screen)
// into the picture pixels the outline shader counts, for both camera presets.
// Put this file in: Assets/_Project/Tests/Editor/
// Run with: Window > General > Test Runner > EditMode > Run All.

using NUnit.Framework;

public class HighlightEffectTests
{
    private const float Tolerance = 0.01f;

    [Test]
    public void OutlineWidth_ThreeQuarterRoom_PPU128_Is4Point74PicturePixels()
    {
        // ThreeQuarter: orthographic size 5 shows 10 units on 1080 pixels = 108 pixels per unit.

        float picturePixels = HighlightEffect.WidthInPicturePixels(4f, 128f, true, 5f, 19f, 20f);

        Assert.AreEqual(4f * 128f / 108f, picturePixels, Tolerance);
    }

    [Test]
    public void OutlineWidth_SideViewCutter_IsAbout4PicturePixels()
    {
        // The cutter: PPU 2.6758869 shown at scale 0.016358823, so 163.575 picture pixels per unit.
        // SideView: FOV 19 at distance 20 shows 6.6937 units on 1080 pixels = 161.35 pixels per unit.
        float picturePixelsPerUnit = 2.6758869f / 0.016358823f;

        float picturePixels = HighlightEffect.WidthInPicturePixels(4f, picturePixelsPerUnit, false, 5f, 19f, 20f);

        Assert.AreEqual(4.055f, picturePixels, Tolerance);
    }

    [Test]
    public void OutlineWidth_PictureWithTwiceThePPU_NeedsTwiceThePicturePixels()
    {
        float normal = HighlightEffect.WidthInPicturePixels(4f, 128f, false, 5f, 19f, 20f);

        float sharper = HighlightEffect.WidthInPicturePixels(4f, 256f, false, 5f, 19f, 20f);

        Assert.AreEqual(normal * 2f, sharper, Tolerance);
    }

    [Test]
    public void OutlineWidth_PerspectiveObjectTwiceAsFar_NeedsTwiceThePicturePixels()
    {
        float near = HighlightEffect.WidthInPicturePixels(4f, 128f, false, 5f, 19f, 20f);

        float far = HighlightEffect.WidthInPicturePixels(4f, 128f, false, 5f, 19f, 40f);

        Assert.AreEqual(near * 2f, far, Tolerance);
    }

    [Test]
    public void OutlineWidth_OrthographicCamera_DoesNotDependOnDistance()
    {
        float near = HighlightEffect.WidthInPicturePixels(4f, 128f, true, 5f, 19f, 20f);

        float far = HighlightEffect.WidthInPicturePixels(4f, 128f, true, 5f, 19f, 40f);

        Assert.AreEqual(near, far, Tolerance);
    }

    [Test]
    public void OutlineWidth_ObjectBehindThePerspectiveCamera_FallsBackToTheWidthItself()
    {
        float picturePixels = HighlightEffect.WidthInPicturePixels(4f, 128f, false, 5f, 19f, -3f);

        Assert.AreEqual(4f, picturePixels, Tolerance);
    }
}
