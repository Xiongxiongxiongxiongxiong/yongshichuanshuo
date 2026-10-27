using System;
using System.Collections.Generic;

/// <summary>Bounded stacks. Transfers preflight the complete quantity before either side changes.</summary>
[Serializable]
public sealed class InventoryCollection
{
    [Serializable]
    public sealed class Stack
    {
        public string itemId;
        public int quantity;
        public Stack(string id, int count) { itemId = id; quantity = count; }
    }

    public const int Capacity = 20;
    public List<Stack> stacks = new List<Stack>();
    public int Count => stacks.Count;
    public Stack At(int index) => index >= 0 && index < stacks.Count ? stacks[index] : null;

    public bool CanAdd(string id, int amount, int limit)
    {
        if (string.IsNullOrWhiteSpace(id) || amount <= 0 || limit <= 0) return false;
        long space = (long)(Capacity - stacks.Count) * limit;
        foreach (var stack in stacks)
            if (stack.itemId == id) space += Math.Max(0, limit - stack.quantity);
        return space >= amount;
    }

    public bool TryAdd(string id, int amount, int limit)
    {
        if (!CanAdd(id, amount, limit)) return false;
        foreach (var stack in stacks)
        {
            if (stack.itemId != id || stack.quantity >= limit) continue;
            int add = Math.Min(amount, limit - stack.quantity);
            stack.quantity += add;
            amount -= add;
            if (amount == 0) return true;
        }
        while (amount > 0)
        {
            int add = Math.Min(amount, limit);
            stacks.Add(new Stack(id, add));
            amount -= add;
        }
        return true;
    }

    public bool Remove(int index, int amount)
    {
        var stack = At(index);
        if (stack == null || amount <= 0 || amount > stack.quantity) return false;
        stack.quantity -= amount;
        if (stack.quantity == 0) stacks.RemoveAt(index);
        return true;
    }

    public bool TransferTo(InventoryCollection destination, int index, int amount, int limit)
    {
        var stack = At(index);
        if (destination == null || destination == this || stack == null || amount <= 0 || amount > stack.quantity)
            return false;
        if (!destination.TryAdd(stack.itemId, amount, limit)) return false;
        Remove(index, amount);
        return true;
    }
}
