namespace InventoryUI
{
    internal sealed class HotbarStack
    {
        public string Id { get; }
        public int Quantity { get; }

        public HotbarStack(string id, int quantity)
        {
            Id = id;
            Quantity = quantity;
        }
    }

    // Slot allocations belong to the UI. The framework only sees total counts.
    internal sealed class HotbarSlots
    {
        public const int Count = 9;
        private readonly HotbarStack?[] _items = new HotbarStack?[Count];
        private readonly Dictionary<string, int> _knownQuantities = new(StringComparer.Ordinal);
        private HotbarStack?[]? _beforePickup;
        private int _heldOwned;
        private int _heldPending;
        private int _preferredSlot;

        public HotbarStack? this[int index] => _items[index];
        public string? HeldItem { get; private set; }
        public int HeldQuantity => _heldOwned + _heldPending;
        public int PendingQuantity => _heldPending;

        public void Reconcile(IEnumerable<KeyValuePair<string, int>> inventory, int preferredSlot = 0)
        {
            _preferredSlot = preferredSlot >= 0 && preferredSlot < Count ? preferredSlot : 0;
            var current = inventory.Where(item => item.Value > 0)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            foreach (var previous in _knownQuantities)
            {
                current.TryGetValue(previous.Key, out int quantity);
                int removed = previous.Value - quantity;
                if (removed <= 0) continue;
                // Consumption comes out of the selected stack first, then
                // unassigned inventory, then other allocations of this item.
                int reserve = Math.Max(0, previous.Value - AllocatedQuantity(previous.Key));
                RemoveFromSlot(_items, _preferredSlot, previous.Key, ref removed);
                removed -= Math.Min(removed, reserve);
                for (int i = 0; i < Count; i++)
                    RemoveFromSlot(_items, i, previous.Key, ref removed);
                if (HeldItem == previous.Key)
                    _heldOwned -= Math.Min(_heldOwned, removed);
            }
            foreach (var item in current)
            {
                _knownQuantities.TryGetValue(item.Key, out int previous);
                int added = item.Value - previous;
                if (added <= 0) continue;
                AddToSlots(_items, item.Key, added);
                if (_beforePickup != null) AddToSlots(_beforePickup, item.Key, added);
            }
            _knownQuantities.Clear();
            foreach (var item in current) _knownQuantities.Add(item.Key, item.Value);
            if (HeldItem != null && HeldQuantity == 0) Cancel();
        }

        public void SelectCatalogItem(string id)
        {
            if (HeldItem != id)
            {
                Cancel();
                HeldItem = id;
            }
            // Catalog clicks add to the cursor stack, never to an existing slot.
            int next = checked(HeldQuantity + 1);
            _heldPending = next - _heldOwned;
        }

        public void Click(int index, Action<string, int> addCatalogItems)
        {
            if (index < 0 || index >= Count) return;
            if (HeldItem == null)
            {
                HotbarStack? picked = _items[index];
                if (picked == null) return;
                _beforePickup = (HotbarStack?[])_items.Clone();
                HeldItem = picked.Id;
                _heldOwned = picked.Quantity;
                _items[index] = null;
                return;
            }

            string id = HeldItem;
            HotbarStack? target = _items[index];
            int placedQuantity = checked(HeldQuantity + (target?.Id == id ? target.Quantity : 0));
            bool addedItems = _heldPending > 0;
            if (addedItems)
            {
                _knownQuantities.TryGetValue(id, out int known);
                int newTotal = checked(known + _heldPending);
                // The UI suppresses refresh during this callback. A failure
                // leaves both the held stack and its destination unchanged.
                addCatalogItems(id, _heldPending);
                _knownQuantities[id] = newTotal;
            }
            _items[index] = new HotbarStack(id, placedQuantity);
            if (target != null && target.Id != id)
            {
                HeldItem = target.Id;
                _heldOwned = target.Quantity;
                _heldPending = 0;
                if (addedItems) _beforePickup = (HotbarStack?[])_items.Clone();
            }
            else ClearPickup();
        }

        public void DiscardHeld(Action<string, int> removeInventoryItems)
        {
            if (HeldItem == null) return;
            if (_heldOwned > 0)
            {
                removeInventoryItems(HeldItem, _heldOwned);
                _knownQuantities[HeldItem] -= _heldOwned;
            }
            // Pending catalog items have never entered the inventory.
            ClearPickup();
        }

        public void Cancel()
        {
            if (_beforePickup != null) Array.Copy(_beforePickup, _items, Count);
            ClearPickup();
            // Inventory may have changed while carrying a stack. A rollback
            // must not resurrect anything consumed or removed externally.
            foreach (string id in _items.Where(item => item != null).Select(item => item!.Id).Distinct().ToList())
            {
                _knownQuantities.TryGetValue(id, out int available);
                int excess = Math.Max(0, AllocatedQuantity(id) - available);
                RemoveFromSlot(_items, _preferredSlot, id, ref excess);
                for (int i = 0; i < Count; i++) RemoveFromSlot(_items, i, id, ref excess);
            }
        }

        private void ClearPickup()
        {
            HeldItem = null;
            _heldOwned = 0;
            _heldPending = 0;
            _beforePickup = null;
        }

        private int AllocatedQuantity(string id) =>
            _items.Where(item => item?.Id == id).Sum(item => item!.Quantity) + (HeldItem == id ? _heldOwned : 0);

        private void AddToSlots(HotbarStack?[] slots, string id, int quantity)
        {
            int index = slots[_preferredSlot]?.Id == id ? _preferredSlot : Array.FindIndex(slots, item => item?.Id == id);
            if (index < 0) index = Array.IndexOf(slots, null);
            if (index >= 0) slots[index] = new HotbarStack(id, checked((slots[index]?.Quantity ?? 0) + quantity));
        }

        private static void RemoveFromSlot(HotbarStack?[] slots, int index, string id, ref int quantity)
        {
            HotbarStack? stack = slots[index];
            if (quantity <= 0 || stack?.Id != id) return;
            int removed = Math.Min(stack.Quantity, quantity);
            int remaining = stack.Quantity - removed;
            slots[index] = remaining > 0 ? new HotbarStack(id, remaining) : null;
            quantity -= removed;
        }
    }
}
