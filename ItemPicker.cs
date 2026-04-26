using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using InventoryFramework;

namespace InventoryUI
{
    public class ItemPicker
    {
        public static ItemPicker? Instance { get; private set; }

        private const int Columns = 9;
        private const float CellSize = 50f;
        private const float Padding = 10f;
        private const float Spacing = 4f;

        private GameObject _root;
        private GameObject _grid;
        private bool _visible = false;

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

            _grid = new GameObject("Grid");
            _grid.transform.SetParent(_root.transform, false);
            RectTransform gridRect = _grid.AddComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(Padding, Padding);
            gridRect.offsetMax = new Vector2(-Padding, -Padding);

            GridLayoutGroup grid = _grid.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CellSize, CellSize + 20f);
            grid.spacing = new Vector2(Spacing, Spacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;

            _root.SetActive(false);

            _onItemRegistered = (_) => Populate();
            InventoryManager.Instance.OnItemRegistered += _onItemRegistered;
        }

        public void Dispose()
        {
            InventoryManager.Instance.OnItemRegistered -= _onItemRegistered;
        }

        public void Populate()
        {
            // clear existing cells
            for (int i = _grid.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_grid.transform.GetChild(i).gameObject);

            List<ItemDefinition> items = new(InventoryManager.Instance.Registry.Values);
            int rows = Mathf.CeilToInt((float)items.Count / Columns);
            float totalWidth = Columns * CellSize + (Columns - 1) * Spacing + Padding * 2;
            float totalHeight = rows * (CellSize + 20f) + (rows - 1) * Spacing + Padding * 2;

            MelonLogger.Msg($"Items: {items.Count}, Rows: {rows}, Width: {totalWidth}, Height: {totalHeight}");

            RectTransform rootRect = _root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(totalWidth, totalHeight);

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
            button.onClick.AddListener(new Action(() =>
            {
                InventoryManager.Instance.PlayerInventory.AddItem(def.Id);
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
                Cursor.lockState = _previousLockState;
                Cursor.visible = _previousCursorVisible;
            }
        }
    }
}
