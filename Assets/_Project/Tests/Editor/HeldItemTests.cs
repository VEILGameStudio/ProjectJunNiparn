// HeldItemTests
// EditMode tests for: holding an item, what the bag draws while something is held, the
// "must be held" check of interactables, and HeldItemCheck.
// Put this file in: Assets/_Project/Tests/Editor/
// Run with: Window > General > Test Runner > EditMode > Run All.

using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class HeldItemTests
{
    // Everything a test makes, so it can be removed again afterwards.
    private readonly List<Object> created = new List<Object>();

    // What GameEvents.OnHeldItemChanged told us during the test.
    private int heldEvents;
    private ItemData lastHeldItem;

    [SetUp]
    public void StartListening()
    {
        heldEvents = 0;
        lastHeldItem = null;
        GameEvents.OnHeldItemChanged += RememberHeldEvent;
    }

    [TearDown]
    public void StopListeningAndRemoveTestObjects()
    {
        GameEvents.OnHeldItemChanged -= RememberHeldEvent;
        foreach (Object testObject in created)
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        created.Clear();
    }

    private void RememberHeldEvent(ItemData item)
    {
        heldEvents++;
        lastHeldItem = item;
    }

    // ---------- Helpers ----------

    private ItemData MakeItem(string id, int maxStack)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        SerializedObject serialized = new SerializedObject(item);
        serialized.FindProperty("id").stringValue = id;
        serialized.FindProperty("maxStack").intValue = maxStack;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        created.Add(item);
        return item;
    }

    private Inventory MakeInventory()
    {
        GameObject inventoryObject = new GameObject("Inventory (test)");
        created.Add(inventoryObject);
        return inventoryObject.AddComponent<Inventory>();
    }

    // An inventory that knows these items, so it can load a save.
    private Inventory MakeInventoryThatKnows(params ItemData[] items)
    {
        ItemDatabase database = ScriptableObject.CreateInstance<ItemDatabase>();
        created.Add(database);
        SerializedObject serializedDatabase = new SerializedObject(database);
        SerializedProperty list = serializedDatabase.FindProperty("items");
        list.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
        serializedDatabase.ApplyModifiedPropertiesWithoutUndo();

        Inventory inventory = MakeInventory();
        SerializedObject serializedInventory = new SerializedObject(inventory);
        serializedInventory.FindProperty("itemDatabase").objectReferenceValue = database;
        serializedInventory.ApplyModifiedPropertiesWithoutUndo();
        return inventory;
    }

    private HeldItemCheck MakeCheck(ItemData requiredItem, bool useUpItem)
    {
        GameObject checkObject = new GameObject("HeldItemCheck (test)");
        created.Add(checkObject);
        HeldItemCheck check = checkObject.AddComponent<HeldItemCheck>();
        SerializedObject serialized = new SerializedObject(check);
        serialized.FindProperty("requiredItem").objectReferenceValue = requiredItem;
        serialized.FindProperty("useUpItem").boolValue = useUpItem;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return check;
    }

    // ---------- Holding ----------

    [Test]
    public void Hold_ItemThatIsNotInTheBag_IsRefused()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);

        bool held = inventory.Hold(key);

        Assert.IsFalse(held);
        Assert.IsNull(inventory.HeldItem);
    }

    [Test]
    public void Hold_ItemInTheBag_IsHoldingThatItemOnly()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        ItemData gear = MakeItem("gear", 1);
        inventory.Add(key);
        inventory.Add(gear);

        bool held = inventory.Hold(key);

        Assert.IsTrue(held);
        Assert.IsTrue(inventory.IsHolding(key));
        Assert.IsFalse(inventory.IsHolding(gear));
        Assert.IsFalse(inventory.IsHolding(null));
    }

    [Test]
    public void Hold_DoesNotTakeTheItemOutOfTheBag()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        inventory.Add(key);

        inventory.Hold(key);

        Assert.AreEqual(1, inventory.Count(key));
        Assert.IsTrue(inventory.Has(key));
    }

    [Test]
    public void Hold_AnotherItem_ReplacesTheFirst()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        ItemData gear = MakeItem("gear", 1);
        inventory.Add(key);
        inventory.Add(gear);
        inventory.Hold(key);

        inventory.Hold(gear);

        Assert.AreSame(gear, inventory.HeldItem);
        Assert.AreEqual(1, inventory.Count(key));
    }

    [Test]
    public void StopHolding_EmptiesTheHand_AndKeepsTheItem()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        inventory.Add(key);
        inventory.Hold(key);

        inventory.StopHolding();

        Assert.IsNull(inventory.HeldItem);
        Assert.AreEqual(1, inventory.Count(key));
    }

    [Test]
    public void Remove_TheLastHeldItem_EmptiesTheHand()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        inventory.Add(key);
        inventory.Hold(key);

        inventory.Remove(key);

        Assert.IsNull(inventory.HeldItem);
    }

    [Test]
    public void Remove_OneOfSeveralHeldItems_KeepsHolding()
    {
        Inventory inventory = MakeInventory();
        ItemData potion = MakeItem("potion", 5);
        inventory.Add(potion, 3);
        inventory.Hold(potion);

        inventory.Remove(potion);

        Assert.AreSame(potion, inventory.HeldItem);
        Assert.AreEqual(2, inventory.Count(potion));
    }

    [Test]
    public void HeldItemChanged_IsRaisedOncePerChange()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        inventory.Add(key);

        inventory.Hold(key);
        int eventsAfterHold = heldEvents;
        ItemData itemAfterHold = lastHeldItem;
        inventory.StopHolding();

        Assert.AreEqual(1, eventsAfterHold);
        Assert.AreSame(key, itemAfterHold);
        Assert.AreEqual(2, heldEvents);
        Assert.IsNull(lastHeldItem);
    }

    [Test]
    public void HeldItemChanged_IsNotRaised_WhenHoldingTheSameItemAgain()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        inventory.Add(key);
        inventory.Hold(key);

        inventory.Hold(key);

        Assert.AreEqual(1, heldEvents);
    }

    [Test]
    public void HeldItemChanged_IsNotRaised_WhenNothingChanges()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);

        inventory.Hold(key);        // refused: not in the bag
        inventory.StopHolding();    // the hand was already empty

        Assert.AreEqual(0, heldEvents);
    }

    [Test]
    public void HeldItemChanged_AfterUnsubscribing_IsNotReceived()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        inventory.Add(key);
        GameEvents.OnHeldItemChanged -= RememberHeldEvent;

        inventory.Hold(key);

        Assert.AreEqual(0, heldEvents);
    }

    [Test]
    public void SaveAndLoad_KeepsTheHeldItem()
    {
        ItemData key = MakeItem("key", 1);
        ItemData gear = MakeItem("gear", 1);
        Inventory saved = MakeInventoryThatKnows(key, gear);
        saved.Add(key);
        saved.Add(gear);
        saved.Hold(gear);
        GameSaveData data = new GameSaveData();
        saved.CaptureState(data);
        Inventory loaded = MakeInventoryThatKnows(key, gear);

        loaded.RestoreState(JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data)));

        Assert.AreSame(gear, loaded.HeldItem);
        Assert.AreEqual(1, loaded.Count(key));
    }

    [Test]
    public void Load_SaveMadeBeforeHoldingExisted_LeavesTheHandEmpty()
    {
        ItemData key = MakeItem("key", 1);
        Inventory loaded = MakeInventoryThatKnows(key);
        string oldSave = "{\"sceneName\":\"Test\",\"inventoryEntries\":[{\"itemId\":\"key\",\"count\":1}]}";

        loaded.RestoreState(JsonUtility.FromJson<GameSaveData>(oldSave));

        Assert.IsNull(loaded.HeldItem);
        Assert.AreEqual(1, loaded.Count(key));
    }

    [Test]
    public void Load_HeldItemThatIsNotInTheBag_LeavesTheHandEmpty()
    {
        ItemData key = MakeItem("key", 1);
        Inventory loaded = MakeInventoryThatKnows(key);
        GameSaveData data = new GameSaveData();
        data.heldItemId = "key";

        loaded.RestoreState(data);

        Assert.IsNull(loaded.HeldItem);
    }

    // ---------- What the bag draws ----------

    [Test]
    public void Bag_HoldingOneOfThree_ShowsTwo()
    {
        ItemData potion = MakeItem("potion", 5);
        List<InventorySlot> stacks = new List<InventorySlot> { new InventorySlot(potion, 3) };

        List<InventorySlot> bag = InventoryUI.BagStacksWithoutHeld(stacks, potion);

        Assert.AreEqual(1, bag.Count);
        Assert.AreEqual(2, bag[0].Count);
    }

    [Test]
    public void Bag_HoldingTheOnlyOne_ShowsNoStackOfIt()
    {
        ItemData key = MakeItem("key", 1);
        ItemData gear = MakeItem("gear", 1);
        List<InventorySlot> stacks = new List<InventorySlot> { new InventorySlot(key, 1), new InventorySlot(gear, 1) };

        List<InventorySlot> bag = InventoryUI.BagStacksWithoutHeld(stacks, key);

        Assert.AreEqual(1, bag.Count);
        Assert.AreSame(gear, bag[0].Item);
    }

    [Test]
    public void Bag_HoldingFromTwoStacks_TakesFromTheLastStack()
    {
        ItemData potion = MakeItem("potion", 5);
        List<InventorySlot> stacks = new List<InventorySlot> { new InventorySlot(potion, 5), new InventorySlot(potion, 1) };

        List<InventorySlot> bag = InventoryUI.BagStacksWithoutHeld(stacks, potion);

        Assert.AreEqual(1, bag.Count);
        Assert.AreEqual(5, bag[0].Count);
    }

    [Test]
    public void Bag_NothingHeld_ShowsEveryStack()
    {
        ItemData potion = MakeItem("potion", 5);
        List<InventorySlot> stacks = new List<InventorySlot> { new InventorySlot(potion, 5), new InventorySlot(potion, 1) };

        List<InventorySlot> bag = InventoryUI.BagStacksWithoutHeld(stacks, null);

        Assert.AreEqual(2, bag.Count);
        Assert.AreEqual(5, bag[0].Count);
        Assert.AreEqual(1, bag[1].Count);
    }

    [Test]
    public void Bag_HeldItemNotInAnyStack_ShowsEveryStack_AndDoesNotThrow()
    {
        ItemData potion = MakeItem("potion", 5);
        ItemData ghost = MakeItem("ghost", 1);
        List<InventorySlot> stacks = new List<InventorySlot> { new InventorySlot(potion, 3) };

        List<InventorySlot> bag = null;
        Assert.DoesNotThrow(() => bag = InventoryUI.BagStacksWithoutHeld(stacks, ghost));

        Assert.AreEqual(1, bag.Count);
        Assert.AreEqual(3, bag[0].Count);
    }

    [Test]
    public void Bag_NoStacksAtAll_IsEmpty_AndDoesNotThrow()
    {
        ItemData key = MakeItem("key", 1);

        List<InventorySlot> bag = null;
        Assert.DoesNotThrow(() => bag = InventoryUI.BagStacksWithoutHeld(null, key));

        Assert.AreEqual(0, bag.Count);
    }

    [Test]
    public void Bag_NeverChangesTheRealStacks()
    {
        ItemData potion = MakeItem("potion", 5);
        List<InventorySlot> stacks = new List<InventorySlot> { new InventorySlot(potion, 3) };

        List<InventorySlot> bag = InventoryUI.BagStacksWithoutHeld(stacks, potion);

        Assert.AreEqual(3, stacks[0].Count);
        Assert.AreNotSame(stacks[0], bag[0]);
    }

    [Test]
    public void Cells_EmptyBag_DrawsTheMinimum()
    {
        Assert.AreEqual(25, InventoryUI.CountCells(0, 5, 25));
    }

    [Test]
    public void Cells_ExactlyTheMinimum_AddsNothing()
    {
        Assert.AreEqual(25, InventoryUI.CountCells(25, 5, 25));
    }

    [Test]
    public void Cells_OneMoreThanTheMinimum_AddsAWholeRow()
    {
        Assert.AreEqual(30, InventoryUI.CountCells(26, 5, 25));
    }

    // ---------- "Must Be Held" on interactables ----------

    [Test]
    public void RequiredItem_None_IsReady()
    {
        Inventory inventory = MakeInventory();

        RequiredItemResult result = InteractableBase.CheckRequiredItem(inventory, null, true);

        Assert.AreEqual(RequiredItemResult.Ready, result);
    }

    [Test]
    public void RequiredItem_NotInTheBag_IsMissing()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);

        RequiredItemResult result = InteractableBase.CheckRequiredItem(inventory, key, true);

        Assert.AreEqual(RequiredItemResult.Missing, result);
    }

    [Test]
    public void RequiredItem_InTheBag_WhenHoldingIsNotAsked_IsReady()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        inventory.Add(key);

        RequiredItemResult result = InteractableBase.CheckRequiredItem(inventory, key, false);

        Assert.AreEqual(RequiredItemResult.Ready, result);
    }

    [Test]
    public void RequiredItem_InTheBagButNotHeld_IsNotHeld()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        inventory.Add(key);

        RequiredItemResult result = InteractableBase.CheckRequiredItem(inventory, key, true);

        Assert.AreEqual(RequiredItemResult.NotHeld, result);
    }

    [Test]
    public void RequiredItem_Held_IsReady()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        inventory.Add(key);
        inventory.Hold(key);

        RequiredItemResult result = InteractableBase.CheckRequiredItem(inventory, key, true);

        Assert.AreEqual(RequiredItemResult.Ready, result);
    }

    [Test]
    public void RequiredItem_NoInventory_IsMissing()
    {
        ItemData key = MakeItem("key", 1);

        RequiredItemResult result = InteractableBase.CheckRequiredItem(null, key, false);

        Assert.AreEqual(RequiredItemResult.Missing, result);
    }

    // ---------- HeldItemCheck ----------

    [Test]
    public void Check_HoldingTheRightItem_RunsOnCorrectOnce()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        inventory.Add(key);
        inventory.Hold(key);
        HeldItemCheck check = MakeCheck(key, false);
        int correct = 0;
        int wrong = 0;
        check.OnCorrect.AddListener(() => correct++);
        check.OnWrong.AddListener(() => wrong++);

        bool result = check.TryCheck(inventory);

        Assert.IsTrue(result);
        Assert.AreEqual(1, correct);
        Assert.AreEqual(0, wrong);
        Assert.AreEqual(1, inventory.Count(key));
    }

    [Test]
    public void Check_HoldingAnotherItem_RunsOnWrongOnce()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        ItemData gear = MakeItem("gear", 1);
        inventory.Add(key);
        inventory.Add(gear);
        inventory.Hold(gear);
        HeldItemCheck check = MakeCheck(key, true);
        int correct = 0;
        int wrong = 0;
        check.OnCorrect.AddListener(() => correct++);
        check.OnWrong.AddListener(() => wrong++);

        bool result = check.TryCheck(inventory);

        Assert.IsFalse(result);
        Assert.AreEqual(0, correct);
        Assert.AreEqual(1, wrong);
        Assert.AreEqual(1, inventory.Count(key));
        Assert.AreEqual(1, inventory.Count(gear));
    }

    [Test]
    public void Check_ItemInTheBagButNotHeld_RunsOnWrong()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        inventory.Add(key);
        HeldItemCheck check = MakeCheck(key, true);
        int wrong = 0;
        check.OnWrong.AddListener(() => wrong++);

        bool result = check.TryCheck(inventory);

        Assert.IsFalse(result);
        Assert.AreEqual(1, wrong);
        Assert.AreEqual(1, inventory.Count(key));
    }

    [Test]
    public void Check_UseUpItem_TakesOneAndEmptiesTheHand()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        inventory.Add(key);
        inventory.Hold(key);
        HeldItemCheck check = MakeCheck(key, true);

        check.TryCheck(inventory);

        Assert.AreEqual(0, inventory.Count(key));
        Assert.IsNull(inventory.HeldItem);
    }

    [Test]
    public void Check_UseUpItem_TakesTheItemBeforeOnCorrectRuns()
    {
        Inventory inventory = MakeInventory();
        ItemData key = MakeItem("key", 1);
        inventory.Add(key);
        inventory.Hold(key);
        HeldItemCheck check = MakeCheck(key, true);
        int countSeenByOnCorrect = -1;
        check.OnCorrect.AddListener(() => countSeenByOnCorrect = inventory.Count(key));

        check.TryCheck(inventory);

        Assert.AreEqual(0, countSeenByOnCorrect);
    }

    [Test]
    public void Check_NoInventory_RunsOnWrong()
    {
        ItemData key = MakeItem("key", 1);
        HeldItemCheck check = MakeCheck(key, false);
        int wrong = 0;
        check.OnWrong.AddListener(() => wrong++);

        bool result = check.TryCheck(null);

        Assert.IsFalse(result);
        Assert.AreEqual(1, wrong);
    }
}
