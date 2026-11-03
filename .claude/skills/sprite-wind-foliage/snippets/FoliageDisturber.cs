// SKILL REFERENCE — not in Assets/, not compiled, not tested in Unity.
// Two small scripts for the player-push half of foliage wind. Copy into the project only after
// Oak decides the folder (Scripts/Player is Oak's) and approves the CLAUDE.md wind change.
// Unity needs one MonoBehaviour per file with a matching name: split into FoliageDisturber.cs
// and FoliageWindFeed.cs when copying.

using System.Collections.Generic;
using UnityEngine;

// FoliageDisturber
// Marks something that pushes leaves and grass aside when it moves through them (the player, an NPC).
// Put this on: the Player GameObject (or any moving character).
// Assign in Inspector: Radius, Center Height, Full Push Speed, Fade Time. Nothing to drag in.
public class FoliageDisturber : MonoBehaviour
{
    [Header("Push")]
    [Tooltip("How far around this object leaves are pushed, in world units. About 1.5 x the character's height.")]
    [SerializeField] private float radius = 2.5f;

    [Tooltip("Height of the push centre above this object's pivot (the feet), in world units. About half the character's height.")]
    [SerializeField] private float centerHeight = 0.9f;

    [Tooltip("Speed (units per second) that gives the full push. Walking slower pushes less.")]
    [SerializeField] private float fullPushSpeed = 6f;

    [Tooltip("Seconds for the push to fade after the character stops, so leaves settle instead of snapping back.")]
    [SerializeField] private float fadeTime = 0.6f;

    public static readonly List<FoliageDisturber> Active = new List<FoliageDisturber>();

    public Vector3 Push { get; private set; }
    public float Radius => radius;
    public Vector3 Center => transform.position + Vector3.up * centerHeight;

    private Vector3 lastPosition;

    private void OnEnable()
    {
        Active.Add(this);
        lastPosition = transform.position;
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    // Turns this frame's movement into a push that eases toward the current velocity.
    private void Update()
    {
        if (Time.deltaTime <= 0f)
        {
            return; // paused: keep the leaves exactly where they are
        }
        Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
        Vector3 target = Vector3.ClampMagnitude(velocity / fullPushSpeed, 1f);
        float blend = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(fadeTime, 0.01f));
        Push = Vector3.Lerp(Push, target, blend);
    }
}

// FoliageWindFeed
// Sends every active FoliageDisturber to the foliage shader once per frame. One per scene, not a singleton.
// Put this on: any always-on object in the scene (for example the scene's root "Systems" object).
// Assign in Inspector: nothing.
public class FoliageWindFeed : MonoBehaviour
{
    private const int MaxDisturbers = 8; // must match FOLIAGE_MAX_DISTURBERS in FoliageFlow.hlsl

    private static readonly int PositionId = Shader.PropertyToID("_FoliageDisturberPosition");
    private static readonly int PushId = Shader.PropertyToID("_FoliageDisturberPush");
    private static readonly int CountId = Shader.PropertyToID("_FoliageDisturberCount");

    private readonly Vector4[] positions = new Vector4[MaxDisturbers];
    private readonly Vector4[] pushes = new Vector4[MaxDisturbers];

    // Copies positions and pushes into the shader globals after everything has moved.
    private void LateUpdate()
    {
        int count = Mathf.Min(FoliageDisturber.Active.Count, MaxDisturbers);
        for (int i = 0; i < count; i++)
        {
            FoliageDisturber disturber = FoliageDisturber.Active[i];
            Vector3 position = disturber.Center;
            positions[i] = new Vector4(position.x, position.y, position.z, disturber.Radius);
            pushes[i] = disturber.Push;
        }
        Shader.SetGlobalVectorArray(PositionId, positions);
        Shader.SetGlobalVectorArray(PushId, pushes);
        Shader.SetGlobalFloat(CountId, count);
    }

    private void OnDisable()
    {
        Shader.SetGlobalFloat(CountId, 0f);
    }
}
