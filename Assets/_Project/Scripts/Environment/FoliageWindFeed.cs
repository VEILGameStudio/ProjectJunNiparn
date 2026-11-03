// FoliageWindFeed
// Sends every FoliagePusher (the player, and anyone else that has one) to the foliage shader
// once per frame, so trees and grass drawn with SpriteLitNormal part around them.
// The wind itself needs no script: it runs in the shader. Without this component the wind still
// blows, but nothing pushes the leaves.
// It is not a singleton: one per scene that has pushable foliage.
//
// Put this on: one always-on GameObject in the scene (for example an empty "FoliageWind" object
//   at the top of the Hierarchy, outside every camera zone).
// Assign in Inspector: nothing.

using UnityEngine;

public class FoliageWindFeed : MonoBehaviour
{
    // Must match FOLIAGE_MAX_PUSHERS in SpriteLitNormal.shader.
    public const int MaxPushers = 8;

    private static readonly int PositionId = Shader.PropertyToID("_FoliagePusherPosition");
    private static readonly int PushId = Shader.PropertyToID("_FoliagePusherPush");
    private static readonly int CountId = Shader.PropertyToID("_FoliagePusherCount");

    private readonly Vector4[] positions = new Vector4[MaxPushers];
    private readonly Vector4[] pushes = new Vector4[MaxPushers];
    private bool warnedAboutTooMany;

    // Copies each pusher's centre, radius and push into the shader after everything has moved.
    private void LateUpdate()
    {
        int count = FoliagePusher.Active.Count;
        if (count > MaxPushers)
        {
            if (!warnedAboutTooMany)
            {
                Debug.LogWarning($"FoliageWindFeed on '{name}': {count} FoliagePushers are switched on, but the shader only takes {MaxPushers}. The extra ones will not push leaves.", this);
                warnedAboutTooMany = true;
            }
            count = MaxPushers;
        }

        for (int i = 0; i < count; i++)
        {
            FoliagePusher pusher = FoliagePusher.Active[i];
            Vector3 center = pusher.Center;
            positions[i] = new Vector4(center.x, center.y, center.z, pusher.Radius);
            pushes[i] = pusher.Push;
        }

        Shader.SetGlobalVectorArray(PositionId, positions);
        Shader.SetGlobalVectorArray(PushId, pushes);
        Shader.SetGlobalFloat(CountId, count);
    }

    // Stops pushing when the scene closes, so the next scene does not inherit an old push.
    private void OnDisable()
    {
        Shader.SetGlobalFloat(CountId, 0f);
    }
}
