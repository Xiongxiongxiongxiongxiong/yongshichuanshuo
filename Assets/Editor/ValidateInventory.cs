using System;
using UnityEditor;
using UnityEngine;

/// <summary>Focused invariant checks; restores the player's save even when an assertion fails.</summary>
public static class ValidateInventory
{
    [MenuItem("Tools/Inventory/Validate Data and Persistence")]
    public static void Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Run data checks outside Play Mode.");
        bool hadSave = PlayerPrefs.HasKey(PlayerInventory.SaveKey);
        string original = PlayerPrefs.GetString(PlayerInventory.SaveKey, "");
        var item = ScriptableObject.CreateInstance<InventoryItemDefinition>();
        item.id = "validation-only"; item.displayName = "临时验证物品"; item.maxStack = 5;
        var go = new GameObject("Inventory validation only");
        try
        {
            int assertions = 0;
            Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); assertions++; };
            var source = new InventoryCollection(); var target = new InventoryCollection();
            check(source.TryAdd("a", 8, 5) && source.Count == 2, "Stack splitting");
            check(!source.TryAdd("a", -1, 5), "Reject negative add");
            check(!source.TransferTo(target, 0, 0, 5), "Reject zero transfer");
            check(source.TransferTo(target, 0, 5, 5) && source.At(0).quantity == 3 && target.At(0).quantity == 5, "Conserve transferred quantity");
            var full = new InventoryCollection();
            check(full.TryAdd("b", 100, 5) && full.Count == 20, "Fill container");
            check(!source.TransferTo(full, 0, 3, 5) && source.At(0).quantity == 3 && full.Count == 20, "Full destination is atomic");
            check(full.Remove(0, 2) && full.TryAdd("b", 2, 5) && full.Count == 20, "Merge without free slot");
            check(!source.Remove(0, 4) && source.At(0).quantity == 3, "Reject overdraw");
            var data = go.AddComponent<PlayerInventory>();
            data.itemDefinitions = new[] { item };
            check(data.Backpack.Count == 0 && data.Warehouse.Count == 0, "No starter items");
            check(data.TryAdd(item.id, 12), "Future item definition integration");
            string message;
            check(data.Transfer(true, 0, out message), "Deposit");
            check(data.Backpack.Count == 2 && data.Warehouse.At(0).quantity == 5, "Deposit conservation");
            check(data.Load() && data.Warehouse.At(0).quantity == 5 && data.Backpack.At(0).quantity == 5, "Save reload");
            check(data.Transfer(false, 0, out message) && data.Warehouse.Count == 0, "Withdraw");
            int total = 0; foreach (var stack in data.Backpack.stacks) total += stack.quantity;
            check(total == 12, "Round trip conservation");
            check(!data.TryAdd("unknown", 1), "Reject undefined item");
            Debug.Log("Inventory validation passed: " + assertions + " assertions. Original save restored.");
        }
        finally
        {
            if (hadSave) PlayerPrefs.SetString(PlayerInventory.SaveKey, original);
            else PlayerPrefs.DeleteKey(PlayerInventory.SaveKey);
            PlayerPrefs.Save();
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(item);
        }
    }
}
