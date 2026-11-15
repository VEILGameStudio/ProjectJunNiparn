// UIOverrideGuard
// Keeps the UI prefabs the single source of truth. A UI prefab (anything in Prefabs/UI)
// must look the same in every scene, so a scene must never change it: change the prefab
// instead, and every scene follows. This guard warns in the Console whenever a scene is
// saved with a change on a UI prefab, and adds two menu items:
//   Tools > JunNiparn > UI > Check UI Overrides In Open Scenes
//   Tools > JunNiparn > UI > Revert UI Overrides In Open Scenes
//
// Put this on: nothing. It lives in an Editor folder and runs by itself in the editor.
// Assign in Inspector: nothing.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class UIOverrideGuard
{
    static UIOverrideGuard()
    {
        EditorSceneManager.sceneSaving += HandleSceneSaving;
    }

    private static void HandleSceneSaving(Scene scene, string path)
    {
        ReportOrRevert(scene, false);
    }

    [MenuItem("Tools/JunNiparn/UI/Check UI Overrides In Open Scenes")]
    private static void CheckOpenScenes()
    {
        int found = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            found += ReportOrRevert(SceneManager.GetSceneAt(i), false);
        }
        if (found == 0)
        {
            Debug.Log("UIOverrideGuard: no scene changes on UI prefabs. Good.");
        }
    }

    [MenuItem("Tools/JunNiparn/UI/Revert UI Overrides In Open Scenes")]
    private static void RevertOpenScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (ReportOrRevert(scene, true) > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }

    // Finds every change a scene makes on a UI prefab. Logs each one, or reverts it when asked. Returns how many.
    private static int ReportOrRevert(Scene scene, bool revert)
    {
        int count = 0;
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return count;
        }

        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            foreach (Transform child in sceneRoot.GetComponentsInChildren<Transform>(true))
            {
                GameObject instance = child.gameObject;
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(instance))
                {
                    continue;
                }

                foreach (ObjectOverride change in PrefabUtility.GetObjectOverrides(instance, false))
                {
                    count += Handle(change, change.instanceObject, "changed value", scene, revert);
                }
                foreach (AddedGameObject change in PrefabUtility.GetAddedGameObjects(instance))
                {
                    count += Handle(change, change.instanceGameObject.transform.parent, "added object", scene, revert);
                }
                foreach (AddedComponent change in PrefabUtility.GetAddedComponents(instance))
                {
                    count += Handle(change, change.instanceComponent, "added component", scene, revert);
                }
                foreach (RemovedComponent change in PrefabUtility.GetRemovedComponents(instance))
                {
                    count += Handle(change, change.containingInstanceGameObject, "removed component", scene, revert);
                }
            }
        }
        return count;
    }

    // Logs or reverts one change when it sits inside a UI prefab. Returns 1 if it did, 0 if not.
    private static int Handle(PrefabOverride change, Object target, string kind, Scene scene, bool revert)
    {
        string uiPrefabPath = FindUIPrefabPath(target);
        if (uiPrefabPath == null)
        {
            return 0;
        }

        if (revert)
        {
            change.Revert(InteractionMode.UserAction);
            Debug.Log($"UIOverrideGuard: reverted a scene change ({kind}) on '{target.name}' in scene '{scene.name}'.");
        }
        else
        {
            Debug.LogWarning($"UIOverrideGuard: scene '{scene.name}' has a {kind} on '{target.name}', which belongs to the UI prefab {uiPrefabPath}. UI is changed on the prefab, never in a scene. Open the prefab and make the change there, then use Tools > JunNiparn > UI > Revert UI Overrides In Open Scenes.", target);
        }
        return 1;
    }

    // Returns the path of the closest UI prefab the object sits inside, or null when it is not inside one.
    private static string FindUIPrefabPath(Object target)
    {
        Transform current = null;
        if (target is GameObject targetObject)
        {
            current = targetObject.transform;
        }
        else if (target is Component targetComponent)
        {
            current = targetComponent.transform;
        }

        while (current != null)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(current.gameObject))
            {
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(current.gameObject);
                if (path.StartsWith(UIPrefabTools.UIFolder + "/"))
                {
                    return path;
                }
            }
            current = current.parent;
        }
        return null;
    }
}
