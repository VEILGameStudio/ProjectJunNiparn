// CookieBoxPrefabBuilder
// Editor-only tool: builds the CookieBoxPuzzle prefab from Kungnang's cookie box art
// (Assets/Puzzle/box), so nobody has to assemble the close-up by hand.
// Run it from: Tools > JunNiparn > Create Cookie Box Prefab.
// It writes: Assets/_Project/Prefabs/Puzzles/CookieBoxPuzzle.prefab
//
// What the prefab looks like:
//   CookieBoxPuzzle        layer Interactable, click box, CookieBoxInteractable
//     Visual               the taped box sprite, SpriteBillboard, no shadows
//     Blocker              layer Blocking, small solid box at the base
//     CloseUpCanvas        Screen Space Overlay canvas, 1920 x 1080, Match 0.5
//       CloseUp            switched off; dark background, CookieBoxCloseUp
//         Box              Tape-on cookie, with Cut / Opened / Lid / TapeScraps on top
//         Cutter           the cutter button (only shown when the player has the cutter)
//         CloseButton
// Put this on: nothing. It is an Editor script.

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CookieBoxPrefabBuilder
{
    private const string ArtFolder = "Assets/Puzzle/box/";
    private const string CutterItemPath = "Assets/Puzzle/box/Cutter_Item.asset";
    private const string SpriteMaterialPath = "Assets/_Project/Art/Materials/SpriteLit.mat";
    private const string PrefabFolder = "Assets/_Project/Prefabs/Puzzles";
    private const string PrefabPath = PrefabFolder + "/CookieBoxPuzzle.prefab";

    // How wide the box stands in the world, in units.
    private const float WorldWidth = 0.6f;

    // Measured from the 8192 px art: the drawn box is 5280 px wide, and its bottom edge is
    // 1769 px below the centre of the picture (the sprite's pivot).
    private const float DrawnWidthPixels = 5280f;
    private const float DrawnHeightPixels = 3812f;
    private const float DrawnBottomBelowPivotPixels = 1769f;
    private const float ArtPixelsPerUnit = 100f;

    // Builds the prefab and selects it in the Project window.
    [MenuItem("Tools/JunNiparn/Create Cookie Box Prefab")]
    public static void CreatePrefab()
    {
        Sprite tapedBox = LoadSprite("Tape-on cookie.png");
        Sprite cutBox = LoadSprite("cookie.png");
        Sprite openedBox = LoadSprite("Incookie.png");
        Sprite lidSprite = LoadSprite("Cookie jar lid.png");
        Sprite tapeScrapSprite = LoadSprite("tape1.png");
        Sprite cutterSprite = LoadSprite("Cutter.png");
        ItemData cutterItem = AssetDatabase.LoadAssetAtPath<ItemData>(CutterItemPath);
        Material spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath);

        if (tapedBox == null || cutBox == null || openedBox == null || lidSprite == null || tapeScrapSprite == null || cutterSprite == null || cutterItem == null)
        {
            EditorUtility.DisplayDialog("Cookie Box", "Some cookie box art or Cutter_Item is missing in " + ArtFolder + ". See the Console for which one.", "OK");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            bool replace = EditorUtility.DisplayDialog("Cookie Box", PrefabPath + " already exists. Replace it? Copies placed in scenes keep their own changes only where they overrode the prefab.", "Replace", "Cancel");
            if (!replace)
            {
                return;
            }
        }

        GameObject root = new GameObject("CookieBoxPuzzle");
        root.layer = LayerMask.NameToLayer(GameLayers.Interactable);

        SpriteRenderer worldSprite = BuildWorldVisual(root.transform, tapedBox, spriteMaterial);
        BuildColliders(root);
        CookieBoxCloseUp closeUp = BuildCloseUp(root.transform, tapedBox, cutBox, openedBox, lidSprite, tapeScrapSprite, cutterSprite, cutterItem);

        CookieBoxInteractable interactable = root.AddComponent<CookieBoxInteractable>();
        SerializedObject serialized = new SerializedObject(interactable);
        serialized.FindProperty("uniqueId").stringValue = "cookie_box";
        serialized.FindProperty("maxInteractDistance").floatValue = 2f;
        serialized.FindProperty("closeUp").objectReferenceValue = closeUp;
        serialized.FindProperty("worldSprite").objectReferenceValue = worldSprite;
        serialized.FindProperty("openedSprite").objectReferenceValue = openedBox;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            Debug.LogError("CookieBoxPrefabBuilder: the folder " + PrefabFolder + " does not exist. Create it in the Project window and run the tool again.");
            Object.DestroyImmediate(root);
            return;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("CookieBoxPrefabBuilder: made " + PrefabPath + ". Drag it into a ThreeQuarter room and give each copy its own Unique Id.");
    }

    // The upright taped box the player sees in the room.
    private static SpriteRenderer BuildWorldVisual(Transform parent, Sprite sprite, Material material)
    {
        float scale = WorldWidth / (DrawnWidthPixels / ArtPixelsPerUnit);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(parent, false);
        visual.transform.localScale = new Vector3(scale, scale, scale);
        // Lift the picture so the bottom of the drawn box sits on the floor.
        visual.transform.localPosition = new Vector3(0f, DrawnBottomBelowPivotPixels / ArtPixelsPerUnit * scale, 0f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }
        else
        {
            Debug.LogWarning("CookieBoxPrefabBuilder: " + SpriteMaterialPath + " was not found. Give the Visual sprite an alpha-clipped sprite material by hand.");
        }

        visual.AddComponent<SpriteBillboard>();
        return renderer;
    }

    // The click box on the root, and a small solid Blocker child at the base.
    private static void BuildColliders(GameObject root)
    {
        float worldHeight = WorldWidth * DrawnHeightPixels / DrawnWidthPixels;

        BoxCollider clickBox = root.AddComponent<BoxCollider>();
        clickBox.isTrigger = true;
        clickBox.size = new Vector3(WorldWidth, worldHeight, WorldWidth * 0.7f);
        clickBox.center = new Vector3(0f, worldHeight * 0.5f, 0f);

        GameObject blocker = new GameObject("Blocker");
        blocker.transform.SetParent(root.transform, false);
        blocker.layer = LayerMask.NameToLayer(GameLayers.Blocking);
        BoxCollider solid = blocker.AddComponent<BoxCollider>();
        solid.size = new Vector3(WorldWidth * 0.8f, 0.3f, WorldWidth * 0.5f);
        solid.center = new Vector3(0f, 0.15f, 0f);
    }

    // The close-up canvas and its switched-off panel.
    private static CookieBoxCloseUp BuildCloseUp(Transform parent, Sprite tapedBox, Sprite cutBox, Sprite openedBox, Sprite lidSprite, Sprite tapeScrapSprite, Sprite cutterSprite, ItemData cutterItem)
    {
        int uiLayer = LayerMask.NameToLayer("UI");

        GameObject canvasObject = new GameObject("CloseUpCanvas", typeof(RectTransform));
        canvasObject.layer = uiLayer;
        canvasObject.transform.SetParent(parent, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        // A full-screen dark background that also stops clicks reaching the world.
        GameObject panel = MakeUiObject("CloseUp", canvasObject.transform, uiLayer);
        StretchToParent(panel.GetComponent<RectTransform>());
        Image background = panel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.75f);

        // 800 x 800 keeps the box inside the 935-unit canvas height at 21:9.
        GameObject box = MakeImage("Box", panel.transform, uiLayer, tapedBox, new Vector2(0f, 0f), new Vector2(800f, 800f));
        GameObject cutLayer = MakeLayer("Cut", box.transform, uiLayer, cutBox);
        GameObject openedLayer = MakeLayer("Opened", box.transform, uiLayer, openedBox);
        GameObject lid = MakeLayer("Lid", box.transform, uiLayer, lidSprite);
        GameObject tapeScraps = MakeLayer("TapeScraps", box.transform, uiLayer, tapeScrapSprite);

        GameObject cutter = MakeImage("Cutter", panel.transform, uiLayer, cutterSprite, new Vector2(560f, 0f), new Vector2(120f, 420f));
        Image cutterImage = cutter.GetComponent<Image>();
        cutterImage.raycastTarget = true;
        Button cutterButton = cutter.AddComponent<Button>();
        cutterButton.targetGraphic = cutterImage;
        cutter.SetActive(false);

        Button closeButton = MakeCloseButton(panel.transform, uiLayer);

        CookieBoxCloseUp closeUp = panel.AddComponent<CookieBoxCloseUp>();
        SerializedObject serialized = new SerializedObject(closeUp);
        serialized.FindProperty("cutterItem").objectReferenceValue = cutterItem;
        serialized.FindProperty("cutterButton").objectReferenceValue = cutterButton;
        serialized.FindProperty("cutLayer").objectReferenceValue = cutLayer;
        SerializedProperty scraps = serialized.FindProperty("tapeScraps");
        scraps.arraySize = 1;
        scraps.GetArrayElementAtIndex(0).objectReferenceValue = tapeScraps;
        serialized.FindProperty("openedLayer").objectReferenceValue = openedLayer;
        serialized.FindProperty("lid").objectReferenceValue = lid.GetComponent<RectTransform>();
        serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        return closeUp;
    }

    // A box layer drawn on top of the box, same size, switched off at the start.
    private static GameObject MakeLayer(string objectName, Transform parent, int layer, Sprite sprite)
    {
        GameObject layerObject = MakeUiObject(objectName, parent, layer);
        StretchToParent(layerObject.GetComponent<RectTransform>());
        Image image = layerObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        layerObject.SetActive(false);
        return layerObject;
    }

    private static GameObject MakeImage(string objectName, Transform parent, int layer, Sprite sprite, Vector2 position, Vector2 size)
    {
        GameObject imageObject = MakeUiObject(objectName, parent, layer);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return imageObject;
    }

    // The X button at the top right of the box.
    private static Button MakeCloseButton(Transform parent, int layer)
    {
        GameObject buttonObject = MakeUiObject("CloseButton", parent, layer);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(420f, 360f);
        rect.sizeDelta = new Vector2(80f, 80f);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.9f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject label = MakeUiObject("Label", buttonObject.transform, layer);
        StretchToParent(label.GetComponent<RectTransform>());
        TextMeshProUGUI text = label.AddComponent<TextMeshProUGUI>();
        text.text = "X";
        text.fontSize = 48f;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return button;
    }

    private static GameObject MakeUiObject(string objectName, Transform parent, int layer)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.layer = layer;
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Loads the first sprite in an art file (Cutter.png is cut into several, the first is the cutter).
    private static Sprite LoadSprite(string fileName)
    {
        string path = ArtFolder + fileName;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            Sprite sprite = asset as Sprite;
            if (sprite != null)
            {
                return sprite;
            }
        }
        Debug.LogError("CookieBoxPrefabBuilder: no sprite found at " + path + ".");
        return null;
    }
}
