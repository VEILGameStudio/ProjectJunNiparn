// FoliagePusher
// Makes leaves and grass part around this object when it moves through or past them, like the
// player brushing a bush. It only works out how hard and which way it pushes; the scene's
// FoliageWindFeed hands that to the foliage shader (SpriteLitNormal) every frame.
// The push follows the movement and fades out after the object stops, so the leaves settle
// back instead of snapping. It freezes while the game is paused.
//
// Put this on: the Player root (the object that moves). An NPC or a monster can have one too.
// Assign in Inspector: nothing is required. Radius, Center Height, Full Push Speed and Fade Time
//   can be tuned.

using System.Collections.Generic;
using UnityEngine;

public class FoliagePusher : MonoBehaviour
{
    [Header("Reach")]
    [Tooltip("How far around this object leaves are pushed, in world units, measured across the screen. About 1.5 x the character's height.")]
    [SerializeField] private float radius = 2.5f;

    [Tooltip("Height of the push centre above this object's feet, in world units. About half the character's height.")]
    [SerializeField] private float centerHeight = 0.9f;

    [Header("Strength")]
    [Tooltip("Moving at this speed (world units per second) gives the full push. Walking slower pushes less.")]
    [SerializeField] private float fullPushSpeed = 6f;

    [Tooltip("Seconds for the push to ease toward the new movement, and to fade after stopping.")]
    [SerializeField] private float fadeTime = 0.6f;

    // Every pusher that is switched on. FoliageWindFeed reads this list.
    public static readonly List<FoliagePusher> Active = new List<FoliagePusher>();

    // Push direction times strength, from 0 (still) to 1 long (full push).
    public Vector3 Push { get; private set; }

    public float Radius => radius;

    public Vector3 Center => transform.position + Vector3.up * centerHeight;

    private Vector3 lastPosition;

    private void OnEnable()
    {
        Active.Add(this);
        lastPosition = transform.position;
        Push = Vector3.zero;
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    // Turns this frame's movement into the push the leaves feel.
    private void Update()
    {
        float deltaTime = Time.deltaTime;
        Vector3 velocity = Vector3.zero;
        if (deltaTime > 0f)
        {
            velocity = (transform.position - lastPosition) / deltaTime;
        }
        lastPosition = transform.position;
        Push = CalculatePush(Push, velocity, fullPushSpeed, fadeTime, deltaTime);
    }

    // Eases the previous push toward the current velocity, scaled so Full Push Speed gives a
    // push 1 long. A zero deltaTime (paused) keeps the push exactly as it was.
    // Public and static so it can be tested without a scene.
    public static Vector3 CalculatePush(Vector3 previousPush, Vector3 velocity, float fullPushSpeed, float fadeTime, float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return previousPush;
        }

        Vector3 target = Vector3.ClampMagnitude(velocity / Mathf.Max(fullPushSpeed, 0.01f), 1f);
        float blend = 1f - Mathf.Exp(-deltaTime / Mathf.Max(fadeTime, 0.01f));
        return Vector3.Lerp(previousPush, target, blend);
    }
}
