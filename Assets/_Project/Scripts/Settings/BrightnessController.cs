// BrightnessController
// Controls screen brightness using a URP Global Volume with a Color Adjustments
// override. The Settings menu sends a slider value here, and this turns it into a
// "Post Exposure" value:
//   slider 10 (the minimum) = normal brightness (Post Exposure 0).
//   slider at its maximum    = the brightest allowed (Max Post Exposure).
// The maximum brightness is capped in the Inspector so the screen never gets too
// bright. Other scripts reach it with: GameManager.Instance.Brightness
//
// Put this on: the "Managers" GameObject (next to the GameManager).
// Assign in Inspector:
//   - Volume: the URP Volume whose profile has a Color Adjustments override.
//   - Slider Maximum: the highest slider value (kept between 50 and 70).
//   - Max Post Exposure: how bright the top of the slider is (keep it modest).

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BrightnessController : MonoBehaviour
{
    // The slider always starts at 10, which means normal brightness.
    public const float SliderMinimum = 10f;

    [Header("Volume")]
    [Tooltip("The URP Global Volume. Its profile must have a Color Adjustments override.")]
    [SerializeField] private Volume volume;

    [Header("Slider Range")]
    [Tooltip("The highest value on the brightness slider. Kept between 50 and 70.")]
    [SerializeField] private float sliderMaximum = 60f;

    [Tooltip("How bright the top of the slider is, as Post Exposure. Keep this modest so it never blinds the player.")]
    [SerializeField] private float maxPostExposure = 1.2f;

    private ColorAdjustments colorAdjustments;

    // The highest value the brightness slider should use.
    public float SliderMaximum => sliderMaximum;

    private void Awake()
    {
        if (volume == null)
        {
            Debug.LogError("BrightnessController: Volume is not assigned. Drag a URP Global Volume here.", this);
            return;
        }
        if (volume.profile == null || !volume.profile.TryGet(out colorAdjustments))
        {
            Debug.LogError("BrightnessController: the Volume's profile has no Color Adjustments override. Add one to the profile.", this);
        }
    }

    private void Start()
    {
        // Apply the saved brightness (or the default) when the game starts.
        float startValue = SaveManager.Instance != null ? SaveManager.Instance.Settings.brightness : SliderMinimum;
        ApplyBrightness(startValue);
    }

    // Turns a slider value into Post Exposure and applies it to the screen.
    public void ApplyBrightness(float sliderValue)
    {
        if (colorAdjustments == null)
        {
            return;
        }

        float amount = Mathf.InverseLerp(SliderMinimum, sliderMaximum, sliderValue); // 0 at min, 1 at max.
        float exposure = Mathf.Lerp(0f, maxPostExposure, amount);

        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = exposure;
    }

    // Keeps the slider maximum sensible while editing in the Inspector.
    private void OnValidate()
    {
        sliderMaximum = Mathf.Clamp(sliderMaximum, 50f, 70f);
    }
}
