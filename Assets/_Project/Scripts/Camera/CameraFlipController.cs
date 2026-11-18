// CameraFlipController
// Lets the player flip the camera 180 degrees in a Side View corridor, to see the other side
// of it, and flip back. Press Q (keyboard) or Right Shoulder (gamepad).
// It only works in an area whose LevelSettings has "Allow Camera Flip" ticked. In every other
// area (all Three Quarter rooms, and the Side View corridors that did not tick it) the key does nothing.
// The flip is hidden behind a fade (fade out, camera cuts, fade in), never a blend: a camera
// cannot blend smoothly between two sides. Order after the cut: camera first, then sprites turn
// (SpriteBillboard.RefreshAll), then the rest of the game hears OnCameraZoneChanged.
// The flipped state is saved in the gameplay save, so loading brings the player back to the same side.
// Leaving a flippable corridor for any other area puts the flip back to normal.
//
// Put this on: the "Managers" GameObject, or any always-on object of a scene that has a flippable corridor.
//   One per scene. It is not a singleton.
// Assign in Inspector:
//   - Input Reader: the InputReader asset (the same one the Player uses).

using UnityEngine;

public class CameraFlipController : MonoBehaviour, ISaveParticipant
{
    [Header("Input")]
    [Tooltip("The InputReader asset that reads the Q key and the gamepad button. Drag the same asset the Player uses.")]
    [SerializeField] private InputReader inputReader;

    // What the player wants: true = looking from the other side of the corridor.
    private bool wantsFlipped;

    // True when the player may flip now. Public and static so tests can check it without a scene.
    public static bool CanFlipNow(bool areaCanFlip, bool gameIsPlaying, bool isFading)
    {
        return areaCanFlip && gameIsPlaying && !isFading;
    }

    private void OnEnable()
    {
        GameEvents.OnCameraZoneChanged += HandleCameraZoneChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnCameraZoneChanged -= HandleCameraZoneChanged;
    }

    private void Start()
    {
        if (inputReader == null)
        {
            Debug.LogError($"CameraFlipController on '{name}': Input Reader is not assigned, so the Q key cannot flip the camera. Drag the InputReader asset here.", this);
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Register(this);
        }
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Unregister(this);
        }
    }

    // Flips when the key is pressed.
    private void Update()
    {
        if (inputReader != null && inputReader.FlipCameraPressed)
        {
            TryFlip();
        }
    }

    // Flips the camera if the area allows it right now. The Q key and the on-screen button both call this.
    public void TryFlip()
    {
        LevelSettings settings = FindAnyObjectByType<LevelSettings>();
        if (settings == null || GameManager.Instance == null || GameManager.Instance.SceneLoader == null)
        {
            return;
        }

        if (!CanFlipNow(settings.CanFlip, GameManager.Instance.IsPlaying, GameManager.Instance.SceneLoader.IsBusy))
        {
            return;
        }

        wantsFlipped = !settings.IsFlipped;
        GameManager.Instance.SceneLoader.RunBehindFade(ApplyWantedFlip);
    }

    // True when the current area allows a flip. The on-screen button reads this to show or hide itself.
    public bool AreaAllowsFlip()
    {
        LevelSettings settings = FindAnyObjectByType<LevelSettings>();
        return settings != null && settings.CanFlip;
    }

    // Puts the camera on the side the player wants, then turns the sprites and tells the game.
    private void ApplyWantedFlip()
    {
        LevelSettings settings = FindAnyObjectByType<LevelSettings>();
        if (settings == null || !settings.SetFlipped(wantsFlipped))
        {
            return;
        }

        SpriteBillboard.RefreshAll();
        GameEvents.RaiseCameraZoneChanged(settings.Preset); // Our own handler hears this too; it does nothing twice.
    }

    // The player walked into another zone. A zone that cannot flip puts the flip back to normal;
    // a zone that can flip gets the side the player last wanted (this is also what a loaded save needs).
    private void HandleCameraZoneChanged(CameraPreset preset)
    {
        LevelSettings settings = FindAnyObjectByType<LevelSettings>();
        if (settings == null)
        {
            return;
        }

        if (!settings.CanFlip)
        {
            wantsFlipped = false;
            return;
        }

        if (settings.SetFlipped(wantsFlipped))
        {
            SpriteBillboard.RefreshAll();
        }
    }

    // Writes which side the camera is on into the save.
    public void CaptureState(GameSaveData data)
    {
        LevelSettings settings = FindAnyObjectByType<LevelSettings>();
        data.cameraFlipped = settings != null ? settings.IsFlipped : wantsFlipped;
    }

    // Brings the camera back to the saved side. Safe to run twice: it sets the state, it never toggles.
    public void RestoreState(GameSaveData data)
    {
        wantsFlipped = data.cameraFlipped;

        LevelSettings settings = FindAnyObjectByType<LevelSettings>();
        if (settings != null && settings.CanFlip && settings.SetFlipped(wantsFlipped))
        {
            SpriteBillboard.RefreshAll();
        }
    }
}
