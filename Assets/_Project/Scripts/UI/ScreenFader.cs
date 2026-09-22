// ScreenFader
// A full-screen black overlay that can fade in and out. The SceneLoader uses it
// to hide scene loading, and the Game Over screen uses it to fade to black.
// It uses unscaled time so it still works when the game is paused or frozen.
//
// Put this on: a full-screen UI Image (black) that has a CanvasGroup.
//   The Image should cover the whole screen and start fully transparent.
// Assign in Inspector: "Canvas Group" (the CanvasGroup on this same object).

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    [Header("Fade")]
    [Tooltip("The CanvasGroup whose alpha we fade. Usually on this same object.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("How long a fade takes, in seconds.")]
    [SerializeField] private float fadeDuration = 0.5f;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    // Fades the screen to solid black. Yield on this from a coroutine.
    public IEnumerator FadeOut()
    {
        canvasGroup.blocksRaycasts = true; // Block clicks while the screen is dark.
        yield return Fade(1f);
    }

    // Fades the screen from black back to clear. Yield on this from a coroutine.
    public IEnumerator FadeIn()
    {
        yield return Fade(0f);
        canvasGroup.blocksRaycasts = false;
    }

    // Moves the overlay's transparency toward the target value over time.
    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}
