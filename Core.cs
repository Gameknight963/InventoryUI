using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using InventoryFramework;

[assembly: MelonInfo(typeof(InventoryUI.Core), "InventoryUI", "1.0.0", "gameknight963")]

namespace InventoryUI
{
    public class Core : MelonMod
    {
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName != "Version 1.9 POST") return;

            InventoryManager.Instance.RegisterItem(new ItemDefinition("test_item", "Test Item"));

            GameObject canvasObj = new("InventoryCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.AddComponent<HotbarBehaviour>();

            Hotbar hotbar = new(canvasObj);
            InventoryManager.Instance.PlayerInventory.OnChanged += () =>
                Hotbar.Instance!.Refresh(InventoryManager.Instance.PlayerInventory.Items);

            // test item, uncomment if you need it
            //InventoryManager.Instance.PlayerInventory.AddItem("test_item", 2);

            canvasObj.AddComponent<ItemPickerBehaviour>();
            ItemPicker picker = new(canvasObj);
        }
    }
}