// CookieBoxInteractable
// The taped cookie box standing in the world. Clicking it opens its close-up view
// (CookieBoxCloseUp). If the player has the Cutter, they can cut the tape there and open the
// box. No key is needed and no item is given: what the open box unlocks is wired on On Opened.
// Once opened, the save file remembers it: after loading, the box is open again and
// On Opened runs again (silently), so anything wired to it is back in its opened state.
//
// Put this on: the root of the CookieBoxPuzzle prefab (layer Interactable, with the click box).
// Assign in Inspector: Close Up, World Sprite, Opened Sprite, and a Unique Id that no other
//   object in the scene has (for example 'forest_cookie_box').

using UnityEngine;
using UnityEngine.Events;

public class CookieBoxInteractable : InteractableBase
{
    [Header("Cookie Box")]
    [Tooltip("The CloseUp panel inside this prefab (it has the CookieBoxCloseUp script).")]
    [SerializeField] private CookieBoxCloseUp closeUp;

    [Tooltip("The SpriteRenderer of the box in the world (the 'Visual' child).")]
    [SerializeField] private SpriteRenderer worldSprite;

    [Tooltip("The picture the world box switches to once it is open (Incookie.png). Leave empty to keep the taped picture.")]
    [SerializeField] private Sprite openedSprite;

    [Header("Events")]
    [Tooltip("Runs when the box is opened: wire the door, sound or puzzle step it unlocks here. It also runs again after loading a save where the box was open, so put sounds in a sound field, not here.")]
    [SerializeField] private UnityEvent onOpened = new UnityEvent();

    // Opening the box happens once, and the save file remembers it.
    protected override bool UsedOnlyOnce => true;

    protected override void OnStarted()
    {
        if (closeUp == null)
        {
            Debug.LogError($"CookieBoxInteractable on '{name}': Close Up is not assigned. Drag the 'CloseUp' child (with CookieBoxCloseUp) here.", this);
        }
        if (worldSprite == null)
        {
            Debug.LogError($"CookieBoxInteractable on '{name}': World Sprite is not assigned. Drag the 'Visual' child here.", this);
        }
    }

    protected override void OnEnabled()
    {
        if (closeUp != null)
        {
            closeUp.Opened += HandleOpened;
        }
    }

    protected override void OnDisabled()
    {
        if (closeUp != null)
        {
            closeUp.Opened -= HandleOpened;
        }
    }

    // Clicked: show the close-up. It decides whether the cutter appears.
    protected override void OnInteract(PlayerContext player)
    {
        if (closeUp == null)
        {
            return;
        }
        Inventory inventory = player != null ? player.Inventory : null;
        closeUp.Show(inventory);
    }

    // The tape was cut and the lid is off: remember it, show it open, run On Opened.
    private void HandleOpened()
    {
        MarkUsed();
        ShowOpenInWorld();
        onOpened.Invoke();
    }

    // After loading a save where the box was open. Only sets state; plays nothing.
    protected override void OnRestoredAsUsed()
    {
        base.OnRestoredAsUsed();
        if (closeUp != null)
        {
            closeUp.ShowOpenedState();
        }
        ShowOpenInWorld();
        onOpened.Invoke();
    }

    private void ShowOpenInWorld()
    {
        if (worldSprite != null && openedSprite != null)
        {
            worldSprite.sprite = openedSprite;
        }
    }
}
