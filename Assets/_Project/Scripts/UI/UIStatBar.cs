// UIStatBar
// A bar that shows the player's Health or Stamina. It listens for the matching
// GameEvents event (OnHealthChanged or OnStaminaChanged) and updates itself -
// nothing needs to call it. When the value reaches 0 it can play a "warning"
// effect (flash color + shake). It uses real (unscaled) time so the effect still
// plays if the game is frozen.
//
// Put this on: the bar GameObject (the one with the fill Image).
// Assign in Inspector:
//   - Stat: Health or Stamina.
//   - Fill Image: a UI Image set to Image Type = Filled (this is what shrinks).
//   - Flash When Empty: tick to flash and shake when the value reaches 0.
//   - Shake Target (optional): the object to shake; leave empty to shake this bar.
//   - Flash Color / durations / shake strength: tune the warning effect.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIStatBar : MonoBehaviour
{
    // The player values a bar can show.
    private enum StatType { Health, Stamina }

    [Header("Stat")]
    [Tooltip("Which player value this bar shows.")]
    [SerializeField] private StatType stat = StatType.Health;

    [Header("Fill")]
    [Tooltip("A UI Image with Image Type = Filled. Its Fill Amount shows the value.")]
    [SerializeField] private Image fillImage;

    [Header("Warning Effect")]
    [Tooltip("Tick to flash and shake the bar when the value reaches 0 (use it on the stamina bar).")]
    [SerializeField] private bool flashWhenEmpty = true;

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

    // True while the value is at 0, so the warning plays only once per emptying.
    private bool wasEmpty;

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

    private void OnEnable()
    {
        GameEvents.OnHealthChanged += HandleHealthChanged;
        GameEvents.OnStaminaChanged += HandleStaminaChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnHealthChanged -= HandleHealthChanged;
        GameEvents.OnStaminaChanged -= HandleStaminaChanged;
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

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (stat == StatType.Health)
        {
            ShowValue(currentHealth, maxHealth);
        }
    }

    private void HandleStaminaChanged(float currentStamina, float maxStamina)
    {
        if (stat == StatType.Stamina)
        {
            ShowValue(currentStamina, maxStamina);
        }
    }

    // Shows the new value, and plays the warning the moment it reaches 0.
    private void ShowValue(float current, float max)
    {
        SetFill(max > 0f ? current / max : 0f);

        bool isEmpty = current <= 0f;
        if (isEmpty && !wasEmpty && flashWhenEmpty)
        {
            PlayWarningEffect();
        }
        wasEmpty = isEmpty;
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
