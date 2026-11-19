using UnityEngine;

namespace InventoryUI
{
    public class ItemPickerBehaviour : MonoBehaviour
    {
        public void Update()
        {
            ItemPicker? picker = ItemPicker.Instance;
            if (picker == null) return;
            if (picker.IsVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                if (Hotbar.Instance?.CancelPickup() != true) picker.Toggle();
            }
            else if (!picker.IsSearching && Input.GetKeyDown(KeyCode.E))
                picker.Toggle();
        }

        public void LateUpdate()
        {
            ItemPicker.Instance?.UpdateView();
        }
    }
}
