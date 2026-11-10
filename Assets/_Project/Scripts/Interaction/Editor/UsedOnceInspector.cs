// UsedOnceInspector
// Hides the One Shot box in the Inspector of pickups and containers. Those are always
// used only once, so an unticked One Shot box there would say something that is not true.
// Everything else in their Inspector is drawn the normal way.
// To hide the box on another script that is always used only once, add one more small
// class at the bottom of this file, like the two that are there.
//
// This script only runs in the Unity Editor (it lives in an Editor folder).
// Put this on: nothing. Unity uses it by itself.

using UnityEditor;

public abstract class UsedOnceInspector : Editor
{
    // Draws the normal Inspector without the One Shot box.
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // The Script line at the top, greyed out like in every other Inspector.
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        }

        DrawPropertiesExcluding(serializedObject, "m_Script", "oneShot");

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(PickupItem))]
[CanEditMultipleObjects]
public class PickupItemInspector : UsedOnceInspector
{
}

[CustomEditor(typeof(ContainerInteractable))]
[CanEditMultipleObjects]
public class ContainerInteractableInspector : UsedOnceInspector
{
}
