using UnityEngine;
using UnityEngine.UI;
using InventoryFramework;

namespace InventoryUI
{
    public class Hotbar
    {
        public static Hotbar? Instance { get; private set; }

        private const int SlotCount = 9;
        private readonly GameObject[] _slots = new GameObject[SlotCount];
        private int _selectedIndex = 0;

        public event Action<InventoryItem?>? OnSlotSelected;
        public event Action<InventoryItem?>? OnItemUsed;
        public event Action<InventoryItem?>? OnItemUsedAlt;
        public event Action<InventoryItem?>? OnKeyDown;
        public event Action<InventoryItem?>? OnUpdate;

        internal void Use() => OnItemUsed?.Invoke(GetSelectedItem());
        internal void AltUse() => OnItemUsedAlt?.Invoke(GetSelectedItem());
        internal void KeyDown() => OnKeyDown?.Invoke(GetSelectedItem());
        internal void Tick() => OnUpdate?.Invoke(GetSelectedItem());

        public Hotbar(GameObject canvas)
        {
            Instance = this;
            GameObject hotbar = new("Hotbar");
            hotbar.transform.SetParent(canvas.transform, false);
            RectTransform rect = hotbar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 10f);
            rect.sizeDelta = new Vector2(SlotCount * 50f + (SlotCount - 1) * 4f, 50f);
            HorizontalLayoutGroup layout = hotbar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            for (int i = 0; i < SlotCount; i++)
                _slots[i] = CreateSlot(hotbar, i);
        }

        private GameObject CreateSlot(GameObject parent, int index)
        {
            GameObject slot = new($"Slot_{index}");
            slot.transform.SetParent(parent.transform, false);
            RectTransform rect = slot.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(50f, 50f);
            Image bg = slot.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);

            GameObject icon = new("Icon");
            icon.transform.SetParent(slot.transform, false);
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4f, 4f);
            iconRect.offsetMax = new Vector2(-4f, -4f);
            icon.AddComponent<Image>().enabled = false;

            GameObject qty = new("Quantity");
            qty.transform.SetParent(slot.transform, false);
            RectTransform qtyRect = qty.AddComponent<RectTransform>();
            qtyRect.anchorMin = new Vector2(1f, 0f);
            qtyRect.anchorMax = new Vector2(1f, 0f);
            qtyRect.pivot = new Vector2(1f, 0f);
            qtyRect.anchoredPosition = new Vector2(-2f, 2f);
            Text text = qty.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 10;
            text.alignment = TextAnchor.LowerRight;
            text.color = Color.white;

            return slot;
        }

        public InventoryItem? GetSelectedItem()
        {
            IReadOnlyList<InventoryItem> items = InventoryManager.Instance.PlayerInventory.Items;
            return _selectedIndex < items.Count ? items[_selectedIndex] : null;
        }

        public void SelectSlot(int index)
        {
            _slots[_selectedIndex].GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            _selectedIndex = index;
            _slots[_selectedIndex].GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.4f);
            OnSlotSelected?.Invoke(GetSelectedItem());
        }

        public void Refresh(IReadOnlyList<InventoryItem> items)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                GameObject slot = _slots[i];
                Image icon = slot.transform.Find("Icon").GetComponent<Image>();
                Text qty = slot.transform.Find("Quantity").GetComponent<Text>();
                Image bg = slot.GetComponent<Image>();
                if (i < items.Count)
                {
                    InventoryItem item = items[i];
                    icon.enabled = item.Definition.Image != null;
                    if (item.Definition.Image != null)
                        icon.sprite = item.Definition.Image;
                    qty.text = item.Quantity > 1 ? item.Quantity.ToString() : "";
                }
                else
                {
                    icon.enabled = false;
                    qty.text = "";
                }
            }
        }
    }
}
