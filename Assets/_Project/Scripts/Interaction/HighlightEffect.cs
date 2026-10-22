// HighlightEffect
// Makes an interactable sprite show a soft white outline in two situations:
//   1. The player walks near it.
//   2. The player has not interacted with anything for a while (a hint), handled
//      by the HintManager.
// It turns the outline on by setting _OutlineThickness and _OutlineColor through a
// MaterialPropertyBlock on this one renderer only. The shared material file is never
// changed, so other sprites do not light up and nothing is left changed after Play mode.
//
// Put this on: the interactable sprite GameObject (it needs a SpriteRenderer using the
//   SpriteLit material, shader "Game/SpriteLitOutline"). The sprite needs a transparent
//   border at least as wide as the outline, and its import setting Mesh Type = Full Rect,
//   or the outline has nowhere to be drawn.
// Assign in Inspector:
//   - Outline Color / Outline Thickness: how the highlight looks.
//   - Show Distance: how close the player must be to light it up.
//   - Use Proximity / Use Idle Hint: which triggers turn the highlight on.

using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HighlightEffect : MonoBehaviour
{
    [Header("Look")]
    [Tooltip("The color of the outline. A soft white (low alpha) looks best.")]
    [SerializeField] private Color outlineColor = new Color(1f, 1f, 1f, 0.6f);

    [Tooltip("How thick the outline is when shown.")]
    [SerializeField] private float outlineThickness = 2f;

    [Header("Proximity")]
    [Tooltip("Turn the outline on when the player is this close (in units).")]
    [SerializeField] private float showDistance = 3f;

    [Tooltip("The tag on the player object.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Triggers")]
    [Tooltip("Highlight when the player is near.")]
    [SerializeField] private bool useProximity = true;

    [Tooltip("Highlight as a hint when the player has been idle too long.")]
    [SerializeField] private bool useIdleHint = true;

    private static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Transform player;
    private bool isShown;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        SetOutline(false); // Start hidden.
    }

    // Hides the outline when switched off (for example after a one-shot interactable is used).
    private void OnDisable()
    {
        isShown = false;
        SetOutline(false);
    }

    private void Update()
    {
        bool shouldShow = ShouldShow();
        if (shouldShow != isShown)
        {
            isShown = shouldShow;
            SetOutline(shouldShow);
        }
    }

    // Decides whether the outline should be visible right now.
    private bool ShouldShow()
    {
        if (useProximity && IsPlayerNear())
        {
            return true;
        }
        if (useIdleHint && IsHintTime())
        {
            return true;
        }
        return false;
    }

    // True when the HintManager says the player has been idle long enough.
    private bool IsHintTime()
    {
        return GameManager.Instance != null
            && GameManager.Instance.HintManager != null
            && GameManager.Instance.HintManager.ShouldShowHint;
    }

    // True when the player is within Show Distance.
    private bool IsPlayerNear()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag(playerTag);
            if (found != null)
            {
                player = found.transform;
            }
        }

        return player != null && Vector3.Distance(player.position, transform.position) <= showDistance;
    }

    // Turns the outline on or off using a property block (no material copies made).
    private void SetOutline(bool show)
    {
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(OutlineThicknessId, show ? outlineThickness : 0f);
        propertyBlock.SetColor(OutlineColorId, outlineColor);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }
}
