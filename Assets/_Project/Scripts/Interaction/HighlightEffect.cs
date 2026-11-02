// HighlightEffect
// Makes an interactable sprite show a soft white outline in two situations:
//   1. The player walks near it.
//   2. The player has not interacted with anything for a while (a hint), handled
//      by the HintManager.
// It turns the outline on by setting _OutlineThickness and _OutlineColor through a
// MaterialPropertyBlock on this one renderer only. The shared material file is never
// changed, so other sprites do not light up and nothing is left changed after Play mode.
// Outline Width is measured on the screen (pixels on a 1080-high screen), so every object
// gets the same outline whatever the size or Pixels Per Unit of its picture.
//
// Put this on: the interactable sprite GameObject (it needs a SpriteRenderer using the
//   SpriteLit material, shader "Game/SpriteLitOutline"). The sprite's import setting must be
//   Mesh Type = Full Rect, and the picture needs a transparent border around the drawing,
//   or the outline has nowhere to be drawn. A border of 8 picture pixels is enough for
//   pictures drawn at the screen size they are shown at.
// Assign in Inspector:
//   - Outline Color / Outline Width: how the highlight looks.
//   - Show Distance: how close the player must be to light it up.
//   - Use Proximity / Use Idle Hint: which triggers turn the highlight on.

using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HighlightEffect : MonoBehaviour
{
    [Header("Look")]
    [Tooltip("The color of the outline. A soft white (low alpha) looks best.")]
    [SerializeField] private Color outlineColor = new Color(1f, 1f, 1f, 0.6f);

    [Tooltip("How wide the outline is on the screen, in pixels on a 1080-high screen. It grows with bigger screens like the rest of the picture. The picture needs a transparent border at least this wide.")]
    [SerializeField] private float outlineWidth = 4f;

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

    // Outline Width is given for a screen this many pixels high.
    private const float ReferenceScreenHeight = 1080f;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Transform player;
    private bool isShown;
    private bool reportedNoCamera;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        ReportSetupProblems();
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
        propertyBlock.SetFloat(OutlineThicknessId, show ? OutlineWidthInPicturePixels() : 0f);
        propertyBlock.SetColor(OutlineColorId, outlineColor);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    // The shader counts the outline in picture pixels; this turns Outline Width into that
    // number for this sprite and the camera that is looking at it right now.
    private float OutlineWidthInPicturePixels()
    {
        Camera cam = Camera.main;
        if (cam == null || spriteRenderer.sprite == null)
        {
            if (!reportedNoCamera)
            {
                reportedNoCamera = true;
                Debug.LogWarning($"HighlightEffect on '{name}': there is no Main Camera (or no sprite), so Outline Width is used as picture pixels and the outline may look too thin or too thick.", this);
            }
            return outlineWidth;
        }

        float picturePixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit / Mathf.Abs(transform.lossyScale.y);
        float distance = Vector3.Dot(transform.position - cam.transform.position, cam.transform.forward);
        return WidthInPicturePixels(outlineWidth, picturePixelsPerUnit, cam.orthographic, cam.orthographicSize, cam.fieldOfView, distance);
    }

    // How many picture pixels make an outline widthOnScreen pixels wide on a 1080-high screen.
    // Orthographic cameras show the same size at every distance; perspective cameras do not.
    // Public and static so tests can check it without a scene.
    public static float WidthInPicturePixels(float widthOnScreen, float picturePixelsPerUnit, bool orthographic, float orthographicSize, float fieldOfView, float distance)
    {
        float visibleHeight = orthographic
            ? 2f * orthographicSize
            : 2f * distance * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        if (visibleHeight <= 0f)
        {
            return widthOnScreen;
        }

        float screenPixelsPerUnit = ReferenceScreenHeight / visibleHeight;
        return widthOnScreen * picturePixelsPerUnit / screenPixelsPerUnit;
    }

    // Says once, at the start, why the outline would never show.
    private void ReportSetupProblems()
    {
        if (spriteRenderer.sprite == null)
        {
            Debug.LogError($"HighlightEffect on '{name}': the SpriteRenderer here has no sprite. Put the HighlightEffect on the object that shows the picture (usually the 'Visual' child), not on its parent.", this);
            return;
        }
        if (spriteRenderer.sharedMaterial == null || !spriteRenderer.sharedMaterial.HasProperty(OutlineThicknessId))
        {
            Debug.LogError($"HighlightEffect on '{name}': the material cannot draw an outline. Set the SpriteRenderer's Material to 'SpriteLit'.", this);
        }
        if (spriteRenderer.sprite.vertices.Length != 4)
        {
            Debug.LogError($"HighlightEffect on '{name}': the sprite '{spriteRenderer.sprite.name}' is cut tight around the drawing, so the outline has no room. Select the picture file and set Mesh Type to 'Full Rect'.", this);
        }
    }
}
