using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using InventoryFramework;
using UnityEngine.Events;

namespace InventoryUI
{
    public class ItemPicker
    {
        public static ItemPicker? Instance { get; private set; }

        private const int Columns = 9;
        private const float CellSize = 50f;
        private const float Padding = 10f;
        private const float Spacing = 4f;

        private readonly GameObject _root;
        private readonly GameObject _grid;
        private readonly InputField _search;
        private readonly ScrollRect _scroll;
        private readonly Text _emptyMessage;
        private bool _dirty;
        private int _screenHeight;
        private bool _visible = false;
        public bool IsVisible => _visible;
        public bool IsSearching => _visible && _search.isFocused;

        private readonly Action<ItemDefinition> _onItemRegistered;

        public ItemPicker(GameObject canvas)
        {
            Instance = this;

            _root = new GameObject("ItemPicker");
            _root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;

            Image bg = _root.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.85f);

            GameObject search = new("Search");
            search.transform.SetParent(_root.transform, false);
            RectTransform searchRect = search.AddComponent<RectTransform>();
            searchRect.anchorMin = new Vector2(0f, 1f);
            searchRect.anchorMax = Vector2.one;
            searchRect.pivot = new Vector2(0.5f, 1f);
            searchRect.anchoredPosition = new Vector2(0f, -Padding);
            searchRect.sizeDelta = new Vector2(-Padding * 2f, 24f);
            search.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
            _search = search.AddComponent<InputField>();
            Text searchText = CreateText(search, "Text", "");
            searchText.alignment = TextAnchor.MiddleLeft;
            searchText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _search.textComponent = searchText;
            Text placeholder = CreateText(search, "Placeholder", "Search items...");
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.color = new Color(1f, 1f, 1f, 0.5f);
            _search.placeholder = placeholder;
            _search.onValueChanged.AddListener(new UnityAction<string>((_) => _dirty = true));

            GameObject scrollObject = new("Scroll");
            scrollObject.transform.SetParent(_root.transform, false);
            RectTransform scrollRect = scrollObject.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(Padding, Padding);
            scrollRect.offsetMax = new Vector2(-Padding, -(Padding + 28f));
            _scroll = scrollObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = CellSize + 20f + Spacing;

            GameObject viewport = new("Viewport");
            viewport.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<RectMask2D>();
            _scroll.viewport = viewportRect;

            _grid = new GameObject("Grid");
            _grid.transform.SetParent(viewport.transform, false);
            RectTransform gridRect = _grid.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 1f);
            gridRect.anchorMax = Vector2.one;
            gridRect.pivot = new Vector2(0.5f, 1f);
            gridRect.anchoredPosition = Vector2.zero;
            gridRect.sizeDelta = Vector2.zero;
            _scroll.content = gridRect;

            GridLayoutGroup grid = _grid.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CellSize, CellSize + 20f);
            grid.spacing = new Vector2(Spacing, Spacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;

            GameObject scrollbarObject = new("Scrollbar");
            scrollbarObject.transform.SetParent(scrollObject.transform, false);
            RectTransform scrollbarRect = scrollbarObject.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.offsetMin = new Vector2(2f, 0f);
            scrollbarRect.offsetMax = new Vector2(8f, 0f);
            scrollbarObject.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
            Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            GameObject handle = new("Handle");
            handle.transform.SetParent(scrollbarObject.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            // Scrollbar drives the anchors, but retains these offsets. Clear
            // Unity's default 100x100 rect so the thumb fits the 6px track.
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            _scroll.verticalScrollbar = scrollbar;
            _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            _emptyMessage = CreateText(scrollObject, "EmptyMessage", "No matching items");
            _emptyMessage.gameObject.SetActive(false);

            _root.SetActive(false);
            Resize();
            Populate();
            _onItemRegistered = (_) => _dirty = true;
            InventoryManager.Instance.OnItemRegistered += _onItemRegistered;
        }

        internal void Dispose()
        {
            InventoryManager.Instance.OnItemRegistered -= _onItemRegistered;
            // A scene transition may already have destroyed the old canvas.
            if (_visible)
            {
                _visible = false;
                Cursor.lockState = _previousLockState;
                Cursor.visible = _previousCursorVisible;
            }
            if (_root != null) UnityEngine.Object.Destroy(_root);
            if (Instance == this) Instance = null;
        }

        private static Text CreateText(GameObject parent, string name, string value)
        {
            GameObject label = new(name);
            label.transform.SetParent(parent.transform, false);
            RectTransform rect = label.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 0f);
            rect.offsetMax = new Vector2(-4f, 0f);
            Text text = label.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 9;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private void Resize()
        {
            _screenHeight = Screen.height;
            _root.GetComponent<RectTransform>().sizeDelta = new Vector2(
                Columns * CellSize + (Columns - 1) * Spacing + Padding * 2f,
                Mathf.Max(90f, Mathf.Min(5 * (CellSize + 20f) + 4 * Spacing + Padding * 2f + 28f,
                    Screen.height - 180f)));
        }

        public void UpdateView()
        {
            if (!_visible) return;
            if (_screenHeight != Screen.height) Resize();
            if (_dirty) Populate();
        }

        public void Populate()
        {
            _dirty = false;
            // clear existing cells
            for (int i = _grid.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = _grid.transform.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }

            string query = _search.text.Trim();
            List<ItemDefinition> items = InventoryManager.Instance.Registry.Values
                .Where(item => item.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            int rows = Mathf.CeilToInt((float)items.Count / Columns);
            float totalHeight = rows * (CellSize + 20f) + Mathf.Max(0, rows - 1) * Spacing;
            _grid.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, totalHeight);
            _scroll.StopMovement();
            _scroll.verticalNormalizedPosition = 1f;
            _emptyMessage.gameObject.SetActive(items.Count == 0);

            foreach (ItemDefinition def in items)
                CreateCell(def);
        }

        private void CreateCell(ItemDefinition def)
        {
            GameObject cell = new(def.Id);
            cell.transform.SetParent(_grid.transform, false);
            cell.AddComponent<RectTransform>();

            Image bg = cell.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            Button button = cell.AddComponent<Button>();
            button.onClick.AddListener(new UnityAction(() =>
            {
                Hotbar.Instance?.PickFromCatalog(def);
            }));

            // icon
            GameObject icon = new("Icon");
            icon.transform.SetParent(cell.transform, false);
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.35f);
            iconRect.anchorMax = new Vector2(1f, 1f);
            iconRect.offsetMin = new Vector2(4f, 4f);
            iconRect.offsetMax = new Vector2(-4f, -4f);
            Image iconImage = icon.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            if (def.Image != null)
                iconImage.sprite = def.Image;
            else
                iconImage.color = new Color(0.4f, 0.4f, 0.4f, 1f); // grey placeholder

            // label
            GameObject label = new("Label");
            label.transform.SetParent(cell.transform, false);
            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0.35f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text text = label.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 9;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = def.Name;
        }

        private CursorLockMode _previousLockState;
        private bool _previousCursorVisible;

        public bool ContainsScreenPoint(Vector2 position) => _visible &&
            RectTransformUtility.RectangleContainsScreenPoint(_root.GetComponent<RectTransform>(), position, null);

        public void Toggle()
        {
            _visible = !_visible;
            _root.SetActive(_visible);

            if (_visible)
            {
                _previousLockState = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Hotbar.Instance?.CancelPickup();
                _search.DeactivateInputField();
                Cursor.lockState = _previousLockState;
                Cursor.visible = _previousCursorVisible;
            }
            if (_visible && _dirty) Populate();
        }
    }
}
