// CutsceneTrigger
// Starts a cutscene and covers the three places the game needs one:
//   - Intro story:           Play On = Scene Start (plays when the scene loads).
//   - Start game:            Play On = Manual, and wire a Start button's OnClick to Play().
//   - Before the puzzle gem: Play On = Interact (click the gem, or set Activation =
//                            Touch to play when the player walks in), then use
//                            On Cutscene End to actually give the gem.
// It can play a Timeline or a video, and runs the On Cutscene End event afterwards.
// Player input is locked while the cutscene plays (the CutsceneManager does that).
//
// Put this on: the object that should start the cutscene. Add a Collider2D first
//   (for example a BoxCollider2D); tick "Is Trigger" on it for Activation = Touch.
// Assign in Inspector:
//   - Play On: when the cutscene starts.
//   - Cutscene Type + Timeline/Video: what to play.
//   - One Shot (from InteractableBase): tick to play it only the first time.
//   - Activation / Max Click Distance / Required Item: used when Play On = Interact.
//   - On Cutscene End: what happens after the cutscene.

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Video;

public class CutsceneTrigger : InteractableBase
{
    private enum PlayMode { SceneStart, Interact, Manual }
    private enum CutsceneType { Timeline, Video }

    [Header("When To Play")]
    [Tooltip("Scene Start = on load. Interact = the player clicks it or walks into it (see Activation). Manual = call Play() yourself (e.g. a button).")]
    [SerializeField] private PlayMode playOn = PlayMode.SceneStart;

    [Header("What To Play")]
    [Tooltip("Choose Timeline or Video.")]
    [SerializeField] private CutsceneType cutsceneType = CutsceneType.Timeline;

    [Tooltip("The Timeline director to play (for Cutscene Type = Timeline).")]
    [SerializeField] private PlayableDirector timeline;

    [Tooltip("The VideoPlayer to play (for Cutscene Type = Video).")]
    [SerializeField] private VideoPlayer video;

    [Header("Events")]
    [Tooltip("Runs after the cutscene ends (or is skipped). For the gem, give the gem here.")]
    [SerializeField] private UnityEvent onCutsceneEnd;

    private bool hasPlayed;

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
        if (OneShot && hasPlayed)
        {
            return;
        }
        if (UIManager.Instance == null || UIManager.Instance.Cutscene == null)
        {
            Debug.LogError($"CutsceneTrigger on '{name}': no CutsceneManager found. Make sure the PersistentCanvas (with a UIManager) is in the scene.", this);
            return;
        }

        hasPlayed = true;

        CutsceneManager cutscenes = UIManager.Instance.Cutscene;
        if (cutsceneType == CutsceneType.Video)
        {
            cutscenes.PlayVideo(video, RaiseEnd);
        }
        else
        {
            cutscenes.PlayTimeline(timeline, RaiseEnd);
        }
    }

    // Only Play On = Interact reacts to clicks and touches.
    public override bool CanInteract(PlayerContext player)
    {
        return playOn == PlayMode.Interact && base.CanInteract(player);
    }

    // Starts the cutscene when the player clicks it or walks into it.
    protected override void OnInteract(PlayerContext player)
    {
        Play();
    }

    // Runs the On Cutscene End actions.
    private void RaiseEnd()
    {
        onCutsceneEnd?.Invoke();
    }
}
