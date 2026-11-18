using UnityEngine;

namespace InventoryUI
{
    public class ItemPickerBehaviour : MonoBehaviour
    {
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
                ItemPicker.Instance?.Toggle();
        }
    }
}
