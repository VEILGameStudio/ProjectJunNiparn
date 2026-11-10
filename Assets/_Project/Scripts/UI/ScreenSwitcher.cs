// ScreenSwitcher
// Shows one menu screen at a time: the main menu, Settings, Credits, or the tabs inside
// Settings. Drag every screen (a Canvas or a panel) into Screens, then make each button
// call Open and drag in the screen it should open. Back returns to the screen before.
// A scene can have several of these: one for the main menu screens, and another one on
// the Settings panel for its tabs. It is not a singleton.
//
// Animations (optional, no code): add a DOTween Animation to the screen object itself
// (the object dragged into Screens) and set its Id to "open" or "close".
//   - "open" plays every time the screen opens.
//   - "close" plays when the screen closes; the screen switches off when it has finished.
//   In those DOTween Animations untick Auto Play, and tick Ignore TimeScale so they also
//   play while the game is paused.
// Anything else (a sound, a child animation) can be wired to On Opened / On Closed.
//
// Put this on: an object that this switcher never switches off, for example an empty
//   "Screens" object in the menu scene, or the Settings panel for the tab switcher.
//   Never put it on one of its own screens.
// Assign in Inspector:
//   - Screens: every screen this switcher controls.
//   - First Screen: the screen shown when the scene starts (empty = none).
//   - Buttons: OnClick -> ScreenSwitcher.Open, then drag the screen to open into the box.
//     Other buttons: ScreenSwitcher.Back, ScreenSwitcher.CloseCurrent, ScreenSwitcher.QuitGame.

