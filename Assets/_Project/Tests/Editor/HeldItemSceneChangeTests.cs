// HeldItemSceneChangeTests
// Integration test: an item held in one scene is still held after going to another scene,
// and the small hand slot on screen still shows it.
// It is an Edit Mode test that enters Play Mode by itself: this Unity version cannot compile
// Play Mode tests in the game's own assembly (there is no "Enable playmode tests for all
// assemblies" option any more), and the game code has no asmdef to point a test assembly at.
// Put this file in: Assets/_Project/Tests/Editor/
// Needs: the scenes Test_Zones and Test_25D in the Scene List (File > Build Profiles).
// Run with: Window > General > Test Runner > EditMode > Run All.
// Careful: changing scene autosaves, so this replaces the saved game on this computer.

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class HeldItemSceneChangeTests
{
    // Leaves Play Mode again even when an assert failed half way.
    [UnityTearDown]
    public IEnumerator LeavePlayMode()
    {
        if (Application.isPlaying)
        {
            yield return new ExitPlayMode();
        }
    }

    // Finds the key pickup of Test_Zones even when it is switched off.
    private static PickupItem FindKey()
    {
        foreach (PickupItem pickup in Object.FindObjectsByType<PickupItem>(FindObjectsInactive.Include))
        {
            if (pickup.name == "Key")
            {
                return pickup;
            }
        }
        return null;
    }

    // The Image of the small hand slot next to the bag button.
    private static Image FindHandSlotOnScreen()
    {
        foreach (HeldItemDisplay display in Object.FindObjectsByType<HeldItemDisplay>(FindObjectsInactive.Include))
        {
            if (display.transform.parent != null && display.transform.parent.name == "HandSlotHud")
            {
                return display.GetComponent<Image>();
            }
        }
        return null;
    }

    [UnityTest]
    public IEnumerator HeldItem_SurvivesSceneChange_AndHandSlotStillShowsIt()
    {
        // Entering Play Mode reloads the scripts, so nothing is kept from before this line.
        yield return new EnterPlayMode();
        yield return SceneManager.LoadSceneAsync("Test_Zones");
        yield return null;
        yield return null;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Inventory inventory = GameManager.Instance.Inventory;
        PickupItem key = FindKey();
        Assert.IsNotNull(key, "There is no pickup named 'Key' in Test_Zones.");
        Assert.IsTrue(key.gameObject.activeInHierarchy, "The key was already taken before the test started.");
        player.GetComponent<PlayerMovement>().TeleportTo(key.transform.position + new Vector3(-0.8f, 0.08f, -0.8f));
        yield return null;
        key.Interact(new PlayerContext(player.transform, inventory, player.GetComponent<PlayerHealth>()));
        yield return null;
        Assert.AreEqual(1, inventory.Slots.Count, "Picking up the key should leave exactly one stack in the bag.");
        ItemData keyItem = inventory.Slots[0].Item;
        Assert.IsTrue(inventory.Hold(keyItem), "The key is in the bag, so holding it must work.");

        GameManager.Instance.SceneLoader.LoadScene("Test_25D", "");
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Test_25D");
        yield return null;
        yield return null;

        Assert.AreSame(keyItem, GameManager.Instance.Inventory.HeldItem);
        Image handSlot = FindHandSlotOnScreen();
        Assert.IsNotNull(handSlot, "The small hand slot (InventoryHud/HandSlotHud/Icon) was not found.");
        Assert.IsTrue(handSlot.enabled);
        Assert.AreSame(keyItem.Icon, handSlot.sprite);
    }
}
