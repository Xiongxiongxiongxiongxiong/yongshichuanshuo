using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One save owns both containers; no starter items are granted.</summary>
public sealed class PlayerInventory : MonoBehaviour
{
    public const string SaveKey = "Yongshi.Inventory.v1";
    public InventoryItemDefinition[] itemDefinitions = new InventoryItemDefinition[0];
    public event Action Changed;
    public InventoryCollection Backpack { get; private set; } = new InventoryCollection();
    public InventoryCollection Warehouse { get; private set; } = new InventoryCollection();
    public bool SaveAvailable { get; private set; } = true;
    public string EquippedWeaponId { get; private set; } = "";
    private bool equipmentReady;

    [Serializable] private sealed class SaveData
    {
        public int version = 1;
        public InventoryCollection backpack;
        public InventoryCollection warehouse;
        public string equippedWeaponId;
    }

    private void Awake() { Load(); }
    private void Start() { equipmentReady = true; RestoreEquipment(); }

    private bool HasInBackpack(string id)
    { return Backpack.stacks.Exists(stack => stack.itemId == id && stack.quantity > 0); }

    private void RestoreEquipment()
    {
        var swap = GetComponent<TestWeaponSwap>();
        if (swap == null) return;
        var item = Definition(EquippedWeaponId);
        if (item != null && item.IsWeapon && HasInBackpack(item.id) && swap.EquipFrames(item.weaponFrames)) return;
        EquippedWeaponId = "";
        swap.RestoreDefaultWeapon();
    }

    public bool Equip(int index, out string message)
    {
        message = "无法装备这件物品";
        var stack = Backpack.At(index);
        var item = stack != null ? Definition(stack.itemId) : null;
        var swap = GetComponent<TestWeaponSwap>();
        if (!SaveAvailable || item == null || !item.IsWeapon || swap == null) return false;
        if (EquippedWeaponId == item.id)
        {
            EquippedWeaponId = ""; swap.RestoreDefaultWeapon(); message = "已卸下 " + item.displayName;
        }
        else
        {
            if (!swap.EquipFrames(item.weaponFrames)) { message = "武器动画配置不完整"; return false; }
            EquippedWeaponId = item.id; message = "已装备 " + item.displayName;
        }
        Commit();
        return true;
    }

    public InventoryItemDefinition Definition(string id)
    {
        foreach (var item in itemDefinitions)
            if (item != null && item.id == id) return item;
        return null;
    }

    public bool TryAdd(string id, int amount)
    {
        var item = Definition(id);
        if (!SaveAvailable || item == null || !Backpack.TryAdd(id, amount, Mathf.Max(1, item.maxStack))) return false;
        Commit();
        return true;
    }

    public bool Transfer(bool deposit, int index, out string message)
    {
        message = "请先选择物品";
        if (!SaveAvailable) { message = "存档读取失败，暂时无法修改物品"; return false; }
        var source = deposit ? Backpack : Warehouse;
        var destination = deposit ? Warehouse : Backpack;
        var stack = source.At(index);
        if (stack == null) return false;
        var item = Definition(stack.itemId);
        // Unknown saved items can still be retrieved safely without deleting their data.
        int limit = item != null ? Mathf.Max(1, item.maxStack) : Mathf.Max(1, stack.quantity);
        if (!source.TransferTo(destination, index, stack.quantity, limit))
        { message = deposit ? "仓库空间不足" : "背包空间不足"; return false; }
        message = deposit ? "已存入仓库" : "已取回背包";
        if (!HasInBackpack(EquippedWeaponId)) RestoreEquipment();
        Commit();
        return true;
    }

    public bool Discard(int index)
    {
        if (!SaveAvailable || !Backpack.Remove(index, 1)) return false;
        if (!HasInBackpack(EquippedWeaponId)) RestoreEquipment();
        Commit();
        return true;
    }

    public bool Use(int index, PlayerController player, out string message)
    {
        message = "该物品暂不可使用";
        var stack = Backpack.At(index);
        var item = stack != null ? Definition(stack.itemId) : null;
        if (!SaveAvailable || item == null || player == null || player.isDead || item.restoreAmount <= 0) return false;
        if (item.useEffect == InventoryItemDefinition.UseEffect.RestoreHealth)
        {
            var health = player.GetComponent<Character>();
            if (health.CurrentHp >= health.maxHp) { message = "生命值已满"; return false; }
            health.CurrentHp = Mathf.Min(health.maxHp, health.CurrentHp + item.restoreAmount);
            health.OnHealthChange?.Invoke(health);
        }
        else if (item.useEffect == InventoryItemDefinition.UseEffect.RestoreStamina)
        {
            var stamina = player.GetComponent<PlayerStamina>();
            if (stamina.Current >= stamina.maxStamina) { message = "体力已满"; return false; }
            stamina.Restore(item.restoreAmount);
        }
        else return false;
        Backpack.Remove(index, 1);
        message = "已使用 " + item.displayName;
        Commit();
        return true;
    }

    public void Save()
    {
        if (!SaveAvailable) return;
        var data = new SaveData { backpack = Backpack, warehouse = Warehouse, equippedWeaponId = EquippedWeaponId };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private void Commit() { Save(); Changed?.Invoke(); }

    private static bool Valid(InventoryCollection collection)
    {
        if (collection == null || collection.stacks == null || collection.Count > InventoryCollection.Capacity) return false;
        foreach (var stack in collection.stacks)
            if (stack == null || string.IsNullOrWhiteSpace(stack.itemId) || stack.quantity <= 0) return false;
        return true;
    }

    public bool Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            Backpack = new InventoryCollection();
            Warehouse = new InventoryCollection();
            EquippedWeaponId = "";
            SaveAvailable = true;
            if (equipmentReady) RestoreEquipment();
            Changed?.Invoke();
            return true;
        }
        try
        {
            var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
            if (data == null || data.version != 1 || !Valid(data.backpack) || !Valid(data.warehouse))
                throw new InvalidOperationException("Invalid inventory save");
            Backpack = data.backpack;
            Warehouse = data.warehouse;
            EquippedWeaponId = data.equippedWeaponId ?? "";
            if (equipmentReady) RestoreEquipment();
            SaveAvailable = true;
            Changed?.Invoke();
            return true;
        }
        catch (Exception exception)
        {
            SaveAvailable = false;
            Debug.LogWarning("物品存档未能读取，已保留原存档：" + exception.Message, this);
            return false;
        }
    }
}
