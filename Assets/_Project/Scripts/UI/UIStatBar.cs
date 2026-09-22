// UIStatBar
// A simple bar that shows a value from 0 to 1, used for health and stamina. It can
// also play a "warning" effect (flash color + shake) when something runs out. It
// uses real (unscaled) time so the effect still plays if the game is frozen.
//
// Put this on: the bar GameObject (the one with the fill Image).
// Assign in Inspector:
//   - Fill Image: a UI Image set to Image Type = Filled (this is what shrinks).
//   - Shake Target (optional): the object to shake; leave empty to shake this bar.
//   - Flash Color / durations / shake strength: tune the warning effect.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIStatBar : MonoBehaviour
{
    [Header("Fill")]
    [Tooltip("A UI Image with Image Type = Filled. Its Fill Amount shows the value.")]
    [SerializeField] private Image fillImage;

    [Header("Warning Effect")]
    [Tooltip("The object that shakes. Leave empty to shake this bar itself.")]
    [SerializeField] private RectTransform shakeTarget;

    [Tooltip("The color the bar flashes to when it runs out.")]
    [SerializeField] private Color flashColor = Color.red;

    [Tooltip("How long the color flash lasts, in seconds.")]
    [SerializeField] private float flashDuration = 0.4f;

    [Tooltip("How far the bar shakes, in pixels.")]
    [SerializeField] private float shakeStrength = 8f;

    [Tooltip("How long the shake lasts, in seconds.")]
    [SerializeField] private float shakeDuration = 0.4f;

    private Color originalColor;
    private Vector2 originalShakePosition;
    private Coroutine flashRoutine;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        if (fillImage != null)
        {
            originalColor = fillImage.color;
        }
        if (shakeTarget == null)
        {
            shakeTarget = transform as RectTransform;
        }
        if (shakeTarget != null)
        {
            originalShakePosition = shakeTarget.anchoredPosition;
        }
    }

    // Sets how full the bar is, from 0 (empty) to 1 (full).
    public void SetFill(float normalizedValue)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(normalizedValue);
        }
    }

    // Plays the warning effect: flash the color and shake the bar.
    public void PlayWarningEffect()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }

        flashRoutine = StartCoroutine(FlashColor());
        shakeRoutine = StartCoroutine(Shake());
    }

    // Blinks the bar toward the flash color and back.
    private IEnumerator FlashColor()
    {
        if (fillImage == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float blend = Mathf.PingPong(elapsed * 8f, 1f); // 8 = blink speed
            fillImage.color = Color.Lerp(originalColor, flashColor, blend);
            yield return null;
        }

        fillImage.color = originalColor;
        flashRoutine = null;
    }

    // Jitters the bar's position for a short time, then puts it back.
    private IEnumerator Shake()
    {
        if (shakeTarget == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector2 offset = Random.insideUnitCircle * shakeStrength;
            shakeTarget.anchoredPosition = originalShakePosition + offset;
            yield return null;
        }

        shakeTarget.anchoredPosition = originalShakePosition;
        shakeRoutine = null;
    }
}
