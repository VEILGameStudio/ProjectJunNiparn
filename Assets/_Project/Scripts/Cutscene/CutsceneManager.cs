// CutsceneManager
// Plays a cutscene - either a Timeline (through a PlayableDirector) or a video
// clip (through a VideoPlayer) - and shows a Skip button. While a cutscene plays,
// player input is locked in two ways: the game state is set to Cutscene, and the
// Gameplay controls are switched off. Both are restored when the cutscene ends or
// is skipped. The Skip button still works because it is UI.
// Other scripts reach it with: UIManager.Instance.Cutscene
//
// Put this on: the PersistentCanvas (next to the UIManager).
// Assign in Inspector:
//   - Input Reader: the shared MainInputReader asset (so input can be turned off).
//   - Skip Button: a UI Button shown during cutscenes (its click skips).
//   - Video Screen (optional): a full-screen UI object shown only during videos.

using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.Video;

public class CutsceneManager : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Drag the shared Input Reader asset here so input can be turned off during cutscenes.")]
    [SerializeField] private InputReader inputReader;

    [Header("UI")]
    [Tooltip("A UI Button shown while a cutscene plays. Clicking it skips.")]
    [SerializeField] private Button skipButton;

    [Tooltip("Optional. A full-screen UI object (the video image) shown only during video cutscenes.")]
    [SerializeField] private GameObject videoScreen;

    private PlayableDirector activeDirector;
    private VideoPlayer activeVideo;
    private Action onFinished;

    // True while a cutscene is playing.
    public bool IsPlaying { get; private set; }

    private void Start()
    {
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(Skip);
            skipButton.gameObject.SetActive(false);
        }
        if (videoScreen != null)
        {
            videoScreen.SetActive(false);
        }
    }

    // Stops listening to the director or video if this is switched off mid-cutscene.
    private void OnDisable()
    {
        StopListening();
    }

    // Plays a Timeline cutscene. onFinished runs when it ends or is skipped.
    public void PlayTimeline(PlayableDirector director, Action finishedCallback = null)
    {
        if (IsPlaying)
        {
            return;
        }
        if (director == null)
        {
            Debug.LogError("CutsceneManager: PlayTimeline was given no PlayableDirector.");
            finishedCallback?.Invoke();
            return;
        }

        onFinished = finishedCallback;
        activeDirector = director;
        Begin(false);

        director.stopped += HandleDirectorStopped;
        director.Play();
    }

    // Plays a video cutscene. onFinished runs when it ends or is skipped.
    public void PlayVideo(VideoPlayer video, Action finishedCallback = null)
    {
        if (IsPlaying)
        {
            return;
        }
        if (video == null)
        {
            Debug.LogError("CutsceneManager: PlayVideo was given no VideoPlayer.");
            finishedCallback?.Invoke();
            return;
        }

        onFinished = finishedCallback;
        activeVideo = video;
        activeVideo.isLooping = false;
        Begin(true);

        video.loopPointReached += HandleVideoFinished;
        video.Play();
    }

    // Stops the current cutscene early. Wired to the Skip button.
    public void Skip()
    {
        if (!IsPlaying)
        {
            return;
        }

        if (activeVideo != null)
        {
            activeVideo.Stop();
        }
        if (activeDirector != null)
        {
            activeDirector.Stop(); // May raise 'stopped' which also ends the cutscene.
        }

        End();
    }

    // Sets up the game for a cutscene: lock input, show skip (and video screen).
    private void Begin(bool isVideo)
    {
        IsPlaying = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameState.Cutscene);
        }
        if (inputReader != null)
        {
            inputReader.DisableGameplay();
        }
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(true);
        }
        if (isVideo && videoScreen != null)
        {
            videoScreen.SetActive(true);
        }
    }

    // Cleans up after a cutscene: restore input and normal play, run the callback.
    private void End()
    {
        if (!IsPlaying)
        {
            return;
        }
        IsPlaying = false;

        StopListening();
        activeDirector = null;
        activeVideo = null;

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }
        if (videoScreen != null)
        {
            videoScreen.SetActive(false);
        }
        if (inputReader != null)
        {
            inputReader.EnableGameplay();
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameState.Playing);
        }

        Action callback = onFinished;
        onFinished = null;
        callback?.Invoke();
    }

    // Unsubscribes from the director and video events.
    private void StopListening()
    {
        if (activeDirector != null)
        {
            activeDirector.stopped -= HandleDirectorStopped;
        }
        if (activeVideo != null)
        {
            activeVideo.loopPointReached -= HandleVideoFinished;
        }
    }

    private void HandleDirectorStopped(PlayableDirector director) => End();
    private void HandleVideoFinished(VideoPlayer video) => End();
}
