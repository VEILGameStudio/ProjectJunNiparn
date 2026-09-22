// DialogueImporter
// A menu button that turns the writer's JSON files into dialogue ScriptableObjects
// the game can use. The writer edits .json files; a programmer or the writer then
// clicks the menu item and the assets are created or updated automatically.
//
// How to use:
//   1. Put .json files in Assets/_Project/Data/Dialogue/Json
//   2. In the top menu click: Tools > Dialogue > Import JSON -> ScriptableObjects
//   3. The matching .asset files appear in Assets/_Project/Data/Dialogue
//
// This script only runs in the Unity Editor (it lives in an Editor folder).

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DialogueImporter
{
    // Where the writer's JSON files live, and where the generated assets go.
    private const string JsonFolder = "Assets/_Project/Data/Dialogue/Json";
    private const string OutputFolder = "Assets/_Project/Data/Dialogue";

    [MenuItem("Tools/Dialogue/Import JSON -> ScriptableObjects")]
    public static void ImportAll()
    {
        if (!Directory.Exists(JsonFolder))
        {
            Debug.LogError($"DialogueImporter: folder '{JsonFolder}' does not exist. Create it and put your .json files there.");
            return;
        }
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            Debug.LogError($"DialogueImporter: output folder '{OutputFolder}' does not exist.");
            return;
        }

        string[] files = Directory.GetFiles(JsonFolder, "*.json");
        int created = 0;
        int updated = 0;
        int failed = 0;

        foreach (string filePath in files)
        {
            if (ImportFile(filePath, ref created, ref updated))
            {
                continue;
            }
            failed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"DialogueImporter finished. Created: {created}, Updated: {updated}, Failed: {failed}, Total files: {files.Length}");
    }

    // Reads one JSON file and creates or updates the matching asset. Returns false on error.
    private static bool ImportFile(string filePath, ref int created, ref int updated)
    {
        string json = File.ReadAllText(filePath);
        DialogueJson data = JsonUtility.FromJson<DialogueJson>(json);

        if (data == null || string.IsNullOrEmpty(data.id) || string.IsNullOrEmpty(data.type))
        {
            Debug.LogError($"DialogueImporter: '{Path.GetFileName(filePath)}' is missing an id or type.");
            return false;
        }

        string assetPath = $"{OutputFolder}/{data.id}.asset";
        bool existedBefore = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) != null;

        switch (data.type.ToLowerInvariant())
        {
            case "full":
                ImportFull(data, assetPath);
                break;
            case "choice":
                ImportChoice(data, assetPath);
                break;
            case "mini":
                ImportMini(data, assetPath);
                break;
            default:
                Debug.LogError($"DialogueImporter: '{Path.GetFileName(filePath)}' has unknown type '{data.type}'. Use full, choice, or mini.");
                return false;
        }

        if (existedBefore)
        {
            updated++;
        }
        else
        {
            created++;
        }
        return true;
    }

    // Builds a full conversation asset.
    private static void ImportFull(DialogueJson data, string assetPath)
    {
        FullDialogueData asset = LoadOrCreate<FullDialogueData>(assetPath);

        List<DialogueLine> lines = new List<DialogueLine>();
        if (data.lines != null)
        {
            foreach (LineJson lineJson in data.lines)
            {
                lines.Add(new DialogueLine
                {
                    speaker = ToLocalized(lineJson.speaker),
                    text = ToLocalized(lineJson.text)
                });
            }
        }

        asset.SetData(data.id, lines);
        EditorUtility.SetDirty(asset);
    }

    // Builds a yes/no question asset.
    private static void ImportChoice(DialogueJson data, string assetPath)
    {
        ChoiceDialogueData asset = LoadOrCreate<ChoiceDialogueData>(assetPath);
        asset.SetData(data.id, ToLocalized(data.question), ToLocalized(data.yesLabel), ToLocalized(data.noLabel));
        EditorUtility.SetDirty(asset);
    }

    // Builds a short popup asset.
    private static void ImportMini(DialogueJson data, string assetPath)
    {
        MiniDialogueData asset = LoadOrCreate<MiniDialogueData>(assetPath);
        asset.SetData(data.id, ToLocalized(data.text));
        EditorUtility.SetDirty(asset);
    }

    // Loads the asset at the path, or creates a new one if it is not there yet.
    private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }
        return asset;
    }

    // Turns the JSON language keys (en, th) into a LocalizedString.
    private static LocalizedString ToLocalized(LocalizedTextJson value)
    {
        if (value == null)
        {
            return new LocalizedString();
        }

        return new LocalizedString
        {
            english = value.en ?? string.Empty,
            thai = value.th ?? string.Empty
        };
    }

    // --- JSON shapes (must match the field names in the JSON files) ---

    [System.Serializable]
    private class DialogueJson
    {
        public string id;
        public string type;          // "full", "choice", or "mini"
        public LineJson[] lines;     // used by "full"
        public LocalizedTextJson question;  // used by "choice"
        public LocalizedTextJson yesLabel;  // used by "choice"
        public LocalizedTextJson noLabel;   // used by "choice"
        public LocalizedTextJson text;      // used by "mini"
    }

    [System.Serializable]
    private class LineJson
    {
        public LocalizedTextJson speaker;
        public LocalizedTextJson text;
    }

    [System.Serializable]
    private class LocalizedTextJson
    {
        public string en;
        public string th;
    }
}
