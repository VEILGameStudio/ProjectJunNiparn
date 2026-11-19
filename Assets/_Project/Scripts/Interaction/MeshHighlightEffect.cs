// MeshHighlightEffect
// The highlight for Props 3/4 (box props built from three quads). Shows a soft white outline
// around the whole box in the same two situations as HighlightEffect:
//   1. The player walks near it.
//   2. The player has not interacted with anything for a while (a hint), handled
//      by the HintManager.
// The outline is a slightly bigger box drawn behind the prop (shader Project/PropOutline).
// This script only switches that box's renderer on and off and sets its colour through a
// MaterialPropertyBlock, so the shared material file is never changed.
//
// Put this on: the root of the 3/4 prop prefab (the prefab made by prop_faces.py has it already).
// Assign in Inspector:
//   - Outline Renderer: the MeshRenderer of the 'Outline' child.
//   - Outline Color: how the highlight looks. A soft white (low alpha) looks best.
//   - Show Distance: how close the player must be to light it up.
//   - Use Proximity / Use Idle Hint: which triggers turn the highlight on.
// How thick the outline is: change the scale of the 'Outline' child. It is the box size plus
// twice the thickness you want (in units). The tool sets it to 0.04 units on each side.

using UnityEngine;

public class MeshHighlightEffect : MonoBehaviour
{
    [Header("Look")]
    [Tooltip("The MeshRenderer of the 'Outline' child (a slightly bigger box using the PropOutline material).")]
    [SerializeField] private MeshRenderer outlineRenderer;

    [Tooltip("The color of the outline. A soft white (low alpha) looks best.")]
    [SerializeField] private Color outlineColor = new Color(1f, 1f, 1f, 0.6f);

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

    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

    private MaterialPropertyBlock propertyBlock;
    private Transform player;
    private bool isShown;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        if (outlineRenderer == null)
        {
            Debug.LogError($"MeshHighlightEffect on '{name}': Outline Renderer is not assigned. Drag the 'Outline' child of this prop here.", this);
            enabled = false;
            return;
        }
        SetOutline(false); // Start hidden.
    }

    // Hides the outline when switched off.
    private void OnDisable()
    {
        isShown = false;
        if (outlineRenderer != null)
        {
            outlineRenderer.enabled = false;
        }
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

    // Turns the outline box on or off and sets its colour (no material copies made).
    private void SetOutline(bool show)
    {
        outlineRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(OutlineColorId, outlineColor);
        outlineRenderer.SetPropertyBlock(propertyBlock);
        outlineRenderer.enabled = show;
    }
}
