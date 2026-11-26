// MeshHighlightEffect
// The highlight for Props 3/4 (box props built from three quads). Shows a soft white outline
// around the whole box while the mouse points at it, like HighlightEffect
// (the check is done once per frame by MouseHoverTarget).
// The outline is a slightly bigger box drawn behind the prop (shader Project/PropOutline).
// This script only switches that box's renderer on and off and sets its colour through a
// MaterialPropertyBlock, so the shared material file is never changed.
//
// Put this on: the root of the 3/4 prop prefab (the prefab made by prop_faces.py has it already).
// Assign in Inspector:
//   - Outline Renderer: the MeshRenderer of the 'Outline' child.
//   - Outline Color: how the highlight looks. A soft white (low alpha) looks best.
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

    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

    private MaterialPropertyBlock propertyBlock;
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
        bool shouldShow = MouseHoverTarget.IsHovering(transform);
        if (shouldShow != isShown)
        {
            isShown = shouldShow;
            SetOutline(shouldShow);
        }
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