using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class ScreenSwitcher : MonoBehaviour
{
    // One screen in the list, with its own settings.
    [System.Serializable]
    public class MenuScreen
    {
        [Tooltip("The Canvas or panel object of this screen.")]
        public GameObject screen;

        [Tooltip("Tick for a pop-up that opens over the current screen instead of replacing it.")]
        public bool openOnTop;

        [Tooltip("Runs every time this screen has opened. Add extra things here, for example a sound or a child animation.")]
        public UnityEvent onOpened = new UnityEvent();

        [Tooltip("Runs every time this screen has closed.")]
        public UnityEvent onClosed = new UnityEvent();
    }

    [Header("Screens")]
    [Tooltip("Every screen this switcher controls. Drag the Canvas or panel objects here.")]
    [SerializeField] private List<MenuScreen> screens = new List<MenuScreen>();

    [Tooltip("The screen shown when the scene starts. Leave empty to start with no screen open.")]
    [SerializeField] private GameObject firstScreen;

    [Header("Animations")]
    [Tooltip("A DOTween Animation on a screen with this Id plays every time the screen opens.")]
    [SerializeField] private string openAnimationId = "open";

    [Tooltip("A DOTween Animation on a screen with this Id plays when the screen closes. The screen switches off when it has finished.")]
    [SerializeField] private string closeAnimationId = "close";

    // The screens opened so far, oldest first. The last one is the current screen.
    private readonly List<MenuScreen> history = new List<MenuScreen>();

    // True while a close animation is playing. Buttons do nothing until it has finished.
    private bool isBusy;

    // The screens in the list, for scripts that want to listen to their events.
    public IReadOnlyList<MenuScreen> Screens => screens;

    // The screen on top right now, or null when none is open.
    public GameObject CurrentScreen => history.Count > 0 ? history[history.Count - 1].screen : null;

    private void Start()
    {
        ReportSetupProblems();

        foreach (MenuScreen entry in screens)
        {
            if (entry.screen != null)
            {
                entry.screen.SetActive(false);
            }
        }

        if (FindScreen(firstScreen) != null)
        {
            Open(firstScreen);
        }
    }

    private void OnDisable()
    {
        // A close animation that was cut off by switching this object off must not block the buttons.
        isBusy = false;
    }

    // Opens a screen. A screen that is already in the history is returned to instead,
    // so "Open Main Menu" and "Back" end in the same place.
    public void Open(GameObject screenObject)
    {
        if (isBusy)
        {
            return;
        }

        MenuScreen target = FindScreen(screenObject);
        if (target == null)
        {
            Debug.LogError($"ScreenSwitcher on '{name}': '{(screenObject != null ? screenObject.name : "nothing")}' is not in Screens. Drag it into the Screens list of this Screen Switcher.", this);
            return;
        }

        List<MenuScreen> toClose = new List<MenuScreen>();
        List<MenuScreen> toOpen = new List<MenuScreen>();
        int index = history.IndexOf(target);

        if (index == history.Count - 1 && index >= 0)
        {
            return; // Already the current screen.
        }

        if (index >= 0)
        {
            // Go back to it: close everything opened after it.
            bool targetWasHidden = false;
            for (int i = history.Count - 1; i > index; i--)
            {
                toClose.Add(history[i]);
                if (!history[i].openOnTop)
                {
                    targetWasHidden = true;
                }
            }
            history.RemoveRange(index + 1, history.Count - index - 1);
            if (targetWasHidden)
            {
                toOpen.AddRange(VisibleScreens());
            }
        }
        else
        {
            if (!target.openOnTop)
            {
                toClose.AddRange(VisibleScreens());
                toClose.Reverse();
            }
            history.Add(target);
            toOpen.Add(target);
        }

        Switch(toClose, toOpen);
    }

    // Goes back to the screen before. Does nothing on the first screen.
    public void Back()
    {
        if (isBusy || history.Count <= 1)
        {
            return;
        }

        CloseTop();
    }

    // Closes the current screen, even the first one (useful for a pop-up or a pause menu).
    public void CloseCurrent()
    {
        if (isBusy || history.Count == 0)
        {
            return;
        }

        CloseTop();
    }

    // Leaves the game. In the Unity Editor it stops Play mode instead.
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Closes the screen on top and shows again whatever it was covering.
    private void CloseTop()
    {
        MenuScreen top = history[history.Count - 1];
        history.RemoveAt(history.Count - 1);

        List<MenuScreen> toClose = new List<MenuScreen> { top };
        List<MenuScreen> toOpen = new List<MenuScreen>();
        if (!top.openOnTop)
        {
            toOpen.AddRange(VisibleScreens());
        }

        Switch(toClose, toOpen);
    }

    // The screens that show together at the top of the history: the last normal screen and
    // the pop-ups opened on top of it.
    private List<MenuScreen> VisibleScreens()
    {
        List<MenuScreen> visible = new List<MenuScreen>();
        for (int i = history.Count - 1; i >= 0; i--)
        {
            visible.Insert(0, history[i]);
            if (!history[i].openOnTop)
            {
                break;
            }
        }
        return visible;
    }

    // Closes and opens screens. Waits for close animations first when there are any.
    private void Switch(List<MenuScreen> toClose, List<MenuScreen> toOpen)
    {
        List<Tween> closeTweens = new List<Tween>();
        foreach (MenuScreen entry in toClose)
        {
            closeTweens.AddRange(PlayAnimations(entry, closeAnimationId, true));
        }

        if (closeTweens.Count == 0)
        {
            CloseNow(toClose);
            OpenNow(toOpen);
            return;
        }

        StartCoroutine(SwitchAfter(closeTweens, toClose, toOpen));
    }

    // Waits until every close animation has finished, then switches the screens.
    private IEnumerator SwitchAfter(List<Tween> closeTweens, List<MenuScreen> toClose, List<MenuScreen> toOpen)
    {
        isBusy = true;
        foreach (Tween tween in closeTweens)
        {
            if (tween != null && tween.active)
            {
                yield return tween.WaitForCompletion();
            }
        }
        isBusy = false;

        CloseNow(toClose);
        OpenNow(toOpen);
    }

    private void CloseNow(List<MenuScreen> toClose)
    {
        foreach (MenuScreen entry in toClose)
        {
            if (entry.screen != null)
            {
                entry.screen.SetActive(false);
            }
            entry.onClosed.Invoke();
        }
    }

    private void OpenNow(List<MenuScreen> toOpen)
    {
        foreach (MenuScreen entry in toOpen)
        {
            if (entry.screen != null)
            {
                entry.screen.SetActive(true);
                PlayAnimations(entry, openAnimationId, false);
            }
            entry.onOpened.Invoke();
        }
    }

    // Starts every DOTween Animation on the screen object whose Id matches, from the beginning.
    // Returns the tweens worth waiting for (only asked for close animations).
    private List<Tween> PlayAnimations(MenuScreen entry, string animationId, bool forClosing)
    {
        List<Tween> started = new List<Tween>();
        if (entry.screen == null || !entry.screen.activeInHierarchy || string.IsNullOrEmpty(animationId))
        {
            return started;
        }

        DOTweenAnimation[] animations = entry.screen.GetComponents<DOTweenAnimation>();

        // Stop this screen's open or close animation that may still be running, so the two never fight.
        foreach (DOTweenAnimation tweenAnimation in animations)
        {
            bool isScreenAnimation = tweenAnimation.id == openAnimationId || tweenAnimation.id == closeAnimationId;
            if (isScreenAnimation && tweenAnimation.tween != null && tweenAnimation.tween.active)
            {
                tweenAnimation.tween.Kill();
            }
        }

        foreach (DOTweenAnimation tweenAnimation in animations)
        {
            if (!tweenAnimation.isActive || tweenAnimation.id != animationId)
            {
                continue;
            }

            tweenAnimation.CreateTween(true, true);
            if (tweenAnimation.tween == null)
            {
                continue;
            }

            if (Time.timeScale == 0f && !tweenAnimation.isIndependentUpdate)
            {
                Debug.LogWarning($"ScreenSwitcher on '{name}': the game is paused and the '{animationId}' animation on '{entry.screen.name}' cannot play. Tick Ignore TimeScale in that DOTween Animation.", tweenAnimation);
                continue;
            }
            if (forClosing && tweenAnimation.loops < 0)
            {
                Debug.LogWarning($"ScreenSwitcher on '{name}': the '{animationId}' animation on '{entry.screen.name}' loops forever, so the screen closes without waiting for it. Set its Loops to 1.", tweenAnimation);
                continue;
            }
            if (forClosing)
            {
                started.Add(tweenAnimation.tween);
            }
        }
        return started;
    }

    // Finds the list entry of a screen object, or null.
    private MenuScreen FindScreen(GameObject screenObject)
    {
        if (screenObject == null)
        {
            return null;
        }
        foreach (MenuScreen entry in screens)
        {
            if (entry.screen == screenObject)
            {
                return entry;
            }
        }
        return null;
    }

    // Says once, at the start, what is set up wrong in the Inspector.
    private void ReportSetupProblems()
    {
        for (int i = 0; i < screens.Count; i++)
        {
            GameObject screenObject = screens[i].screen;
            if (screenObject == null)
            {
                Debug.LogError($"ScreenSwitcher on '{name}': Screens element {i} is empty. Drag a Canvas or panel into it, or remove the element.", this);
                continue;
            }
            if (transform == screenObject.transform || transform.IsChildOf(screenObject.transform))
            {
                Debug.LogError($"ScreenSwitcher on '{name}': it sits on (or inside) its own screen '{screenObject.name}', so closing that screen would switch the Screen Switcher off too. Move the Screen Switcher to an object that stays on, for example an empty 'Screens' object.", this);
            }
            foreach (DOTweenAnimation tweenAnimation in screenObject.GetComponents<DOTweenAnimation>())
            {
                if (tweenAnimation.id == closeAnimationId && tweenAnimation.autoPlay)
                {
                    Debug.LogError($"ScreenSwitcher on '{name}': the '{closeAnimationId}' animation on '{screenObject.name}' has Auto Play ticked, so the screen would play its closing animation as soon as it opens. Untick Auto Play in that DOTween Animation.", tweenAnimation);
                }
            }
        }
        if (firstScreen != null && FindScreen(firstScreen) == null)
        {
            Debug.LogError($"ScreenSwitcher on '{name}': First Screen '{firstScreen.name}' is not in Screens. Drag it into the Screens list too.", this);
        }
    }
}
