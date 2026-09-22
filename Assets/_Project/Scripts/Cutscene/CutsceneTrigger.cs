// CutsceneTrigger
// Starts a cutscene and covers the three places the game needs one:
//   - Intro story:            Play On = Scene Start (plays when the scene loads).
//   - Start game:             Play On = Manual, and wire a Start button's OnClick to Play().
//   - Before the puzzle gem:  Play On = Interact (click the gem), then use On Cutscene End
//                             to actually give the gem.
// It can play a Timeline or a video, and runs the On Cutscene End event afterwards.
//
// Put this on: the object that should start the cutscene. For "Interact" it also
//   needs a Collider (Collider2D in 2D scenes, 3D Collider in 2.5D). For
//   "Trigger Enter" it needs a trigger Collider.
// Assign in Inspector:
//   - Play On: when the cutscene starts.
//   - Cutscene Type + Timeline/Video: what to play.
//   - Prompt: the interact text (only used for Play On = Interact).
//   - On Cutscene End: what happens after the cutscene.

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Video;

public class CutsceneTrigger : MonoBehaviour, IInteractable
{
    private enum PlayMode { SceneStart, TriggerEnter, Interact, Manual }
    private enum CutsceneType { Timeline, Video }

    [Header("When To Play")]
    [Tooltip("Scene Start = on load. Trigger Enter = player walks in. Interact = player clicks. Manual = call Play() yourself (e.g. a button).")]
    [SerializeField] private PlayMode playOn = PlayMode.SceneStart;

    [Tooltip("Play only the first time.")]
    [SerializeField] private bool playOnce = true;

    [Header("What To Play")]
    [Tooltip("Choose Timeline or Video.")]
    [SerializeField] private CutsceneType cutsceneType = CutsceneType.Timeline;

    [Tooltip("The Timeline director to play (for Cutscene Type = Timeline).")]
    [SerializeField] private PlayableDirector timeline;

    [Tooltip("The VideoPlayer to play (for Cutscene Type = Video).")]
    [SerializeField] private VideoPlayer video;

    [Header("Interact")]
    [Tooltip("The text shown to the player when Play On = Interact.")]
    [SerializeField] private LocalizedString prompt;

    [Header("Events")]
    [Tooltip("Runs after the cutscene ends (or is skipped). For the gem, give the gem here.")]
    [SerializeField] private UnityEvent onCutsceneEnd;

    private bool hasPlayed;

    // The text shown to the player (from IInteractable).
    public LocalizedString Prompt => prompt;

    private void Start()
    {
        if (playOn == PlayMode.SceneStart)
        {
            Play();
        }
    }

    // Starts the cutscene. Safe to call from a button (Manual mode) too.
    public void Play()
    {
        if (playOnce && hasPlayed)
        {
            return;
        }
        if (CutsceneManager.Instance == null)
        {
            Debug.LogError($"CutsceneTrigger on '{name}': no CutsceneManager found. Add one to the Managers object.", this);
            return;
        }

        hasPlayed = true;

        if (cutsceneType == CutsceneType.Video)
        {
            CutsceneManager.Instance.PlayVideo(video, RaiseEnd);
        }
        else
        {
            CutsceneManager.Instance.PlayTimeline(timeline, RaiseEnd);
        }
    }

    // Runs the On Cutscene End actions.
    private void RaiseEnd()
    {
        onCutsceneEnd?.Invoke();
    }

    // Starts the cutscene when the player clicks (Play On = Interact).
    public void Interact(GameObject interactor)
    {
        if (playOn == PlayMode.Interact)
        {
            Play();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTriggerEnter(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTriggerEnter(other.gameObject);
    }

    // Plays when the player enters the trigger (Play On = Trigger Enter).
    private void TryTriggerEnter(GameObject other)
    {
        if (playOn == PlayMode.TriggerEnter && other.CompareTag("Player"))
        {
            Play();
        }
    }
}
