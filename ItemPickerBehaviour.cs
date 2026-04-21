using MelonLoader;
using UnityEngine;

namespace InventoryUI
{
    [RegisterTypeInIl2Cpp]
    public class ItemPickerBehaviour : MonoBehaviour
    {
        public ItemPickerBehaviour(IntPtr ptr) : base(ptr) { }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
                ItemPicker.Instance?.Toggle();
        }
    }
}
