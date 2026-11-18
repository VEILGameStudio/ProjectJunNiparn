// CameraFlipTests
// EditMode tests for flipping the Side View camera 180 degrees: the yaw maths, who may flip,
// walking and facing from the flipped side, the camera box, and the save field.
// Put this file in: Assets/_Project/Tests/Editor/
// Run with: Window > General > Test Runner > EditMode > Run All.

using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class CameraFlipTests
{
    [Test]
    public void ApplyFlip_NotFlipped_KeepsYaw()
    {
        float yaw = CameraPresetValues.ApplyFlip(0f, false);

        Assert.AreEqual(0f, yaw, 0.001f);
    }

    [Test]
    public void ApplyFlip_Flipped_TurnsHalfWayAround()
    {
        float yaw = CameraPresetValues.ApplyFlip(0f, true);

        Assert.AreEqual(180f, yaw, 0.001f);
    }

    [Test]
    public void ApplyFlip_FlippedTwiceFromYaw180_WrapsBackToZero()
    {
        float yaw = CameraPresetValues.ApplyFlip(180f, true);

        Assert.AreEqual(0f, yaw, 0.001f);
    }

    [Test]
    public void CanFlipNow_AreaThatCannotFlip_ReturnsFalse()
    {
        Assert.IsFalse(CameraFlipController.CanFlipNow(false, true, false));
    }

    [Test]
    public void CanFlipNow_GameNotPlaying_ReturnsFalse()
    {
        Assert.IsFalse(CameraFlipController.CanFlipNow(true, false, false));
    }

    [Test]
    public void CanFlipNow_WhileFading_ReturnsFalse()
    {
        Assert.IsFalse(CameraFlipController.CanFlipNow(true, true, true));
    }

    [Test]
    public void CanFlipNow_FlippableAreaWhilePlaying_ReturnsTrue()
    {
        Assert.IsTrue(CameraFlipController.CanFlipNow(true, true, false));
    }

    [Test]
    public void LevelSettings_ThreeQuarterWithAllowTicked_CannotFlip()
    {
        GameObject host = new GameObject("LevelSettingsForTest");
        LevelSettings settings = host.AddComponent<LevelSettings>();
        SetPrivateField(settings, "allowCameraFlip", true);
        SetPrivateField(settings, "cameraPreset", CameraPreset.ThreeQuarter);

        bool changed = settings.SetFlipped(true);

        Object.DestroyImmediate(host);
        Assert.IsFalse(changed);
        Assert.AreEqual(45f, settings.CameraYaw, 0.001f);
    }

    [Test]
    public void LevelSettings_SideViewWithoutAllowTicked_CannotFlip()
    {
        GameObject host = new GameObject("LevelSettingsForTest");
        LevelSettings settings = host.AddComponent<LevelSettings>();
        SetPrivateField(settings, "cameraPreset", CameraPreset.SideView);

        bool changed = settings.SetFlipped(true);

        Object.DestroyImmediate(host);
        Assert.IsFalse(changed);
        Assert.AreEqual(0f, settings.CameraYaw, 0.001f);
    }

    [Test]
    public void MoveDirection_WhenFlipped_StillWalksUpTheScreen()
    {
        Quaternion camera = Quaternion.Euler(10f, 180f, 0f);

        Vector3 direction = PlayerMovement.CalculateMoveDirection(new Vector2(0f, 1f), camera * Vector3.forward, camera * Vector3.right);
        Vector2 onScreen = PlayerMovement.CalculateScreenDirection(direction, camera * Vector3.forward, camera * Vector3.right);

        Assert.AreEqual(-1f, direction.z, 0.001f); // Up the screen is world -Z once the camera looks along -Z.
        Assert.AreEqual(1f, onScreen.y, 0.001f);
        Assert.AreEqual(0f, onScreen.x, 0.001f);
    }

    [Test]
    public void MoveDirection_WhenFlipped_RightKeyWalksTowardWorldMinusX()
    {
        Quaternion camera = Quaternion.Euler(10f, 180f, 0f);

        Vector3 direction = PlayerMovement.CalculateMoveDirection(new Vector2(1f, 0f), camera * Vector3.forward, camera * Vector3.right);

        Assert.AreEqual(-1f, direction.x, 0.001f);
    }

    [Test]
    public void BillboardRotation_WhenFlipped_TurnsTheCardAround()
    {
        Quaternion rotation = SpriteBillboard.CalculateRotation(CameraPresetValues.ApplyFlip(0f, true));

        Assert.AreEqual(180f, rotation.eulerAngles.y, 0.01f);
    }

    [Test]
    public void CameraBox_WhenFlipped_SitsOnTheOtherSideOfThePlayArea()
    {
        CameraPresetValues normal = CameraPresetValues.For(CameraPreset.SideView);
        CameraPresetValues flipped = normal;
        flipped.Yaw = CameraPresetValues.ApplyFlip(normal.Yaw, true);

        CameraPresetValues.CalculateCameraBox(Vector3.zero, new Vector3(20f, 4f, 3f), Quaternion.identity, normal, 16f / 9f, out Vector3 normalCenter, out Vector3 normalSize);
        CameraPresetValues.CalculateCameraBox(Vector3.zero, new Vector3(20f, 4f, 3f), Quaternion.identity, flipped, 16f / 9f, out Vector3 flippedCenter, out Vector3 flippedSize);

        Assert.Less(normalCenter.z, 0f);
        Assert.Greater(flippedCenter.z, 0f);
        Assert.AreEqual(normalSize.x, flippedSize.x, 0.001f);
    }

    [Test]
    public void SaveData_CameraFlipped_SurvivesJsonRoundTrip()
    {
        GameSaveData saved = new GameSaveData();
        saved.cameraFlipped = true;

        GameSaveData loaded = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(saved));

        Assert.IsTrue(loaded.cameraFlipped);
    }

    [Test]
    public void SaveData_OldSaveWithoutCameraFlipped_LoadsAsNotFlipped()
    {
        GameSaveData loaded = JsonUtility.FromJson<GameSaveData>("{\"sceneName\":\"Level1\"}");

        Assert.IsFalse(loaded.cameraFlipped);
    }

    // Sets a [SerializeField] private field the way the Inspector would.
    private static void SetPrivateField(LevelSettings target, string fieldName, bool value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrivateField(LevelSettings target, string fieldName, CameraPreset value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).enumValueIndex = (int)value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
