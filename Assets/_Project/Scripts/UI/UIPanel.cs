// UIPanel
// Marks one screen part (a panel, a popup, a pause menu, a puzzle screen) that the
// UIManager opens and closes. Buttons call Open, Close or Toggle on the panel itself,
// and the panel asks the UIManager, so the UIManager is the one place that knows what
// is open and whether the game should be paused.
//
// Animations (optional, no code): add a DOTween Animation to this same object and set
// its Id to "open" or "close". Untick Auto Play and tick Ignore TimeScale, so it also
// plays while the game is paused. The panel switches off when "close" has finished.
//
// Put this on: the root object of a panel prefab in Prefabs/UI/Panels, and place that
//   prefab inside the UIRoot prefab. Panels outside UIRoot are not found.
// Assign in Inspector: Panel Id (a short unique name such as "Pause"), and the options.
//   Buttons: OnClick -> UIPanel.Open / UIPanel.Close / UIPanel.Toggle, drag the panel in.

using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIPanel : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("A short unique name for this panel, for example \"Pause\" or \"Gallery\". Code opens it with UIManager.Instance.Open(\"Pause\").")]
    [SerializeField] private string panelId;

    [Header("Behaviour")]
    [Tooltip("Tick to freeze the game while this panel is open (pause menu, settings, puzzle screen).")]
    [SerializeField] private bool pausesGame;

    [Tooltip("Tick to open this panel as soon as the game starts.")]
    [SerializeField] private bool startOpen;

    [Header("Gamepad")]
    [Tooltip("The button (or slider) selected when the panel opens, so a gamepad can use it. Optional.")]
    [SerializeField] private Selectable firstSelected;

    [Header("Animations")]
    [Tooltip("A DOTween Animation on this object with this Id plays when the panel opens.")]
    [SerializeField] private string openAnimationId = "open";

    [Tooltip("A DOTween Animation on this object with this Id plays when the panel closes. The panel switches off when it has finished.")]
    [SerializeField] private string closeAnimationId = "close";

    [Header("Events")]
    [Tooltip("Runs every time this panel has opened. Add extra things here, for example a sound.")]
    [SerializeField] private UnityEvent onOpened = new UnityEvent();

    [Tooltip("Runs every time this panel has closed.")]
    [SerializeField] private UnityEvent onClosed = new UnityEvent();

    public string PanelId => panelId;
    public bool PausesGame => pausesGame;
    public bool StartOpen => startOpen;

    // Asks the UIManager to open this panel. Use it from a button.
    public void Open()
    {
        if (HasManager())
        {
            UIManager.Instance.Open(this);
        }
    }

    // Asks the UIManager to close this panel. Use it from a button.
    public void Close()
    {
        if (HasManager())
        {
            UIManager.Instance.Close(this);
        }
    }

    // Opens the panel when it is closed, closes it when it is open.
    public void Toggle()
    {
        if (HasManager())
        {
            UIManager.Instance.Toggle(this);
        }
    }

    // Switches the panel on and plays its open animation. Only the UIManager calls this.
    public void ShowNow()
    {
        StopAllCoroutines();
        gameObject.SetActive(true);
        PlayAnimation(openAnimationId);

        if (firstSelected != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
        }
        onOpened.Invoke();
    }

    // Plays the close animation, then switches the panel off. Only the UIManager calls this.
    public void HideNow()
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }

        Tween closeTween = PlayAnimation(closeAnimationId);
        if (closeTween == null)
        {
            FinishHiding();
            return;
        }
        StartCoroutine(HideAfter(closeTween));
    }

    // Switches the panel off without animation or events. Used once at the start.
    public void HideSilently()
    {
        gameObject.SetActive(false);
    }

    private IEnumerator HideAfter(Tween closeTween)
    {
        yield return closeTween.WaitForCompletion();
        FinishHiding();
    }

    private void FinishHiding()
    {
        gameObject.SetActive(false);
        onClosed.Invoke();
    }

    // Restarts the DOTween Animation with this Id on this object. Returns its tween, or null.
    private Tween PlayAnimation(string animationId)
    {
        if (string.IsNullOrEmpty(animationId))
        {
            return null;
        }

        Tween started = null;
        foreach (DOTweenAnimation tweenAnimation in GetComponents<DOTweenAnimation>())
        {
            if (!tweenAnimation.isActive || tweenAnimation.id != animationId)
            {
                continue;
            }
            if (!tweenAnimation.isIndependentUpdate)
            {
                Debug.LogWarning($"UIPanel '{panelId}': the '{animationId}' animation does not have Ignore TimeScale ticked, so it freezes while the game is paused. Tick Ignore TimeScale in that DOTween Animation.", tweenAnimation);
            }
            tweenAnimation.CreateTween(true, true);
            if (tweenAnimation.tween != null && tweenAnimation.loops >= 0)
            {
                started = tweenAnimation.tween;
            }
        }
        return started;
    }

    private bool HasManager()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError($"UIPanel '{panelId}' on '{name}': no UIManager found. Make sure the GameSystems prefab (with UIRoot inside it) is in the scene.", this);
            return false;
        }
        return true;
    }
}
