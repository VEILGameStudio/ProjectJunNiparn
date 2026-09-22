// GameState
// The high-level states the game can be in. GameManager holds the current one.
//
// Put this on: nothing. It is just a list of possible states used by other scripts.

public enum GameState
{
    Playing,   // Normal gameplay. Player can move and interact.
    Paused,    // Menu or inventory is open. Time is frozen.
    Cutscene,  // A cutscene or video is playing. Player input is disabled.
    GameOver   // Player died or ran out of time. Showing the game over sequence.
}
