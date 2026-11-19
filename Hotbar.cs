using DG.Tweening;
using DG.Tweening.Core;
using InventoryFramework;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryUI
{
    public class Hotbar
    {
        public static Hotbar? Instance { get; private set; }

        private const int SlotCount = 9;
        private readonly GameObject[] _slots = new GameObject[SlotCount];
        private readonly HotbarSlots _assignments = new();
        private readonly GameObject _heldRoot;
        private readonly Image _heldIcon;
        private readonly Text _heldQuantity;
        private readonly RectTransform _canvasRect;
        private readonly RectTransform _hotbarRect;
        private bool _changingInventory;
        private int _selectedIndex = 0;

        private static readonly Color SlotNormal = new(0f, 0f, 0f, 0.7f);
        private static readonly Color SlotSelected = new(1f, 1f, 1f, 0.4f);

        private readonly Text _itemLabel;

        private readonly Action _onChanged;

        public Hotbar(GameObject canvas)
        {
            Instance = this;
            _canvasRect = canvas.GetComponent<RectTransform>();
            _onChanged = () =>
            {
                if (!_changingInventory) Refresh(InventoryManager.Instance.PlayerInventory.Items);
            };
            InventoryManager.Instance.PlayerInventory.OnChanged += _onChanged;
            GameObject hotbar = new("Hotbar");
            hotbar.transform.SetParent(canvas.transform, false);
            RectTransform rect = hotbar.AddComponent<RectTransform>();
            _hotbarRect = rect;
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

            GameObject label = new("ItemLabel");
            label.transform.SetParent(canvas.transform, false);
            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 68f);
            labelRect.sizeDelta = new Vector2(300f, 20f);
            _itemLabel = label.AddComponent<Text>();
            _itemLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _itemLabel.fontSize = 14;
            _itemLabel.alignment = TextAnchor.MiddleCenter;
            _itemLabel.color = Color.white;
            _itemLabel.raycastTarget = false;

            _heldRoot = new GameObject("HeldItem");
            _heldRoot.transform.SetParent(canvas.transform, false);
            RectTransform heldRect = _heldRoot.AddComponent<RectTransform>();
            heldRect.sizeDelta = new Vector2(50f, 50f);
            _heldIcon = CreateIcon(_heldRoot);
            _heldQuantity = CreateQuantityLabel(_heldRoot);
            _heldRoot.SetActive(false);

            Refresh(InventoryManager.Instance.PlayerInventory.Items);
        }
        
        internal void Dispose()
        {
            InventoryManager.Instance.PlayerInventory.OnChanged -= _onChanged;
            DOTween.Kill(_itemLabel);
            if (Instance == this) Instance = null;
        }

        private GameObject CreateSlot(GameObject parent, int index)
        {
            GameObject slot = new($"Slot_{index}");
            slot.transform.SetParent(parent.transform, false);
            RectTransform rect = slot.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(50f, 50f);
            Image bg = slot.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            Button button = slot.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(new UnityEngine.Events.UnityAction(() =>
            {
                if (ItemPicker.Instance?.IsVisible != true) return;
                _assignments.Click(index, AddFromCatalog);
                Refresh(InventoryManager.Instance.PlayerInventory.Items);
            }));

            CreateIcon(slot).enabled = false;
            CreateQuantityLabel(slot);
            return slot;
        }

        private static Image CreateIcon(GameObject parent)
        {
            GameObject icon = new("Icon");
            icon.transform.SetParent(parent.transform, false);
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4f, 4f);
            iconRect.offsetMax = new Vector2(-4f, -4f);
            Image iconImage = icon.AddComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            return iconImage;
        }

        private static Text CreateQuantityLabel(GameObject parent)
        {
            GameObject qty = new("Quantity");
            qty.transform.SetParent(parent.transform, false);
            RectTransform qtyRect = qty.AddComponent<RectTransform>();
            qtyRect.anchorMin = new Vector2(1f, 0f);
            qtyRect.anchorMax = new Vector2(1f, 0f);
            qtyRect.pivot = new Vector2(1f, 0f);
            qtyRect.anchoredPosition = new Vector2(-2f, 2f);
            qtyRect.sizeDelta = new Vector2(46f, 46f);
            Text text = qty.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 10;
            text.alignment = TextAnchor.LowerRight;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        public void SelectSlot(int index)
        {
            _slots[_selectedIndex].GetComponent<Image>().color = SlotNormal;
            _selectedIndex = index;
            _slots[_selectedIndex].GetComponent<Image>().color = SlotSelected;
            InventoryItem? item = Resolve(_assignments[_selectedIndex]?.Id);
            InventoryManager.Instance.SelectItem(item);

            DOTween.Kill(_itemLabel);
            if (item != null)
            {
                _itemLabel.text = item.Definition.Name;
                _itemLabel.color = Color.white;
                DOTween.To(
                    (DOGetter<float>)(() => _itemLabel.color.a),
                    (DOSetter<float>)(a => _itemLabel.color = new Color(1f, 1f, 1f, a)),
                    0f,
                    0.5f
                ).SetDelay(1.5f).SetId(_itemLabel);
            }
            else
            {
                _itemLabel.text = "";
            }
        }

        public void Refresh(IReadOnlyList<InventoryItem> items)
        {
            _assignments.Reconcile(items.Select(item => new KeyValuePair<string, int>(item.Definition.Id, item.Quantity)), _selectedIndex);
            for (int i = 0; i < SlotCount; i++)
            {
                GameObject slot = _slots[i];
                Image icon = slot.transform.Find("Icon").GetComponent<Image>();
                Text qty = slot.transform.Find("Quantity").GetComponent<Text>();
                Image bg = slot.GetComponent<Image>();
                bg.color = i == _selectedIndex
                    ? SlotSelected
                    : SlotNormal;
                HotbarStack? stack = _assignments[i];
                InventoryItem? item = Resolve(stack?.Id);
                if (item != null)
                {
                    icon.enabled = item.Definition.Image != null;
                    if (item.Definition.Image != null)
                        icon.sprite = item.Definition.Image;
                    else
                        icon.sprite = null;
                    qty.text = stack!.Quantity > 1 ? stack.Quantity.ToString() : "";
                }
                else
                {
                    icon.enabled = false;
                    qty.text = "";
                }
            }
            InventoryItem? selected = Resolve(_assignments[_selectedIndex]?.Id);
            if (!ReferenceEquals(InventoryManager.Instance.SelectedItem, selected))
                SelectSlot(_selectedIndex);
            UpdateHeldItem();
        }

        private static InventoryItem? Resolve(string? id) => id == null ? null :
            InventoryManager.Instance.PlayerInventory.Items.FirstOrDefault(item => item.Definition.Id == id);

        public void PickFromCatalog(ItemDefinition definition)
        {
            _assignments.SelectCatalogItem(definition.Id);
            Refresh(InventoryManager.Instance.PlayerInventory.Items);
        }

        private void AddFromCatalog(string id, int quantity)
        {
            _changingInventory = true;
            try
            {
                InventoryManager.Instance.PlayerInventory.AddItem(id, quantity);
            }
            finally
            {
                _changingInventory = false;
            }
        }

        public bool CancelPickup()
        {
            if (_assignments.HeldItem == null) return false;
            _assignments.Cancel();
            Refresh(InventoryManager.Instance.PlayerInventory.Items);
            return true;
        }

        public void HandleOutsideClick()
        {
            if (ItemPicker.Instance?.IsVisible != true || !Input.GetMouseButtonDown(0)) return;
            if (RectTransformUtility.RectangleContainsScreenPoint(_hotbarRect, Input.mousePosition, null) ||
                ItemPicker.Instance.ContainsScreenPoint(Input.mousePosition)) return;
            _assignments.DiscardHeld((id, quantity) =>
            {
                _changingInventory = true;
                try { InventoryManager.Instance.PlayerInventory.RemoveItem(id, quantity); }
                finally { _changingInventory = false; }
            });
            Refresh(InventoryManager.Instance.PlayerInventory.Items);
        }

        public void UpdateHeldItem()
        {
            string? heldId = _assignments.HeldItem;
            ItemDefinition? held = heldId == null ? null : InventoryManager.Instance.GetItem(heldId);
            _heldRoot.SetActive(held != null);
            if (held == null) return;
            _heldRoot.transform.SetAsLastSibling();
            _heldIcon.sprite = held.Image;
            _heldIcon.color = held.Image != null ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
            _heldQuantity.text = _assignments.HeldQuantity > 1 ? _assignments.HeldQuantity.ToString() : "";
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, Input.mousePosition, null, out Vector2 position))
                _heldRoot.GetComponent<RectTransform>().anchoredPosition = position + new Vector2(28f, -28f);
        }
    }
}
