// ScreenSwitcherTests
// EditMode tests for ScreenSwitcher: which screens are open after Open, Back and
// CloseCurrent, pop-ups that open on top, the events, and two switchers side by side.
// (Screens here have no DOTween Animation, so every switch happens at once.)
// Put this file in: Assets/_Project/Tests/Editor/
// Run with: Window > General > Test Runner > EditMode > Run All.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class ScreenSwitcherTests
{
    // Everything a test makes, so it can be removed again afterwards.
    private readonly List<Object> created = new List<Object>();

    [TearDown]
    public void RemoveTestObjects()
    {
        foreach (Object testObject in created)
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        created.Clear();
    }

    // ---------- Helpers ----------

    private GameObject MakeScreen(string screenName)
    {
        GameObject screen = new GameObject(screenName + " (test)");
        screen.SetActive(false);
        created.Add(screen);
        return screen;
    }

    // A switcher on its own object, with these screens in its list (and which of them open on top).
    private ScreenSwitcher MakeSwitcher(GameObject[] screenObjects, bool[] openOnTop)
    {
        GameObject switcherObject = new GameObject("Screens (test)");
        created.Add(switcherObject);
        return AddSwitcher(switcherObject, screenObjects, openOnTop);
    }

    private ScreenSwitcher AddSwitcher(GameObject owner, GameObject[] screenObjects, bool[] openOnTop)
    {
        ScreenSwitcher switcher = owner.AddComponent<ScreenSwitcher>();
        SerializedObject serialized = new SerializedObject(switcher);
        SerializedProperty list = serialized.FindProperty("screens");
        list.arraySize = screenObjects.Length;
        for (int i = 0; i < screenObjects.Length; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("screen").objectReferenceValue = screenObjects[i];
            element.FindPropertyRelative("openOnTop").boolValue = openOnTop[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return switcher;
    }

    // ---------- Open ----------

    [Test]
    public void Open_FirstScreen_ShowsIt()
    {
        GameObject main = MakeScreen("Main");
        ScreenSwitcher switcher = MakeSwitcher(new[] { main }, new[] { false });

        switcher.Open(main);

        Assert.IsTrue(main.activeSelf);
        Assert.AreSame(main, switcher.CurrentScreen);
    }

    [Test]
    public void Open_AnotherScreen_HidesTheCurrentOne()
    {
        GameObject main = MakeScreen("Main");
        GameObject settings = MakeScreen("Settings");
        ScreenSwitcher switcher = MakeSwitcher(new[] { main, settings }, new[] { false, false });
        switcher.Open(main);

        switcher.Open(settings);

        Assert.IsFalse(main.activeSelf);
        Assert.IsTrue(settings.activeSelf);
        Assert.AreSame(settings, switcher.CurrentScreen);
    }

    [Test]
    public void Open_OnTopScreen_KeepsTheCurrentOneVisible()
    {
        GameObject main = MakeScreen("Main");
        GameObject popup = MakeScreen("Popup");
        ScreenSwitcher switcher = MakeSwitcher(new[] { main, popup }, new[] { false, true });
        switcher.Open(main);

        switcher.Open(popup);

        Assert.IsTrue(main.activeSelf);
        Assert.IsTrue(popup.activeSelf);
    }

    [Test]
    public void Open_ScreenAlreadyInTheHistory_GoesBackToIt()
    {
        GameObject main = MakeScreen("Main");
        GameObject settings = MakeScreen("Settings");
        GameObject credits = MakeScreen("Credits");
        ScreenSwitcher switcher = MakeSwitcher(new[] { main, settings, credits }, new[] { false, false, false });
        switcher.Open(main);
        switcher.Open(settings);
        switcher.Open(credits);

        switcher.Open(main);
        switcher.Back();

        Assert.IsTrue(main.activeSelf);
        Assert.IsFalse(settings.activeSelf);
        Assert.IsFalse(credits.activeSelf);
        Assert.AreSame(main, switcher.CurrentScreen);
    }

    [Test]
    public void Open_ScreenNotInTheList_LogsAnError_AndChangesNothing()
    {
        GameObject main = MakeScreen("Main");
        GameObject stranger = MakeScreen("Stranger");
        ScreenSwitcher switcher = MakeSwitcher(new[] { main }, new[] { false });
        switcher.Open(main);
        LogAssert.Expect(LogType.Error, new Regex("is not in Screens"));

        switcher.Open(stranger);

        Assert.IsTrue(main.activeSelf);
        Assert.IsFalse(stranger.activeSelf);
        Assert.AreSame(main, switcher.CurrentScreen);
    }

    [Test]
    public void Open_TheCurrentScreenAgain_DoesNotOpenItTwice()
    {
        GameObject main = MakeScreen("Main");
        ScreenSwitcher switcher = MakeSwitcher(new[] { main }, new[] { false });
        int opened = 0;
        switcher.Screens[0].onOpened.AddListener(() => opened++);
        switcher.Open(main);

        switcher.Open(main);

        Assert.AreEqual(1, opened);
    }

    // ---------- Back and CloseCurrent ----------

    [Test]
    public void Back_ReturnsToThePreviousScreen()
    {
        GameObject main = MakeScreen("Main");
        GameObject settings = MakeScreen("Settings");
        ScreenSwitcher switcher = MakeSwitcher(new[] { main, settings }, new[] { false, false });
        switcher.Open(main);
        switcher.Open(settings);

        switcher.Back();

        Assert.IsTrue(main.activeSelf);
        Assert.IsFalse(settings.activeSelf);
        Assert.AreSame(main, switcher.CurrentScreen);
    }

    [Test]
    public void Back_OnTheFirstScreen_DoesNothing()
    {
        GameObject main = MakeScreen("Main");
        ScreenSwitcher switcher = MakeSwitcher(new[] { main }, new[] { false });
        switcher.Open(main);

        switcher.Back();

        Assert.IsTrue(main.activeSelf);
        Assert.AreSame(main, switcher.CurrentScreen);
    }

    [Test]
    public void Back_FromAnOnTopScreen_LeavesTheScreenBelowAsItWas()
    {
        GameObject main = MakeScreen("Main");
        GameObject popup = MakeScreen("Popup");
        ScreenSwitcher switcher = MakeSwitcher(new[] { main, popup }, new[] { false, true });
        int mainOpened = 0;
        switcher.Screens[0].onOpened.AddListener(() => mainOpened++);
        switcher.Open(main);
        switcher.Open(popup);

        switcher.Back();

        Assert.IsTrue(main.activeSelf);
        Assert.IsFalse(popup.activeSelf);
        Assert.AreEqual(1, mainOpened);
    }

    [Test]
    public void CloseCurrent_OnTheFirstScreen_ClosesIt()
    {
        GameObject main = MakeScreen("Main");
        ScreenSwitcher switcher = MakeSwitcher(new[] { main }, new[] { false });
        switcher.Open(main);

        switcher.CloseCurrent();

        Assert.IsFalse(main.activeSelf);
        Assert.IsNull(switcher.CurrentScreen);
    }

    // ---------- Events ----------

    [Test]
    public void Events_FireOncePerOpenAndOncePerClose()
    {
        GameObject main = MakeScreen("Main");
        GameObject settings = MakeScreen("Settings");
        ScreenSwitcher switcher = MakeSwitcher(new[] { main, settings }, new[] { false, false });
        int opened = 0;
        int closed = 0;
        switcher.Screens[1].onOpened.AddListener(() => opened++);
        switcher.Screens[1].onClosed.AddListener(() => closed++);
        switcher.Open(main);

        switcher.Open(settings);
        switcher.Back();

        Assert.AreEqual(1, opened);
        Assert.AreEqual(1, closed);
    }

    // ---------- Two switchers ----------

    // The main menu switcher and the tab switcher inside Settings must not touch each other's screens.
    [Test]
    public void TwoSwitchers_MainMenuAndSettingsTabs_DoNotAffectEachOther()
    {
        GameObject main = MakeScreen("Main");
        GameObject settings = MakeScreen("Settings");
        GameObject general = MakeScreen("General");
        GameObject audio = MakeScreen("Audio");
        general.transform.SetParent(settings.transform);
        audio.transform.SetParent(settings.transform);
        ScreenSwitcher menu = MakeSwitcher(new[] { main, settings }, new[] { false, false });
        ScreenSwitcher tabs = AddSwitcher(settings, new[] { general, audio }, new[] { false, false });
        menu.Open(main);
        menu.Open(settings);
        tabs.Open(general);

        tabs.Open(audio);
        menu.Back();

        Assert.IsTrue(main.activeSelf);
        Assert.IsFalse(settings.activeSelf);
        Assert.IsFalse(general.activeSelf);
        Assert.IsTrue(audio.activeSelf);
        Assert.AreSame(audio, tabs.CurrentScreen);
    }
}
