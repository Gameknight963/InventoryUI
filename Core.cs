using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using InventoryFramework;

[assembly: MelonInfo(typeof(InventoryUI.Core), "InventoryUI", "1.0.0", "gameknight963")]

namespace InventoryUI
{
    public class Core : MelonMod
    {
        private ItemPicker? _picker;
        private Hotbar? _hotbar;

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName != "Version 1.9 POST") return;
            _picker?.Dispose();
            _hotbar?.Dispose();

            GameObject canvasObj = new("InventoryCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.AddComponent<HotbarBehaviour>();

            _hotbar = new(canvasObj);

            // test item, uncomment if you need it
            //InventoryManager.Instance.RegisterItem(new ItemDefinition("test_item", "Test Item"));
            //InventoryManager.Instance.PlayerInventory.AddItem("test_item", 2);

            canvasObj.AddComponent<ItemPickerBehaviour>();
            _picker = new ItemPicker(canvasObj);
        }
    }
}