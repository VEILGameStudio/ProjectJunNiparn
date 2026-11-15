// UIPrefabTools
// Editor menu items that set up the shared UI prefabs. Nothing here runs in the game.
//   Tools > JunNiparn > UI > 1. Split UIRoot Into Nested Prefabs
//       Moves the overrides that GameSystems holds on UIRoot into UIRoot itself, then turns
//       each big part of UIRoot (dialogue panels, inventory, HUD, fader...) into its own
//       prefab in Prefabs/UI, placed back inside UIRoot as a nested prefab. Run it once.
//       Commit first, so the change can be undone with git if anything looks wrong.
//   Tools > JunNiparn > UI > 2. Create Base Widgets
//       Creates UI_Button, UI_Button_Secondary (variant), UI_Panel and UI_Popup (variant)
//       in Prefabs/UI/Widgets. Existing files are never overwritten.
//
// Put this on: nothing. It lives in an Editor folder and only adds menu items.
// Assign in Inspector: nothing.

using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

public static class UIPrefabTools
{
    public const string UIFolder = "Assets/_Project/Prefabs/UI";
    public const string UIRootPath = UIFolder + "/UIRoot.prefab";
    private const string GameSystemsPath = "Assets/_Project/Prefabs/GameSystems.prefab";
    private const string WidgetsFolder = UIFolder + "/Widgets";

    // Each part of UIRoot that becomes its own prefab, and the folder it goes into.
    private static readonly string[] HudParts = { "InventoryHud" };
    private static readonly string[] PanelParts = { "MiniDialoguePanel", "FullDialoguePanel", "ChoiceDialoguePanel", "InventoryPanel", "SettingsPanel" };
    private static readonly string[] OverlayParts = { "SkipButton", "VideoScreen", "FadeImage" };

