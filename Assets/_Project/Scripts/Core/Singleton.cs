// Singleton
// A reusable base class for "manager" scripts that should only ever exist once
// (for example GameManager, SaveManager, AudioManager).
// It keeps one shared Instance, destroys any accidental duplicates, and can
// optionally survive scene changes.
//
// How to use in code:
//   public class GameManager : Singleton<GameManager> { ... }
// Then anywhere else:  GameManager.Instance.DoSomething();
//
// Put this on: nothing directly. Other manager scripts inherit from it.
// Assign in Inspector: "Persist Across Scenes" (tick it for global managers).

using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    [Header("Singleton")]
    [Tooltip("Tick this for global managers that must survive scene loads (GameManager, SaveManager, etc.).")]
    [SerializeField] private bool persistAcrossScenes = true;

    // The one shared instance of this manager. Read it from other scripts.
    public static T Instance { get; private set; }

    // Sets up the shared instance and removes duplicates. Runs before Start.
    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Another copy already exists, so this extra one is not needed.
            Destroy(gameObject);
            return;
        }

        Instance = (T)this;

        if (persistAcrossScenes)
        {
            transform.SetParent(null); // DontDestroyOnLoad only works on root objects.
            DontDestroyOnLoad(gameObject);
        }
    }

    // Clears the instance when this object is destroyed.
    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
