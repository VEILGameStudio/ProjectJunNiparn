// ActivationMode
// How an interactable object is started:
//   Click = the player clicks it with the mouse.
//   Touch = the player bumps into its small solid Blocker, or walks into its
//           TouchZone trigger. The big click area never counts as a touch.
//   Both  = either one works.
//
// Put this on: nothing. It is a list of choices used by interactables.

public enum ActivationMode
{
    Click,
    Touch,
    Both
}