    [MenuItem("Tools/JunNiparn/UI/1. Split UIRoot Into Nested Prefabs")]
    private static void SplitUIRoot()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(UIRootPath) == null)
        {
            Debug.LogError($"UIPrefabTools: no prefab at {UIRootPath}. Pull the latest dev branch first.");
            return;
        }

        ApplyGameSystemsOverrides();

        GameObject root = PrefabUtility.LoadPrefabContents(UIRootPath);
        int made = 0;
        try
        {
            made += SplitParts(root, HudParts, UIFolder + "/HUD");
            made += SplitParts(root, PanelParts, UIFolder + "/Panels");
            made += SplitParts(root, OverlayParts, UIFolder + "/Overlays");
            PrefabUtility.SaveAsPrefabAsset(root, UIRootPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"UIPrefabTools: made {made} nested prefabs inside UIRoot. Open UIRoot and check that every field on UIManager, InventoryUI and CutsceneManager is still filled in.");
    }

    // Turns each named child of the root into its own prefab and links it back as a nested prefab.
    private static int SplitParts(GameObject root, string[] partNames, string folder)
    {
        int made = 0;
        foreach (string partName in partNames)
        {
            Transform part = root.transform.Find(partName);
            if (part == null)
            {
                Debug.LogWarning($"UIPrefabTools: UIRoot has no child called '{partName}'. Skipped.");
                continue;
            }
            if (PrefabUtility.IsAnyPrefabInstanceRoot(part.gameObject))
            {
                Debug.Log($"UIPrefabTools: '{partName}' is already a prefab. Skipped.");
                continue;
            }

            string path = $"{folder}/{partName}.prefab";
            if (File.Exists(path))
            {
                Debug.LogWarning($"UIPrefabTools: {path} already exists, so '{partName}' was not split. Delete or rename that file first.");
                continue;
            }

            EnsureFolder(folder);
            PrefabUtility.SaveAsPrefabAssetAndConnect(part.gameObject, path, InteractionMode.AutomatedAction, out bool success);
            if (success)
            {
                made++;
            }
            else
            {
                Debug.LogError($"UIPrefabTools: could not save '{partName}' as {path}.");
            }
        }
        return made;
    }

    // Moves every change GameSystems made on its UIRoot copy into UIRoot, so nothing is lost when UIRoot changes shape.
    private static void ApplyGameSystemsOverrides()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(GameSystemsPath) == null)
        {
            return;
        }

        GameObject systems = PrefabUtility.LoadPrefabContents(GameSystemsPath);
        try
        {
            foreach (Transform child in systems.GetComponentsInChildren<Transform>(true))
            {
                GameObject candidate = child.gameObject;
                bool isUIRootCopy = PrefabUtility.IsAnyPrefabInstanceRoot(candidate)
                    && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(candidate) == UIRootPath;
                if (isUIRootCopy && PrefabUtility.HasPrefabInstanceAnyOverrides(candidate, false))
                {
                    PrefabUtility.ApplyPrefabInstance(candidate, InteractionMode.AutomatedAction);
                    Debug.Log("UIPrefabTools: applied the overrides GameSystems had on UIRoot into UIRoot.");
                }
            }
            PrefabUtility.SaveAsPrefabAsset(systems, GameSystemsPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(systems);
        }
    }

    [MenuItem("Tools/JunNiparn/UI/2. Create Base Widgets")]
    private static void CreateBaseWidgets()
    {
        EnsureFolder(WidgetsFolder);

        GameObject button = CreateBaseButton(WidgetsFolder + "/UI_Button.prefab");
        CreateButtonVariant(button, WidgetsFolder + "/UI_Button_Secondary.prefab", new Color(0.35f, 0.35f, 0.4f));
        GameObject panel = CreateBasePanel(WidgetsFolder + "/UI_Panel.prefab");
        CreatePopupVariant(panel, button, WidgetsFolder + "/UI_Popup.prefab");

        AssetDatabase.SaveAssets();
        Debug.Log($"UIPrefabTools: base widgets are in {WidgetsFolder}.");
    }

    // A TextMeshPro button sized for a 1920x1080 canvas.
    private static GameObject CreateBaseButton(string path)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing;
        }

        GameObject button = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources());
        button.name = "UI_Button";
        button.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 80f);
        button.GetComponent<Image>().color = new Color(0.85f, 0.7f, 0.45f);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        label.text = "Button";
        label.fontSize = 36f;
        label.color = new Color(0.15f, 0.1f, 0.05f);

        return SaveAndDestroy(button, path);
    }

    // A variant of the base button that only changes its colour.
    private static void CreateButtonVariant(GameObject baseButton, string path, Color color)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(baseButton);
        instance.name = Path.GetFileNameWithoutExtension(path);
        instance.GetComponent<Image>().color = color;
        SaveAndDestroy(instance, path);
    }

    // A full-screen dimmed background with a window that fits 935 canvas units (21:9), a title, and a UIPanel.
    private static GameObject CreateBasePanel(string path)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing;
        }

        GameObject panel = new GameObject("UI_Panel", typeof(RectTransform), typeof(Image), typeof(UIPanel));
        StretchToParent(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        GameObject window = new GameObject("Window", typeof(RectTransform), typeof(Image));
        window.transform.SetParent(panel.transform, false);
        window.GetComponent<RectTransform>().sizeDelta = new Vector2(1200f, 800f);
        window.GetComponent<Image>().color = new Color(0.95f, 0.9f, 0.8f);

        GameObject title = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        title.transform.SetParent(window.transform, false);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 100f);
        TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
        titleText.text = "Title";
        titleText.fontSize = 48f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.15f, 0.1f, 0.05f);

        return SaveAndDestroy(panel, path);
    }

    // A variant of the panel with a smaller window and a close button (a nested UI_Button) wired to UIPanel.Close.
    private static void CreatePopupVariant(GameObject basePanel, GameObject baseButton, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return;
        }

        GameObject popup = (GameObject)PrefabUtility.InstantiatePrefab(basePanel);
        popup.name = "UI_Popup";
        Transform window = popup.transform.Find("Window");
        window.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 500f);

        GameObject closeButton = (GameObject)PrefabUtility.InstantiatePrefab(baseButton, window);
        closeButton.name = "CloseButton";
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 40f);
        closeButton.GetComponentInChildren<TMP_Text>().text = "Close";

        UnityEventTools.AddVoidPersistentListener(closeButton.GetComponent<Button>().onClick, popup.GetComponent<UIPanel>().Close);
        SaveAndDestroy(popup, path);
    }

    private static GameObject SaveAndDestroy(GameObject instance, string path)
    {
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        return saved;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Creates a folder (and its parents) through the AssetDatabase, so Unity gives it a .meta file.
    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
